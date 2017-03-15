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
        //private Calendar.NET.Calendar scheduler;

        public PageCalendar()
        {
            InitializeComponent();
            /*System.Windows.Forms.Integration.WindowsFormsHost host =
                new System.Windows.Forms.Integration.WindowsFormsHost();
            InitCalendar();
            host.Child = scheduler;
            GridContainer.Children.Add(host);*/
        }

        private void label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            return;
        }

        /*private void InitCalendar()
        {
            if (scheduler == null) scheduler = new Calendar.NET.Calendar();

            scheduler.AllowEditingEvents = false;
            scheduler.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            scheduler.BackColor = System.Drawing.Color.Transparent;
            scheduler.CalendarDate = new System.DateTime(2012, 4, 24, 13, 16, 0, 0);
            scheduler.CalendarView = Calendar.NET.CalendarViews.Month;
            scheduler.DateHeaderFont = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            scheduler.DayOfWeekFont = new System.Drawing.Font("Arial", 10F);
            scheduler.DaysFont = new System.Drawing.Font("Arial", 10F);
            scheduler.DimDisabledEvents = true;
            scheduler.HighlightCurrentDay = true;
            scheduler.LoadPresetHolidays = true;
            scheduler.Location = new System.Drawing.Point(12, 12);
            scheduler.Name = "scheduler";
            scheduler.ShowArrowControls = true;
            scheduler.ShowDashedBorderOnDisabledEvents = true;
            scheduler.ShowDateInHeader = true;
            scheduler.ShowDisabledEvents = false;
            scheduler.ShowEventTooltips = true;
            scheduler.ShowTodayButton = true;
            scheduler.Size = new System.Drawing.Size(714, 497);
            scheduler.TabIndex = 0;
            scheduler.TodayFont = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
        }

        private void GridContainer_Loaded(object sender, RoutedEventArgs e)
        {
            //System.Windows.Forms.Integration.WindowsFormsHost host =
            //    new System.Windows.Forms.Integration.WindowsFormsHost();
            //scheduler = new Calendar.NET.Calendar();
            //this.GridContainer.Children.Add(host);
        }*/
    }
}
