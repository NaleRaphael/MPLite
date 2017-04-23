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
using System.Linq;
using Jarloo.Calendar.Themes;

namespace Jarloo.Calendar
{
    using CustomEvent = MPLite.Event.CustomEvent;
    using EventManager = MPLite.Event.EventManager;
    using Utils = MPLite.Event.Utils;
    using CalendarViewingMode = MPLite.Event.CalendarViewingMode;
    using IEvent = MPLite.Event.IEvent;
    using IEventHandlerFactory = MPLite.Event.IEventHandlerFactory;
    
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
        public CalendarViewingMode ViewingMode { get; set; }

        public ObservableCollection<Day> Days { get; set; }
        public ObservableCollection<string> DayNames { get; set; }

        public EventManager EventManager { get; set; }
        //public IEventHandlerFactory EventHandlerFacotry { get; set; }

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

            ViewingMode = CalendarViewingMode.Monthly;
        }

        // Workaround: controls cannot be initialized with parameters, so that EventManager is created after Calendar has been initialized.
        public void OnInitialization(IEventHandlerFactory handlerFactory)
        {
            EventManager = new EventManager(handlerFactory);
            EventManager.EventIsAddedEvent += AddEventsToCalendar; // update layout when there is a new event added
            EventManager.EventIsDeletedEvent += DeleteEventsToCalendar;

            BuildCalendar(DateTime.Today);

            Generic.DayContentSelectionEvent += SelectedDayContentActionEntry;
            Generic.NewEventIsCreatedEvent += OnRecievingNewEventObject;
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

            AddEventsToCalendar(null);
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

        private void OnRecievingNewEventObject(CustomEvent ce)
        {
            EventManager.AddEvent(ce);
        }

        private void RefreshCalendar(int offset)
        {
            CurrentViewingDate = CurrentViewingDate.AddMonths(offset);
            DateTime targetDate = new DateTime(CurrentViewingDate.Year, CurrentViewingDate.Month, 1);
            this.BuildCalendar(targetDate);     // Day of the beginning date should be 1
        }

        // TODO: rewrite this (parameter IEvent is not used)
        private void AddEventsToCalendar(IEvent evnt)
        {
            RefreshEventsToCalendarEntry(evnt, true);
        }

        private void DeleteEventsToCalendar(IEvent evnt)
        {
            RefreshEventsToCalendarEntry(evnt, false);
        }

        private void RefreshEventsToCalendarEntry(IEvent evnt, bool addOrDelete)
        {
            switch (ViewingMode)
            {
                case CalendarViewingMode.Daily:
                    // TODO
                    break;
                case CalendarViewingMode.Weekly:
                    // TODO
                    break;
                case CalendarViewingMode.Monthly:
                    if (evnt == null)   // No event is given, read from database
                    {
                        foreach (IEvent e in EventManager.EventDB)
                        {
                            UpdateEventsMonthlyView(e, addOrDelete);
                        }
                    }
                    else
                    {
                        UpdateEventsMonthlyView(evnt, addOrDelete);
                    }
                    break;
                default:
                    break;
            }
        }

        private void UpdateEventsMonthlyView(IEvent target, bool addOrDel)
        {
            int offset = DateTime.DaysInMonth(Days[0].Date.Year, Days[0].Date.Month) - Days[0].Date.Day;

            // TODO: improve this
            List<DateTime> recurringDates = Utils.FindAllRecurringDate(target, CurrentViewingDate, ViewingMode);
            foreach(DateTime dt in recurringDates)
            {
                if (addOrDel)
                    Days[dt.Day + offset].Events.Add(target);
                else
                {
                    var temp = Days[dt.Day + offset].Events;
                    temp.Remove(temp.Where(x => x.GUID == target.GUID).FirstOrDefault());
                }
                    
            }

            // TODO: add a event for remove / gray out those events have ended already
        }

        private void SelectedDayContentActionEntry(IEvent evnt, SelectedDayContentActions action)
        {
            if (evnt == null) return;

            switch (action)
            {
                case SelectedDayContentActions.ShowInfo:
                    MessageBox.Show(evnt.EventText);
                    break;
                case SelectedDayContentActions.AddEvent:
                    EventManager.AddEvent((CustomEvent)evnt);
                    break;
                case SelectedDayContentActions.DeleteEvent:
                    EventManager.DeleteEvent(evnt.GUID);
                    break;
                default:
                    break;
            }
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
    /*
    public enum CalendarViewingMode
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2
    }*/
}