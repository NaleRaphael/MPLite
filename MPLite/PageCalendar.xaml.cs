using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace MPLite
{
    public partial class PageCalendar : Page
    {
        // ref: https://social.msdn.microsoft.com/Forums/vstudio/en-US/1f99c3c1-aeea-45aa-a501-a5b54b262799/winformhost-control-does-not-shown-when-windows-allowtransparency-true?forum=wpf
        private ProxyWindow proxyWin = null;
        
        public PageCalendar()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (proxyWin == null)
                proxyWin = new ProxyWindow(this.wfh, new Calendar.NET.Calendar());
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (proxyWin != null)
            {
                proxyWin.Close();
                proxyWin = null;
            }
        }
    }
}
