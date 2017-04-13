/*
    Jarloo
    http://www.jarloo.com
 
    This work is licensed under a Creative Commons Attribution-ShareAlike 3.0 Unported License  
    http://creativecommons.org/licenses/by-sa/3.0/ 

*/
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Jarloo.Calendar
{
    public class Calendar : Control, INotifyPropertyChanged
    {
        private int currentViewingYear;
        private int currentViewingMonth;
        private DateTime currentViewingDate;

        public static readonly DependencyProperty CurrentDateProperty = DependencyProperty.Register("CurrentDate", typeof (DateTime), typeof (Calendar));

        #region Event
        public event PropertyChangedEventHandler CurrentlyViewingInfoChanged; // used to update currently viewing year & month
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DayChangedEventArgs> DayChanged;
        #endregion
        
        #region Properties
        // TODO: monthly view, weekly view (, daily view)
        public CalenderViewingMode ViewingMode { get; set; }

        public ObservableCollection<Day> Days { get; set; }
        public ObservableCollection<string> DayNames { get; set; }

        public EventManager EventManager { get; set; }

        public DateTime CurrentDate
        {
            get { return (DateTime) GetValue(CurrentDateProperty); }
            set { SetValue(CurrentDateProperty, value); }
        }

        public string CurrentDate_Short
        {
            get { return CurrentDate.ToShortDateString(); }
        }

        public DateTime CurrentViewingDate
        {
            get { return currentViewingDate; }
            set
            {
                currentViewingDate = value;
                if (CurrentlyViewingInfoChanged != null) CurrentlyViewingInfoChanged(this, new PropertyChangedEventArgs("CurrentViewingDate"));
            }
        }

        public int CurrentViewingYear
        {
            get { return CurrentViewingDate.Year; }
            set
            {
                currentViewingYear = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("CurrentViewingYear"));
            }
        }

        public int CurrentViewingMonth
        {
            get { return CurrentViewingDate.Month; }
            set
            {
                currentViewingMonth = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("CurrentViewingMonth"));
            }
        }
        #endregion

        static Calendar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (Calendar), new FrameworkPropertyMetadata(typeof (Calendar)));
        }

        public Calendar()
        {
            DataContext = this;
            CurrentDate = DateTime.Today;
            CurrentViewingDate = CurrentDate;
            CurrentlyViewingInfoChanged += this.UpdateCurrentViewingInfo;

            //this won't work in Australia where they start the week with Monday. So remember to test in other 
            //places if you plan on using it. 
            DayNames = new ObservableCollection<string> {"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"};

            Days = new ObservableCollection<Day>();
            EventManager = new EventManager();
            EventManager.NewEventIsAddedEvent += UpdateEventsToCalendar; // update layout when there is a new event added

            ViewingMode = CalenderViewingMode.Monthly;

            BuildCalendar(DateTime.Today);
        }

        public void BuildCalendar(DateTime targetDate)
        {
            Days.Clear();

            //Calculate when the first day of the month is and work out an 
            //offset so we can fill in any boxes before that.
            DateTime d = new DateTime(targetDate.Year, targetDate.Month, 1);
            int offset = DayOfWeekNumber(d.DayOfWeek);
            offset = (offset == 0) ? 7 : offset;

            //if (offset != 0)  // BUG: if offset is not 0, beginning date should be modified.
            d = d.AddDays(-offset);

            //Show 6 weeks each with 7 days = 42
            for (int box = 1; box <= 42; box++)
            {
                Day day = new Day {Date = d, Enabled = true, IsTargetMonth = targetDate.Month == d.Month};
                day.PropertyChanged += Day_Changed;
                day.IsToday = d == DateTime.Today; 
                Days.Add(day);
                d = d.AddDays(1);
            }

            foreach (IEvent evnt in EventManager.EventDB)
            {
                UpdateEventsToCalendar(evnt);
            }
        }

        private void Day_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "EventTexts") return;
            if (DayChanged == null) return;

            DayChanged(this, new DayChangedEventArgs(sender as Day));
        }

        // Event subscriber (CurrentlyViewingInfoChanged)
        private void UpdateCurrentViewingInfo(object sender, PropertyChangedEventArgs e)
        {
            CurrentViewingYear = CurrentViewingDate.Year;
            CurrentViewingMonth = CurrentViewingDate.Month;
        }

        private static int DayOfWeekNumber(DayOfWeek dow)
        {
            return Convert.ToInt32(dow.ToString("D"));
        }

        private void RefreshCalendar(int offset)
        {
            CurrentViewingDate = CurrentViewingDate.AddMonths(offset);
            DateTime targetDate = new DateTime(CurrentViewingDate.Year, CurrentViewingDate.Month, 1);
            this.BuildCalendar(targetDate);     // Day of the beginning date should be 1
        }

        private void UpdateEventsToCalendar(IEvent evnt)
        {
            if (!Utils.IsDayInRange(CurrentViewingDate, evnt.BeginningTime, ViewingMode))
                return;

            switch (ViewingMode)
            {
                case CalenderViewingMode.Daily:
                    break;
                case CalenderViewingMode.Weekly:
                    break;
                case CalenderViewingMode.Monthly:
                    UpdateEventsMonthlyView(evnt);
                    break;
                default:
                    break;
            }
        }

        private void UpdateEventsMonthlyView(IEvent target)
        {
            int offset = (target.BeginningTime.Month == Days[0].Date.Month) ? 0 : 
                DateTime.DaysInMonth(Days[0].Date.Year, Days[0].Date.Month) - Days[0].Date.Day;
            //Days[targett.BeginningTime.Day + offset].EventTexts.Add(target.EventText);

            // TODO: improve this
            List<DateTime> recurringDates = Utils.FindAllRecurringDate(target, ViewingMode);
            foreach(DateTime dt in recurringDates)
            {
                Days[dt.Day + offset].EventTexts.Add(target.EventText);
            }

            // TODO: add a event for remove / gray out those events have ended already

        }

        #region Public methods for user
        public void MoveToPrevMonth()
        {
            RefreshCalendar(-1);
        }

        public void MoveToNextMonth()
        {
            RefreshCalendar(1);
        }

        public void MoveToCurrentMonth()
        {
            CurrentViewingDate = CurrentDate;
            DateTime targetDate = new DateTime(CurrentViewingDate.Year, CurrentViewingDate.Month, 1);
            this.BuildCalendar(targetDate);
        }
        #endregion
    }

    public class DayChangedEventArgs : EventArgs
    {
        public Day Day { get; private set; }

        public DayChangedEventArgs(Day day)
        {
            this.Day = day;
        }
    }

    public enum CalenderViewingMode
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2
    }
}