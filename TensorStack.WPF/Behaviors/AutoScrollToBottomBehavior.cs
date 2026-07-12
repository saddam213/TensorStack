using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace TensorStack.WPF.Behaviors
{
    public class AutoScrollBehavior : Behavior<ScrollViewer>
    {
        public double ScrollThreshold { get; set; } = 50;

        private bool _autoScroll = true;

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
            var scrollViewer = AssociatedObject;

            // User scrolled
            if (e.VerticalChange != 0)
            {
                _autoScroll =
                    scrollViewer.VerticalOffset >=
                    scrollViewer.ScrollableHeight - ScrollThreshold;
            }

            // Content size changed
            if (e.ExtentHeightChange != 0 && _autoScroll)
            {
                scrollViewer.ScrollToEnd();
            }
        }
    }
}