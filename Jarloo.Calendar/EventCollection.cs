using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jarloo.Calendar
{
    public class EventCollection
    {
        public List<CustomEvent> EventList { get; set; }

        public EventCollection()
        {
        }

        public void Initialize()
        {
            string dbPath = Properties.Settings.Default.DBPath;
            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath);
            if (ec == null)
            {
                ec = new EventCollection();
                ec.EventList = new List<CustomEvent>();
            }
            this.EventList = ec.EventList;
            ec = null;
        }

        public void AddEvent(CustomEvent target)
        {
            string dbPath = Properties.Settings.Default.DBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath);
            if (ec == null)
            {
                ec = new EventCollection();
                ec.EventList = new List<CustomEvent>();
            }

            ec.EventList.Add(target);
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this);
            ec = null;
        }

        public void DeleteEvent(CustomEvent target)
        {
            string dbPath = Properties.Settings.Default.DBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath);
            if (ec == null)
            {
                return;     // TODO: check this in PlaylistCollection, if plc == null, it should return directly.
            }

            ec.EventList.Remove(ec.EventList.Find(x => x.GUID == target.GUID));
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this);
            ec = null;
        }

        // TODO: directly overwrite an empty list into database? (without reading the original one)
        public void Clear()
        {
            string dbPath = Properties.Settings.Default.DBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath);
            if (ec == null)
            {
                return;
            }

            ec.EventList.Clear();
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, ec);
            ec = null;
        }

        public CustomEvent GetEvent(string eventText)
        {
            string dbPath = Properties.Settings.Default.DBPath;
            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath);
            if (ec == null)
            {
                ec = new EventCollection();
            }

            CustomEvent result;
            try
            {
                result = ec.EventList.Find(x => x.EventText == eventText);
            }
            catch
            {
                throw;
            }
            return result;
        }
    }
}
