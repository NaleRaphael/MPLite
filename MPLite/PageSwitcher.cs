using System.Windows.Controls;

namespace MPLite
{
    public static class PageSwitcher
    {
        public static Frame pageSwitcher;

        public static void Switch(Page newPage)
        {
            pageSwitcher.NavigationService.Navigate(newPage);
        }
    }
}
