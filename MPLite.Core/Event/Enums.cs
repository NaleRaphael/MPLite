using System;

namespace MPLite.Event
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

    public enum CalendarViewingMode
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2
    }
}
