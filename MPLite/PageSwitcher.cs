using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MPLite
{
    public static class PageSwitcher
    {
        public static Frame pageSwitcher;

        public static void Switch(UserControl newPage)
        {
            pageSwitcher.NavigationService.Navigate(newPage);
        }
    }
}
