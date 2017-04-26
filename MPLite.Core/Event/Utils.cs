using System;
using System.Collections.Generic;

namespace MPLite.Event
{
    public static class WeekdayExtension
    {
        public static Weekday ToCustomWeekday(this DateTime date)
        {
            Weekday wd;
            if (Enum.TryParse<Weekday>(date.DayOfWeek.ToString(), out wd))
                return wd;
            else throw new Exception("Failed to convert the weekday of given date");
        }

        public static Weekday ToCustomWeekday(this DayOfWeek wd)
        {
            Weekday result;
            if (Enum.TryParse<Weekday>(wd.ToString(), out result))
                return result;
            else throw new Exception("Failed to convert the weekday of given date");
        }

        public static DayOfWeek ToSystemWeekday(this Weekday wd)
        {
            DayOfWeek result;
            if (Enum.TryParse<DayOfWeek>(wd.ToString(), out result))
                return result;
            else throw new Exception("Failed to covnert custom weekday to system weekday");
        }
    }

    public static class DateTimeExtension
    {
        public static DateTime Floor(this DateTime dt, TimeSpan unit)
        {
            long times = dt.Ticks / unit.Ticks;
            return new DateTime(times * unit.Ticks);
        }

        public static DateTime Round(this DateTime dt, TimeSpan unit)
        {
            long times = (dt.Ticks + (unit.Ticks / 2) + 1) / unit.Ticks;
            return new DateTime(times * unit.Ticks);
        }

        public static DateTime Ceiling(this DateTime dt, TimeSpan unit)
        {
            long times = (dt.Ticks + unit.Ticks - 1) / unit.Ticks;
            return new DateTime(times * unit.Ticks);
        }
    }

    public static class Utils
    {
        public static Weekday ConvertToCustomWeekday(DateTime date)
        {
            Weekday wd;
            if (Enum.TryParse<Weekday>(date.DayOfWeek.ToString(), out wd))
                return wd;
            else throw new Exception("Failed to convert the weekday of given date");
        }

        public static DateTime DateOfNextWeekday(DateTime today, Weekday wd)
        {
            int diff = today.DayOfWeek - DayOfWeek.Sunday;
            return today.AddDays(1);
        }

        public static Weekday GetNextRecurringWeekday(DayOfWeek targetWeekday, RecurringFrequencies rf)
        {
            if (rf == RecurringFrequencies.None)
                return Weekday.None;    // Or throw exception?

            byte nextRecurringWeekday = (byte)targetWeekday.ToCustomWeekday();
            byte targetRF = (byte)rf;

            while (nextRecurringWeekday < 0xF0)    // 0x80: Weekday.Unknown (exceeds the range of weekdays)
            {
                //nextRecurringWeekday <<= 1;
                if (nextRecurringWeekday == 0x00)   // Restart from Sunday (first day of the week)
                    nextRecurringWeekday = 0x01;
                if ((nextRecurringWeekday & targetRF) == 0)
                {
                    nextRecurringWeekday <<= 1;
                    continue;
                }
                else break;
            }
            return (Weekday)nextRecurringWeekday;
        }

        public static DateTime DateTimeOfNextWeekday(DateTime date, DayOfWeek target)
        {
            DayOfWeek today = date.DayOfWeek;
            int diff = (target - today <= 0) ? target - today + 7 : target - today;
            return date.AddDays(diff);
        }

        public static void FindRangeOfWeek(DateTime date, out DateTime beginningDate, out DateTime endingDate)
        {
            int offset = (int)date.DayOfWeek;
            beginningDate = date.AddDays(-offset);
            endingDate = date.AddDays(7 - offset).AddSeconds(-1);
        }

        public static void FindRangeOfMonth(DateTime date, out DateTime beginningDate, out DateTime endingDate)
        {
            beginningDate = new DateTime(date.Year, date.Month, 1);
            endingDate = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }

        public static bool IsDayInRange(DateTime source, DateTime target, CalendarViewingMode mode)
        {
            if (source.Year != target.Year) return false;

            if (mode == CalendarViewingMode.Monthly)
                return (source.Month == target.Month) ? true : false;
            else if (mode == CalendarViewingMode.Daily)
                return (source.Day == target.Day) ? true : false;
            else if (mode == CalendarViewingMode.Weekly)
            {
                DateTime wkb;
                DateTime wke;
                FindRangeOfWeek(source, out wkb, out wke);

                // or compare them by converting into OADate
                return (target >= wkb && target < wke) ? true : false;
            }
            else return false;      // or throw exception (Invalid CalendarViewingMode)
        }

        public static List<DateTime> FindAllRecurringDate(IEvent target, DateTime ViewingDate, CalendarViewingMode mode)
        {
            List<DateTime> result = new List<DateTime>();
            DateTime rngb = DateTime.MinValue;   // beginning of range
            DateTime rnge = DateTime.MinValue;   // ending of range
            RecurringFrequencies rf = target.RecurringFrequency;

            switch (mode)
            {
                case CalendarViewingMode.Monthly:
                    FindRangeOfMonth(ViewingDate, out rngb, out rnge);
                    break;
                case CalendarViewingMode.Weekly:
                    FindRangeOfWeek(ViewingDate, out rngb, out rnge);
                    break;
                default:
                    break;
            }

            // Target is not a recurring event
            if (target.RecurringFrequency == RecurringFrequencies.None)
            {
                if (target.OriginalBeginningTime > rngb && target.OriginalBeginningTime < rnge)
                    result.Add(target.OriginalBeginningTime);
                return result;
            }

            if (target.ThisDayForwardOnly && target.OriginalBeginningTime > rnge)
                return result;

            DateTime iter = target.ThisDayForwardOnly ? ((target.OriginalBeginningTime > rngb) ? target.OriginalBeginningTime : rngb) : rngb;
            for (; iter <= rnge; iter = iter.AddDays(1))
            {
                if (((byte)iter.DayOfWeek.ToCustomWeekday() & (byte)rf) >= 1)
                    result.Add(iter);
            }

            return result;
        }
    }
}
