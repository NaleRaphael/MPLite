using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms.Integration;

namespace Jarloo.Calendar.Themes
{
    public partial class Generic
    {
        // TODO: show event details when a ListBoxItem is selected
        public delegate void DayContentSelectionEventHandler(IEvent evnt, SelectedDayContentActions action);
        public static event DayContentSelectionEventHandler DayContentSelectionEvent;

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
            
            Day obj = dp.DataContext as Day;
            WindowEventSetting winEventSetting = new WindowEventSetting(obj.Date);

            // TODO: set parent window of WindowEventSetting
            //winEventSetting.Owner;
            winEventSetting.PassingDataEvent += ShowDataFromWinEventSetting;
            winEventSetting.WindowStartupLocation = WindowStartupLocation.CenterScreen;     // TODO: try to set startupLocation to center of MPLite
            winEventSetting.Show();
            
            //DayContentSelectionEvent(obj, SelectedDayContentActions.AddEvent);
        }

        private void ShowDataFromWinEventSetting(string data)
        {
            MessageBox.Show("Generic:" + data);
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
        DeleteEvent = 3
    }
}
