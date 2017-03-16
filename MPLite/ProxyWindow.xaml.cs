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
using System.Windows.Shapes;

namespace MPLite
{
    // Solution of adding WinForm controls in a WPF window when `allowTransparency` = True;
    // ref: https://social.msdn.microsoft.com/Forums/vstudio/en-US/1f99c3c1-aeea-45aa-a501-a5b54b262799/winformhost-control-does-not-shown-when-windows-allowtransparency-true?forum=wpf
    public partial class ProxyWindow : Window
    {
        FrameworkElement t;

        public ProxyWindow()
        {
            InitializeComponent();
        }

        public ProxyWindow(FrameworkElement target, System.Windows.Forms.Control child)
        {
            InitializeComponent();

            t = target;
            wfh.Child = child;
            Owner = Window.GetWindow(t);

            Owner.LocationChanged += new EventHandler(PositionAndResize);
            t.LayoutUpdated += new EventHandler(PositionAndResize);
            PositionAndResize(null, null);

            if (Owner.IsVisible)
            {
                Show();
            }
            else
            {
                Owner.IsVisibleChanged += delegate
                {
                    if (Owner.IsVisible)
                    {
                        Show();
                    }
                };
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Owner.LocationChanged -= PositionAndResize;
            t.LayoutUpdated -= PositionAndResize;
        }

        void PositionAndResize(object sender, EventArgs e)
        {
            // Try to solve the problem: "Visual is not connected to a PresentationSource"
            if (PresentationSource.FromVisual(t) == null)
                return;

            Point P = t.PointToScreen(new Point());
            Left = P.X;
            Top = P.Y;
            Height = t.ActualHeight;
            Width = t.ActualWidth;
        }
    }
}
