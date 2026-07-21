using Amuse.App.Services;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TensorStack.WPF;
using TensorStack.WPF.Controls;

namespace Amuse.App.Controls
{
    public partial class MarkdownElement : BaseControl
    {
        private static readonly SemaphoreSlim _environmentLock = new(1, 1);
        private static CoreWebView2Environment _environment;
        private readonly SemaphoreSlim _updateLock = new(1, 1);
        private readonly SemaphoreSlim _switchLock = new(1, 1);
        private bool _isInitalized;
        private bool _isUpdatePending;
        private bool _isContentReady;
        private double _webViewerDpiX = 1;
        private double _webViewerDpiY = 1;
        private static readonly int[] _invalidContextMenuItems =
        [
            33000, // Back
            33001, // Forward
            33002, // Reload
            35003, // Print
            35016  // SendTo
        ];

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownElement"/> class.
        /// </summary>
        public MarkdownElement()
        {
            InitializeComponent();
            TextViewer.TextChanged += (s, e) =>
            {
                NotifyPropertyChanged(nameof(Length));
                IsContentReady = Length > 0;
            };
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(Settings), typeof(MarkdownElement));
        public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(nameof(Markdown), typeof(string), typeof(MarkdownElement), new PropertyMetadata<MarkdownElement, string>((c, e) => c.SetTextAsync(e)));
        public static readonly DependencyProperty IsMarkdownEnabledProperty = DependencyProperty.Register(nameof(IsMarkdownEnabled), typeof(bool), typeof(MarkdownElement), new PropertyMetadata<MarkdownElement>((c) => c.SwitchViewAsync()));
        public static readonly DependencyProperty IsScrollToBottomEnabledProperty = DependencyProperty.Register(nameof(IsScrollToBottomEnabled), typeof(bool), typeof(MarkdownElement));
        public static readonly DependencyProperty IsThinkingVisibleProperty = DependencyProperty.Register(nameof(IsThinkingVisible), typeof(bool), typeof(MarkdownElement), new PropertyMetadata<MarkdownElement>((c) => c.UpdateThinking()) { DefaultValue = true });

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public Settings Settings
        {
            get { return (Settings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the markdown.
        /// </summary>
        public string Markdown
        {
            get { return (string)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the markdown view is enabled.
        /// </summary>
        public bool IsMarkdownEnabled
        {
            get { return (bool)GetValue(IsMarkdownEnabledProperty); }
            set { SetValue(IsMarkdownEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is scroll to bottom enabled.
        /// </summary>
        public bool IsScrollToBottomEnabled
        {
            get { return (bool)GetValue(IsScrollToBottomEnabledProperty); }
            set { SetValue(IsScrollToBottomEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thinking visibility.
        /// </summary>
        public bool IsThinkingVisible
        {
            get { return (bool)GetValue(IsThinkingVisibleProperty); }
            set { SetValue(IsThinkingVisibleProperty, value); }
        }

        /// <summary>
        /// Gets the markup length.
        /// </summary>
        public int Length => TextViewer?.Text?.Length ?? 0;

        /// <summary>
        /// Gets a value indicating whether this instance is content ready.
        /// </summary>
        public bool IsContentReady
        {
            get { return _isContentReady; }
            private set { SetProperty(ref _isContentReady, value); }
        }


        /// <summary>
        /// Set the text content
        /// </summary>
        /// <param name="content">The content.</param>
        public async Task SetTextAsync(string content)
        {
            await ClearAsync();
            TextViewer.AppendText(content);
            await UpdateWebView();
            if (IsScrollToBottomEnabled)
                ScrollViewerHost.ScrollToBottom();
        }


        /// <summary>
        /// Appends text content.
        /// </summary>
        /// <param name="content">The content.</param>
        public Task AppendTextAsync(string content)
        {
            TextViewer.AppendText(content);
            _ = UpdateWebView();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Clears text content.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task ClearAsync()
        {
            TextViewer.Clear();
            await RenderHtmlAsync(string.Empty);
        }


        /// <summary>
        /// Gets the thinking text.
        /// </summary>
        public string GetThinkingText()
        {
            return Utils.GetThinkingText(TextViewer?.Text);
        }


        /// <summary>
        /// Gets the response text.
        /// </summary>
        public string GetResponseText()
        {
            return Utils.GetResponseText(TextViewer?.Text);
        }


        /// <summary>
        /// Switch view (Markdown/PlainText)
        /// </summary>
        private async Task SwitchViewAsync()
        {
            try
            {
                await _switchLock.WaitAsync();
                if (IsMarkdownEnabled)
                {
                    await UpdateWebView();
                    WebViewer.Visibility = Visibility.Visible;
                    TextViewer.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TextViewer.Height = double.NaN;
                    TextViewer.Visibility = Visibility.Visible;
                    WebViewer.Visibility = Visibility.Collapsed;
                }
            }
            finally
            {
                _switchLock.Release();
            }
        }


        /// <summary>
        /// Updates the WebView.
        /// </summary>
        private async Task UpdateWebView()
        {
            if (!IsMarkdownEnabled)
                return;

            _isUpdatePending = true;
            if (!await _updateLock.WaitAsync(0))
                return;

            try
            {
                if (!_isInitalized)
                    await CreateWebViewAsync();

                while (_isUpdatePending)
                {
                    _isUpdatePending = false;
                    var htmlContent = MarkdownConverter.BuildHtml(TextViewer.Text, FontSize, FontFamily, IsThinkingVisible);
                    await RenderHtmlAsync(htmlContent);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] UpdateWebView: {ex.Message}");
            }
            finally
            {
                _updateLock.Release();
            }
        }


        /// <summary>
        /// Render the HTML in the WebView
        /// </summary>
        /// <param name="html">The HTML.</param>
        private async Task RenderHtmlAsync(string htmlContent)
        {
            if (!_isInitalized)
                return;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object sender, CoreWebView2NavigationCompletedEventArgs e) => tcs.TrySetResult();
            WebViewer.NavigationCompleted += Handler;

            try
            {
                WebViewer.NavigateToString(htmlContent);
                await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] RenderHtml: {ex.Message}");
            }
            finally
            {
                WebViewer.NavigationCompleted -= Handler;
            }
        }


        /// <summary>
        /// Updates the thinking section visibility.
        /// </summary>
        private async Task UpdateThinking()
        {
            if (!_isInitalized || Length == 0)
                return;

            try
            {
                var executeScriptTask = IsThinkingVisible
                    ? WebViewer.CoreWebView2.ExecuteScriptAsync("document.getElementById('thinking-panel').setAttribute('open', '')")
                    : WebViewer.CoreWebView2.ExecuteScriptAsync("document.getElementById('thinking-panel').removeAttribute('open')");
                await executeScriptTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] UpdateThinking: {ex.Message}");
            }
        }


        /// <summary>
        /// Create WebView
        /// </summary>
        private async Task CreateWebViewAsync()
        {
            try
            {
                var environment = await GetEnvironmentAsync();
                await WebViewer
                    .EnsureCoreWebView2Async(environment)
                    .WaitAsync(TimeSpan.FromSeconds(5));
                ConfigureWebView();
                _isInitalized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] CreateWebViewAsync: {ex.Message}");
            }
        }


        /// <summary>
        /// Configures the WebView.
        /// </summary>
        private void ConfigureWebView()
        {
            var source = PresentationSource.FromVisual(WebViewer);
            if (source?.CompositionTarget != null)
            {
                _webViewerDpiX = source.CompositionTarget.TransformToDevice.M11;
                _webViewerDpiY = source.CompositionTarget.TransformToDevice.M22;
            }
 
            WebViewer.Height = (int)ScrollViewerHost.ActualHeight;
            WebViewer.CoreWebView2.Settings.IsScriptEnabled = true;
            WebViewer.CoreWebView2.Settings.IsWebMessageEnabled = true;
            WebViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            WebViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebViewer.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            WebViewer.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            WebViewer.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            WebViewer.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            WebViewer.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebViewer.CoreWebView2.Settings.IsZoomControlEnabled = false;
            WebViewer.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
            WebViewer.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            WebViewer.CoreWebView2.Settings.IsNonClientRegionSupportEnabled = false;
            WebViewer.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            WebViewer.CoreWebView2.Settings.IsPinchZoomEnabled = false;
            WebViewer.CoreWebView2.Settings.IsReputationCheckingRequired = false;
            WebViewer.NavigationStarting += WebView_NavigationStarting;
            WebViewer.CoreWebView2.ContextMenuRequested += WebView_ContextMenuRequested;
            WebViewer.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
        }


        /// <summary>
        /// Get WebView environment
        /// </summary>
        private static async Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            if (_environment != null)
                return _environment;

            await _environmentLock.WaitAsync();
            try
            {
                _environment ??= await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(App.DirectoryData, "Temp"), options: new CoreWebView2EnvironmentOptions
                {
                    EnableTrackingPrevention = true,
                    AreBrowserExtensionsEnabled = false,
                    IsCustomCrashReportingEnabled = true,
                    ExclusiveUserDataFolderAccess = true,
                    AllowSingleSignOnUsingOSPrimaryAccount = false,
                    AdditionalBrowserArguments = "--disable-gpu --disable-gpu-compositing"
                });
                return _environment;
            }
            finally
            {
                _environmentLock.Release();
            }
        }


        /// <summary>
        /// Handles the NavigationStarting event of the WebView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!e.IsUserInitiated)
                return;

            if (string.Equals(e.Uri, "about:blank", StringComparison.OrdinalIgnoreCase))
                return;

            Debug.WriteLine($"[WebView] [NavigationStarting] Uri: {e.Uri}");
            if (e.NavigationKind == CoreWebView2NavigationKind.NewDocument)
            {
                e.Cancel = true;
                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri uriResult))
                {
                    if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                    {
                        URL.NavigateToUrl(e.Uri); //TODO: popup warning extranal links
                    }
                }
            }
        }


        /// <summary>
        /// Handles the WebMessageReceived event of the WebView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CoreWebView2WebMessageReceivedEventArgs"/> instance containing the event data.</param>
        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var webMessage = JsonSerializer.Deserialize<WebMessage>(e.WebMessageAsJson, Json.DefaultOptions);
            if (webMessage.Type == WebMessageType.Click)
            {
                if (!string.IsNullOrEmpty(webMessage.Clipboard))
                    Clipboard.SetText(webMessage.Clipboard.Trim());
            }
            else if (webMessage.Type == WebMessageType.Thinking)
            {
                IsThinkingVisible = !IsThinkingVisible;
            }
            else if (webMessage.Type == WebMessageType.Resize)
            {
                WebViewer.Height = (int)Math.Max(ScrollViewerHost.ActualHeight - 0.5, webMessage.Y);
            }
        }


        /// <summary>
        /// Handles the ContextMenuRequested event of the WebView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CoreWebView2ContextMenuRequestedEventArgs"/> instance containing the event data.</param>
        private void WebView_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            var menuItems = new List<CoreWebView2ContextMenuItem>();
            for (int i = 0; i < e.MenuItems.Count; i++)
            {
                if (_invalidContextMenuItems.Contains(e.MenuItems[i].CommandId))
                    continue;
                menuItems.Add(e.MenuItems[i]);
            }

            if (e.ContextMenuTarget.Kind == CoreWebView2ContextMenuTargetKind.Page)
            {
                var itemCopyAll = WebViewer.CoreWebView2.Environment.CreateContextMenuItem("Copy All                       Ctrl+A", null, CoreWebView2ContextMenuItemKind.Command);
                var itemCopyResponse = WebViewer.CoreWebView2.Environment.CreateContextMenuItem("Copy Response           Ctrl+C", null, CoreWebView2ContextMenuItemKind.Command);
                var itemCopyThinking = WebViewer.CoreWebView2.Environment.CreateContextMenuItem("Copy Thinking             Ctrl+T", null, CoreWebView2ContextMenuItemKind.Command);
                itemCopyAll.CustomItemSelected += (s, args) => Clipboard.SetText(TextViewer.Text);
                itemCopyResponse.CustomItemSelected += (s, args) => Clipboard.SetText(GetResponseText());
                itemCopyThinking.CustomItemSelected += (s, args) => Clipboard.SetText(GetThinkingText());
                itemCopyThinking.IsEnabled = Utils.HasThinkingText(TextViewer.Text);

                menuItems.Add(itemCopyAll);
                menuItems.Add(itemCopyResponse);
                menuItems.Add(itemCopyThinking);
            }

            e.MenuItems.Clear();
            foreach (var menuItem in menuItems)
                e.MenuItems.Add(menuItem);
        }


        /// <summary>
        /// Handles the ScrollChanged event of the ScrollViewerHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ScrollChangedEventArgs"/> instance containing the event data.</param>
        private void ScrollViewerHost_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (WebViewer.Handle == nint.Zero || Settings == null)
                return;

            var scrollViewerRect = new Rect(0, 0, ScrollViewerHost.ActualWidth, ScrollViewerHost.ActualHeight);
            var transform = ScrollViewerHost.TransformToDescendant(WebViewer);
            if (transform == null)
                return;

            // This calculates exactly what part of the WebView is inside the ScrollViewer view port
            // and Intersect it with the actual dimension limits of the WebView2 control itself
            var visibleRect = transform.TransformBounds(scrollViewerRect);
            var webViewBounds = new Rect(0, 0, WebViewer.ActualWidth, WebViewer.ActualHeight);
            visibleRect.Intersect(webViewBounds);
            if (visibleRect.IsEmpty || visibleRect.Width <= 0 || visibleRect.Height <= 0)
            {
                _ = Native.SetWindowRgn(WebViewer.Handle, Native.CreateRectRgn(0, 0, 0, 0), true);
            }
            else
            {
                var scale = Settings.UIScale;
                var left = (int)(visibleRect.Left * _webViewerDpiX * scale);
                var top = (int)(visibleRect.Top * _webViewerDpiY * scale);
                var right = (int)(visibleRect.Right * _webViewerDpiX * scale);
                var bottom = (int)(visibleRect.Bottom * _webViewerDpiY * scale);
                _ = Native.SetWindowRgn(WebViewer.Handle, Native.CreateRectRgn(left, top, right, bottom), true);
            }
        }

        private record WebMessage(WebMessageType Type, int X, int Y, string Clipboard);

        public enum WebMessageType
        {
            Click = 0,
            Resize = 1,
            Thinking = 2
        }
    }
}