﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Jarloo.Calendar.Themes
{
    public partial class Generic
    {
        // TODO
        public delegate void DateSelectionEventHandler();
        public event DateSelectionEventHandler DateSelectionEvent;

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

        private void DockPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released)
            {
                Day info = (Day)((DockPanel)sender).DataContext;
                MessageBox.Show(info.Date.ToShortDateString().ToString());
            }
            return;
        }
    }
}
