using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Jarloo.Calendar.Themes
{
    public partial class Generic
    {
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("ListBoxItem: Say YO");
            return;
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released)
            {
                MessageBox.Show("ListBox: Say YO");
            }
            return;
        }
    }
}
