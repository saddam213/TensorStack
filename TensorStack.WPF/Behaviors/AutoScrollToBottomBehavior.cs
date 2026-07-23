using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace TensorStack.WPF.Behaviors
{
    public class AutoScrollBehavior : Behavior<ScrollViewer>
    {
        private bool _autoScroll = true;
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(AutoScrollBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty ScrollThresholdProperty = DependencyProperty.Register(nameof(ScrollThreshold), typeof(double), typeof(AutoScrollBehavior), new PropertyMetadata(60.0));

        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public double ScrollThreshold
        {
            get { return (double)GetValue(ScrollThresholdProperty); }
            set { SetValue(ScrollThresholdProperty, value); }
        }


        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ScrollChanged += OnScrollChanged;
        }


        protected override void OnDetaching()
        {
            AssociatedObject.ScrollChanged -= OnScrollChanged;
            base.OnDetaching();
        }


        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!IsEnabled)
            {
                _autoScroll = false;
                return;
            }

            // User scrolled
            if (e.VerticalChange != 0)
            {
                _autoScroll = AssociatedObject.VerticalOffset >= AssociatedObject.ScrollableHeight - ScrollThreshold;
            }

            // Content size changed
            if (e.ExtentHeightChange != 0 && _autoScroll)
            {
                AssociatedObject.ScrollToEnd();
            }
        }
    }
}