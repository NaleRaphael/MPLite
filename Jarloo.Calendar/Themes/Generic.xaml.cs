using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MPLite.Event;

namespace Jarloo.Calendar.Themes
{
    using IEvent = MPLite.Event.IEvent;
    using CustomEvent = MPLite.Event.CustomEvent;

    public partial class Generic
    {
        // TODO: show event details when a ListBoxItem is selected
        public delegate void DayContentSelectionEventHandler(IEvent evnt, SelectedDayContentActions action);
        public static event DayContentSelectionEventHandler DayContentSelectionEvent;

        public delegate void NewEventIsCreatedEventHandler(CustomEvent ce);
        public static event NewEventIsCreatedEventHandler NewEventIsCreatedEvent;

        public delegate void UpdateEventHandler(IEvent evnt);
        public static event UpdateEventHandler UpdateEvent;

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CustomEvent obj = (CustomEvent)((ListBoxItem)sender).DataContext;
            MessageBox.Show(obj.GUID.ToString());
            
            // Notify subscriber which event is selected
            DayContentSelectionEvent(obj, SelectedDayContentActions.ShowInfo);
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released)
            {
                MessageBox.Show("ListBox: Say YO");
            }
            return;
        }

        private void DockPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released)
            {
                Day info = (Day)((DockPanel)sender).DataContext;
                MessageBox.Show(info.Date.ToShortDateString().ToString());

                TimeSpan diff = info.Date - DateTime.Now;
                MessageBox.Show(diff.ToString());
            }
            return;
        }

        private void miShowInfo_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = ((MenuItem)sender).Parent as ContextMenu;
            ListBoxItem lbi = cm.PlacementTarget as ListBoxItem;

            if (lbi == null) return;
            CustomEvent obj = lbi.DataContext as CustomEvent;
            DayContentSelectionEvent(obj, SelectedDayContentActions.ShowInfo);
        }

        private void miAddEvent_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = ((MenuItem)sender).Parent as ContextMenu;
            DockPanel dp = cm.PlacementTarget as DockPanel;
            if (dp == null) return;
            
            // Set initial DateTime for dateTimePicker in WindowEventSetting
            Day obj = dp.DataContext as Day;
            DateTime initialDateTime = (obj.Date == DateTime.Today) ? DateTime.Now.Ceiling(TimeSpan.FromMinutes(30)) : obj.Date.AddHours(7);

            WindowEventSetting winEventSetting = new WindowEventSetting(initialDateTime);
            winEventSetting.Owner = Calendar.Owner;     // workaround: store owner info by static property
            winEventSetting.NewEventIsCreatedEvent += OnRecievingNewEvent;
            winEventSetting.WindowStartupLocation = WindowStartupLocation.CenterScreen;     // TODO: try to set startupLocation to center of MPLite
            winEventSetting.Show();
        }

        private void OnRecievingNewEvent(CustomEvent ce)
        {
            NewEventIsCreatedEvent(ce);
        }

        private void miEditEvent_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = ((MenuItem)sender).Parent as ContextMenu;
            ListBoxItem lbi = cm.PlacementTarget as ListBoxItem;
            if (lbi == null) return;
            IEvent evnt = lbi.DataContext as IEvent;

            WindowEventSetting winEventSetting = new WindowEventSetting();
            winEventSetting.Owner = Calendar.Owner;
            winEventSetting.UpdateEvent += OnRecievingUpdatedEvent;
            winEventSetting.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            winEventSetting.ShowEventSetting(evnt, DisplayMode.Edit);
            winEventSetting.Show();
        }

        private void OnRecievingUpdatedEvent(IEvent evnt)
        {
            UpdateEvent(evnt);
        }

        private void miDeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = ((MenuItem)sender).Parent as ContextMenu;
            ListBoxItem lbi = cm.PlacementTarget as ListBoxItem;
            if (lbi == null) return;

            CustomEvent obj = (CustomEvent)lbi.DataContext;
            DayContentSelectionEvent(obj, SelectedDayContentActions.DeleteEvent);
        }
    }

    public enum SelectedDayContentActions
    {
        ShowInfo = 1,
        AddEvent = 2,
        EditEvent = 3,
        DeleteEvent = 4
    }
}
