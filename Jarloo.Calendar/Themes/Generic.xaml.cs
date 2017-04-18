using System;
<<<<<<< HEAD
using System.Collections.Generic;
using System.Linq;
using System.Text;
=======
>>>>>>> rev8d0858e
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Jarloo.Calendar.Themes
{
    public partial class Generic
    {
<<<<<<< HEAD
        // TODO
        public delegate void DateSelectionEventHandler();
        public event DateSelectionEventHandler DateSelectionEvent;

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("ListBoxItem: Say YO");
            return;
=======
        // TODO: show event details when a ListBoxItem is selected
        public delegate void DayContentSelectionEventHandler(Guid guid);
        public static event DayContentSelectionEventHandler DayContentSelectionEvent;

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CustomEvent obj = (CustomEvent)((ListBoxItem)sender).DataContext;
            MessageBox.Show(obj.GUID.ToString());
            
            // Notify subscriber which event is selected
            DayContentSelectionEvent(obj.GUID);
        }

        private void ListBoxItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            CustomEvent obj = (CustomEvent)((ListBoxItem)sender).DataContext;
            MessageBox.Show(obj.EventText);

            // Notify subscriber
            // TODO: Add an event
>>>>>>> rev8d0858e
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
    }
}
