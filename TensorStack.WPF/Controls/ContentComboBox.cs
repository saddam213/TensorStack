using System.Windows;
using System.Windows.Controls;

namespace TensorStack.WPF.Controls
{
    public class ContentComboBox : ComboBox
    {
        static ContentComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentComboBox), new FrameworkPropertyMetadata(typeof(ContentComboBox)));
        }

        public static readonly DependencyProperty TopContentProperty = DependencyProperty.Register(nameof(TopContent), typeof(UIElement), typeof(ContentComboBox), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty BottomContentProperty = DependencyProperty.Register(nameof(BottomContent), typeof(UIElement), typeof(ContentComboBox), new FrameworkPropertyMetadata(null));

        public UIElement TopContent
        {
            get => (UIElement)GetValue(TopContentProperty);
            set => SetValue(TopContentProperty, value);
        }

        public UIElement BottomContent
        {
            get => (UIElement)GetValue(BottomContentProperty);
            set => SetValue(BottomContentProperty, value);
        }
    }
}
