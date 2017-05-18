using System.Windows;
using System.Windows.Controls;

namespace MPLite
{
    using SchedulerEventHandler = Event.SchedulerEventHandler;
    using SchedulerEventHandlerFactory = Event.SchedulerEventHandlerFactory;

    public partial class PageCalendar : Page
    {
        public static Window Owner { get; set; }
        public static event SchedulerEventHandler SchedulerEvent;

        public PageCalendar()
        {
            InitializeComponent();
            InitializeCalendar();
        }

        private void InitializeCalendar()
        {
            // Assign custom HandlerEventFactory to Jarloo.Calendar to create handler
            calendar.OnInitialization(new SchedulerEventHandlerFactory(SchedulerEvent), Window.GetWindow(Owner));
        }

        #region Calendar control
        private void gridLeftContainer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToPrevMonth.Visibility = Visibility.Visible;
        }

        private void gridLeftContainer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToPrevMonth.Visibility = Visibility.Hidden;
        }

        private void gridRightContainer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToNextMonth.Visibility = Visibility.Visible;
        }

        private void gridRightContainer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btnMoveToNextMonth.Visibility = Visibility.Hidden;
        }

        private void btnMoveToPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            calendar.MoveToPrevMonth();
        }

        private void btnMoveToNextMonth_Click(object sender, RoutedEventArgs e)
        {
            calendar.MoveToNextMonth();
        }

        private void btnMoveToCurrentDate_Click(object sender, RoutedEventArgs e)
        {
            calendar.MoveToCurrentMonth();
        }
        #endregion
    }
}
