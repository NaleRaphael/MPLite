using System;

namespace Jarloo.Calendar
{
    [Flags]
    public enum RecurringFrequencies
    {
        None = 0x00,
        EverySunday = 0x01,
        EveryMonday = 0x02,
        EveryTuesday = 0x04,
        EveryWednesday = 0x08,
        EveryThursday = 0x10,
        EveryFriday = 0x20,
        EverySaturday = 0x40,
        EveryWeekday = EveryMonday | EveryTuesday | EveryWednesday | EveryThursday | EveryFriday,
        EveryWeekend = EverySunday | EverySaturday,
        Daily = EveryWeekday | EveryWeekend,
        Monthly = 0x80,
        Yearly = 0x100,
        Custom = 0x200
    }

    [Flags]
    public enum Weekday
    {
        None = 0x00,
        Sunday = 0x01,
        Monday = 0x02,
        Tuesday = 0x04,
        Wednesday = 0x08,
        Thursday = 0x10,
        Friday = 0x20,
        Saturday = 0x40,
        Unknown = 0x80
    }

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

        public static Weekday GetNextRecurringWeekday(Weekday wd, RecurringFrequencies rf)
        {
            if (rf == RecurringFrequencies.None)
                return Weekday.None;    // Or throw exception?

            byte nextRecurringWeekday = (byte)wd;
            byte target = (byte)rf;

            while (nextRecurringWeekday < 0xF0)    // 0x80: Weekday.Unknown (exceeds the range of weekdays)
            {
                nextRecurringWeekday <<= 1;
                if (nextRecurringWeekday == 0x00)   // Restart from Sunday (first day of the week)
                    nextRecurringWeekday = 0x01;
                if ((nextRecurringWeekday & target) == 0)
                {
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
    }
}
