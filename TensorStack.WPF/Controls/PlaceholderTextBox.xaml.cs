using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TensorStack.WPF.Controls
{
    /// <summary>
    /// Interaction logic for PlaceholderTextBox.xaml
    /// </summary>
    public partial class PlaceholderTextBox : BaseControl
    {
        public PlaceholderTextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(PlaceholderTextBox));
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(PlaceholderTextBox));
        public static readonly DependencyProperty PlaceholderMarginProperty = DependencyProperty.Register(nameof(PlaceholderMargin), typeof(Thickness), typeof(PlaceholderTextBox), new PropertyMetadata(new Thickness(4, 2, 0, 0)));
        public static readonly DependencyProperty PlaceholderOpacityProperty = DependencyProperty.Register(nameof(PlaceholderOpacity), typeof(double), typeof(PlaceholderTextBox), new PropertyMetadata(0.7d));
        public static readonly DependencyProperty PlaceholderFontStyleProperty = DependencyProperty.Register(nameof(PlaceholderFontStyle), typeof(FontStyle), typeof(PlaceholderTextBox), new PropertyMetadata(FontStyles.Italic));
        public static readonly DependencyProperty AcceptsReturnProperty = DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(PlaceholderTextBox), new PropertyMetadata(false));
        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(PlaceholderTextBox), new PropertyMetadata(TextWrapping.NoWrap));
        public static readonly DependencyProperty TextPaddingProperty = DependencyProperty.Register(nameof(TextPadding), typeof(Thickness), typeof(PlaceholderTextBox), new PropertyMetadata(new Thickness(2)));
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(PlaceholderTextBox), new PropertyMetadata(ScrollBarVisibility.Auto));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string Placeholder
        {
            get { return (string)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        public Thickness PlaceholderMargin
        {
            get { return (Thickness)GetValue(PlaceholderMarginProperty); }
            set { SetValue(PlaceholderMarginProperty, value); }
        }

        public double PlaceholderOpacity
        {
            get { return (double)GetValue(PlaceholderOpacityProperty); }
            set { SetValue(PlaceholderOpacityProperty, value); }
        }

        public FontStyle PlaceholderFontStyle
        {
            get { return (FontStyle)GetValue(PlaceholderFontStyleProperty); }
            set { SetValue(PlaceholderFontStyleProperty, value); }
        }

        public bool AcceptsReturn
        {
            get { return (bool)GetValue(AcceptsReturnProperty); }
            set { SetValue(AcceptsReturnProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public Thickness TextPadding
        {
            get { return (Thickness)GetValue(TextPaddingProperty); }
            set { SetValue(TextPaddingProperty, value); }
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }


        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if(e.Data.GetDataPresent(DataFormats.Text))
            {
                Text = (string)e.Data.GetData(DataFormats.Text);
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileName = ((string[])e.Data.GetData(DataFormats.FileDrop))?.FirstOrDefault();
                if(File.Exists(fileName)) 
                {
                    Text = await File.ReadAllTextAsync(fileName);
                }
            }
        }
    }
}
