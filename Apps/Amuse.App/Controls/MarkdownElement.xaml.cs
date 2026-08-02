using Amuse.App.Services;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        private readonly SemaphoreSlim _createLock = new(1, 1);
        private readonly SemaphoreSlim _updateLock = new(1, 1);
        private readonly SemaphoreSlim _switchLock = new(1, 1);
        private bool _isInitialized;
        private bool _isUpdatePending;
        private bool _isContentReady;
        private double _webViewerDpiX = 1;
        private double _webViewerDpiY = 1;
        private int _cacheLength;
        private static readonly int[] _invalidContextMenuItems =
        [
            33000, // Back
            33001, // Forward
            33002, // Reload
            35003, // Print
            35004, // SaveAs
            35016  // SendTo
        ];

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownElement"/> class.
        /// </summary>
        public MarkdownElement()
        {
            SaveCommand = new AsyncRelayCommand<bool>(SaveAsync, (s) => IsInputEnabled);
            CopyCommand = new AsyncRelayCommand<bool>(CopyAsync, (s) => IsInputEnabled);
            CopyResponseCommand = new AsyncRelayCommand<bool>(CopyResponseAsync, (s) => IsInputEnabled);
            CopyThinkingCommand = new AsyncRelayCommand<bool>(CopyThinkingAsync, (s) => IsInputEnabled);
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
        public static readonly DependencyProperty IsInputEnabledProperty = DependencyProperty.Register(nameof(IsInputEnabled), typeof(bool), typeof(MarkdownElement), new PropertyMetadata(true));
        public static readonly DependencyProperty IsContextMenuEnabledProperty = DependencyProperty.Register(nameof(IsContextMenuEnabled), typeof(bool), typeof(MarkdownElement), new PropertyMetadata(true));
        public AsyncRelayCommand<bool> SaveCommand { get; }
        public AsyncRelayCommand<bool> CopyCommand { get; }
        public AsyncRelayCommand<bool> CopyResponseCommand { get; }
        public AsyncRelayCommand<bool> CopyThinkingCommand { get; }

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
        /// Gets or sets if input actiona are enabled.
        /// </summary>
        public bool IsInputEnabled
        {
            get { return (bool)GetValue(IsInputEnabledProperty); }
            set { SetValue(IsInputEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets if ContextMenus are enabled.
        /// </summary>
        public bool IsContextMenuEnabled
        {
            get { return (bool)GetValue(IsContextMenuEnabledProperty); }
            set { SetValue(IsContextMenuEnabledProperty, value); }
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
        /// Set the body content.
        /// </summary>
        /// <param name="content">The content.</param>
        public async Task SetTextAsync(string content)
        {
            await ClearAsync();
            TextViewer.AppendText(content);
            await UpdateWebView(true);
            await CommitHtmlBody();
            _cacheLength = Length;
            if (IsScrollToBottomEnabled)
                ScrollViewerHost.ScrollToBottom();
        }


        /// <summary>
        /// Appends text content to the end of the body.
        /// </summary>
        /// <param name="content">The content.</param>
        public Task AppendStreamAsync(string content)
        {
            TextViewer.AppendText(content);
            _ = UpdateWebView();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Add a complete message and cache the body content.
        /// </summary>
        /// <param name="content">The content.</param>
        public async Task AppendTextAsync(string content)
        {
            TextViewer.AppendText(content);
            await UpdateWebView(true);
            await CommitHtmlBody();
            _cacheLength = Length;
        }


        /// <summary>
        /// Reload the body content
        /// </summary>
        public async Task ReloadAsync()
        {
            _cacheLength = 0;
            await ClearHtmlBody();
            await UpdateWebView(true);
        }


        /// <summary>
        /// Clears body content.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task ClearAsync()
        {
            _cacheLength = 0;
            TextViewer.Clear();
            await ClearHtmlBody();
        }


        /// <summary>
        /// Gets the plain text.
        /// </summary>
        public string GetPlainText()
        {
            return TextViewer?.Text;
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
        /// Gets the HTML page.
        /// </summary>
        public string GetHtmlPage()
        {
            return MarkdownConverter.BuildFullHtml(TextViewer?.Text, FontSize, FontFamily, IsThinkingVisible);
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
                    await ReloadAsync();
                    WebViewer.Visibility = Visibility.Visible;
                    TextViewer.Visibility = Visibility.Collapsed;
                    WebViewer.Focus();
                }
                else
                {
                    TextViewer.Height = double.NaN;
                    TextViewer.Visibility = Visibility.Visible;
                    WebViewer.Visibility = Visibility.Collapsed;
                    TextViewer.Focus();
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
        private async Task UpdateWebView(bool wait = false)
        {
            if (!IsMarkdownEnabled)
                return;

            _isUpdatePending = true;
            if (!await _updateLock.WaitAsync(wait ? -1 : 0))
                return;

            try
            {
                if (!_isInitialized)
                {
                    if (Length == 0)
                        return;

                    await CreateWebViewAsync();
                }

                while (_isUpdatePending)
                {
                    _isUpdatePending = false;
                    await UpdateHtmlBody();
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
        /// Updates the HTML body.
        /// </summary>
        private async Task UpdateHtmlBody()
        {
            if (!_isInitialized)
                return;

            try
            {
                var markdown = TextViewer.Text[_cacheLength..];
                if (markdown.Length == 0)
                    return;

                var bodyHtml = MarkdownConverter.BuildBody(markdown, IsThinkingVisible);
                await WebViewer.CoreWebView2.ExecuteScriptAsync($"updateStreamContent({JsonSerializer.Serialize(bodyHtml)});");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] UpdateMainContent: {ex.Message}");
            }
        }


        /// <summary>
        /// Clears the HTML body.
        /// </summary>
        private async Task ClearHtmlBody()
        {
            if (!_isInitialized)
                return;

            try
            {
                await WebViewer.CoreWebView2.ExecuteScriptAsync("clearBody();");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] ClearHtmlBody: {ex.Message}");
            }
        }


        /// <summary>
        /// Commits the HTML body.
        /// </summary>
        private async Task CommitHtmlBody()
        {
            if (!_isInitialized)
                return;

            try
            {
                await WebViewer.CoreWebView2.ExecuteScriptAsync("commitStreamContent();");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] CommitHtmlBody: {ex.Message}");
            }
        }


        /// <summary>
        /// Updates the thinking section visibility.
        /// </summary>
        private async Task UpdateThinking()
        {
            if (!_isInitialized || Length == 0)
                return;

            try
            {
                await WebViewer.CoreWebView2.ExecuteScriptAsync($"toggleThinking({JsonSerializer.Serialize(IsThinkingVisible)});");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] UpdateThinking: {ex.Message}");
            }
        }


        /// <summary>
        /// Render the HTML in the WebView
        /// </summary>
        /// <param name="html">The HTML.</param>
        private async Task CreateHtmlPageAsync()
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Handler(object sender, CoreWebView2NavigationCompletedEventArgs e) => tcs.TrySetResult();
            WebViewer.NavigationCompleted += Handler;

            try
            {
                var htmlContent = MarkdownConverter.BuildHtml(FontSize, FontFamily, IsThinkingVisible);
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
        /// Create WebView
        /// </summary>
        private async Task CreateWebViewAsync()
        {
            try
            {
                await _createLock.WaitAsync();
                if (_isInitialized)
                    return;

                var environment = await GetEnvironmentAsync();
                var options = environment.CreateCoreWebView2ControllerOptions();
                options.AllowHostInputProcessing = true;
                await WebViewer.EnsureCoreWebView2Async(environment, options);
                WebViewer.Height = (int)ScrollViewerHost.ActualHeight;
                WebViewer.CoreWebView2.SetVirtualHostNameToFolderMapping("history", Settings.DirectoryHistory, CoreWebView2HostResourceAccessKind.Allow);
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
                var source = PresentationSource.FromVisual(WebViewer);
                if (source?.CompositionTarget != null)
                {
                    _webViewerDpiX = source.CompositionTarget.TransformToDevice.M11;
                    _webViewerDpiY = source.CompositionTarget.TransformToDevice.M22;
                }
                await CreateHtmlPageAsync();
                WebViewer.NavigationStarting += WebView_NavigationStarting;
                WebViewer.CoreWebView2.ContextMenuRequested += WebView_ContextMenuRequested;
                WebViewer.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView] [Exception] CreateWebViewAsync: {ex.Message}");
            }
            finally
            {
                _createLock.Release();
            }
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
                if (_environment != null)
                    return _environment;

                var options = new CoreWebView2EnvironmentOptions
                {
                    EnableTrackingPrevention = true,
                    AreBrowserExtensionsEnabled = false,
                    IsCustomCrashReportingEnabled = true,
                    ExclusiveUserDataFolderAccess = true,
                    AllowSingleSignOnUsingOSPrimaryAccount = false,
                    AdditionalBrowserArguments = "--disable-gpu"
                };
                _environment = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(App.DirectoryData, "Temp"), options: options);
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
        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var webMessage = JsonSerializer.Deserialize<WebMessage>(e.WebMessageAsJson, Json.DefaultOptions);
            if (webMessage.Type == WebMessageType.Click)
            {
            }
            else if (webMessage.Type == WebMessageType.Thinking)
            {
                IsThinkingVisible = !IsThinkingVisible;
            }
            else if (webMessage.Type == WebMessageType.Resize)
            {
                WebViewer.Height = (int)Math.Max(ScrollViewerHost.ActualHeight - 0.5, webMessage.Y);
            }
            else if (webMessage.Type == WebMessageType.Clipboard)
            {
                if (!string.IsNullOrEmpty(webMessage.Clipboard))
                {
                    await SetClipboardTextAsync(webMessage.Clipboard);
                }
            }
        }


        /// <summary>
        /// Handles the ContextMenuRequested event of the WebView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CoreWebView2ContextMenuRequestedEventArgs"/> instance containing the event data.</param>
        private void WebView_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            if (!IsContextMenuEnabled)
            {
                e.Handled = true;
                return;
            }

            var menuItems = new List<CoreWebView2ContextMenuItem>();
            for (int i = 0; i < e.MenuItems.Count; i++)
            {
                if (!_invalidContextMenuItems.Contains(e.MenuItems[i].CommandId))
                    menuItems.Add(e.MenuItems[i]);
            }

            if (e.ContextMenuTarget.Kind == CoreWebView2ContextMenuTargetKind.Page)
            {
                var menuItemSave = CreateMenuItem("Save As                       Ctrl+S", SaveAsync, IsInputEnabled);
                var menuItemCopy = CreateMenuItem("Copy                           Ctrl+C", default, false);
                var menuItemCopyAll = CreateMenuItem("Copy All                      Ctrl+A", CopyAsync, IsInputEnabled);
                var menuItemResponse = CreateMenuItem("Copy Response           Ctrl+R", CopyResponseAsync, IsInputEnabled);
                var menuItemThinking = CreateMenuItem("Copy Thinking             Ctrl+T", CopyThinkingAsync, IsInputEnabled && Utils.HasThinkingText(TextViewer.Text));
                menuItems.Insert(0, menuItemSave);
                menuItems.Add(menuItemCopy);
                menuItems.Add(menuItemCopyAll);
                menuItems.Add(menuItemResponse);
                menuItems.Add(menuItemThinking);
            }

            e.MenuItems.Clear();
            foreach (var menuItem in menuItems)
                e.MenuItems.Add(menuItem);
        }


        /// <summary>
        /// Creates the menu item.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="clickFunction">The click function.</param>
        /// <param name="isEnabled">if set to <c>true</c> [is enabled].</param>
        private CoreWebView2ContextMenuItem CreateMenuItem(string label, Func<bool, Task> clickFunction = default, bool isEnabled = true)
        {
            var menuItem = WebViewer.CoreWebView2.Environment.CreateContextMenuItem(label, null, CoreWebView2ContextMenuItemKind.Command);
            menuItem.IsEnabled = isEnabled;
            if (clickFunction != null)
            {
                menuItem.CustomItemSelected += async (s, args) => await clickFunction(true);
            }
            return menuItem;
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
            Clipboard = 2,
            Thinking = 3,
        }

        private static async Task SetClipboardTextAsync(string text)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return;
                }
                catch (COMException)
                {
                    await Task.Delay(100);
                }
            }
        }


        private async Task CopyAsync(bool isHtml)
        {
            // copy plain all text
            Debug.WriteLine($"CopyTextAsync - IsHtml: {isHtml}");
        }

        private async Task SaveAsync(bool isHtml)
        {
            // save plain all text
            Debug.WriteLine($"SaveTextAsync - IsHtml: {isHtml}");
        }


        private async Task CopyResponseAsync(bool isHtml)
        {
            // save html doc
            Debug.WriteLine($"CopyResponseAsync - IsHtml: {isHtml}");
        }


        private async Task CopyThinkingAsync(bool isHtml)
        {
            // save html doc
            Debug.WriteLine($"CopyThinkingAsync - IsHtml: {isHtml}");
        }
    }
}