using System;
using System.Collections.Generic;

namespace MPLite.Event
{
    using DataControl = Core.DataControl;
    using Properties = Core.Properties;
    public class EventCollection
    {
        public List<IEvent> EventList { get; set; }

        public delegate void DatabaseIsChangedEventHandler();
        public event DatabaseIsChangedEventHandler DatabaseIsChanged;

        public EventCollection()
        {
        }

        public static EventCollection GetDataBase()
        {
            string dbPath = Properties.Settings.Default.EventDBPath;
            return DataControl.ReadFromJson<EventCollection>(dbPath, true);
        }

        public void Initialize()
        {
            string dbPath = Properties.Settings.Default.EventDBPath;
            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                ec = new EventCollection();
                ec.EventList = new List<IEvent>();
            }
            this.EventList = ec.EventList;
            ec = null;
        }

        public void AddEvent(IEvent target)
        {
            string dbPath = Properties.Settings.Default.EventDBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                ec = new EventCollection();
                ec.EventList = new List<IEvent>();
            }

            ec.EventList.Add(target);
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this, true, true);
            ec = null;

            DatabaseIsChanged();
        }

        public void DeleteEvent(CustomEvent target)
        {
            string dbPath = Properties.Settings.Default.EventDBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                return;     // TODO: check this in PlaylistCollection, if plc == null, it should return directly.
            }

            ec.EventList.Remove(ec.EventList.Find(x => x.GUID == target.GUID));
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this, true, true);
            ec = null;

            // Notify subscriber
            DatabaseIsChanged();
        }

        public void DeleteEvent(Guid targetGUID)
        {
            string dbPath = Properties.Settings.Default.EventDBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                return;
            }

            ec.EventList.Remove(ec.EventList.Find(x => x.GUID == targetGUID));
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this, true, true);
            ec = null;

            // Notify subscriber
            DatabaseIsChanged();
        }

        public void UpdateEvent(IEvent evnt)
        {
            string dbPath = Properties.Settings.Default.EventDBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                return;
            }

            int i = ec.EventList.FindIndex(x => x.GUID == evnt.GUID);
            if (i == -1) return;
            ec.EventList[i] = evnt;
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this, true, true);
            ec = null;
        }

        public void UpdateEvent<T>(T evnt) where T : IEvent
        {
            string dbPath = Properties.Settings.Default.EventDBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                return;
            }
            
            int idx = ec.EventList.FindIndex(x => x.GUID == evnt.GUID);
            if (idx == -1)
                return;
            ec.EventList.RemoveAt(idx);
            ec.EventList.Insert(idx, evnt);
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this, true, true);
            ec = null;
        }

        // TODO: directly overwrite an empty list into database? (without reading the original one)
        public void Clear()
        {
            string dbPath = Properties.Settings.Default.EventDBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                return;
            }

            ec.EventList.Clear();
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, ec, true, true);
            ec = null;

            // Notify subscriber
            DatabaseIsChanged();
        }

        public IEvent GetEvent(Guid targetGUID)
        {
            string dbPath = Properties.Settings.Default.EventDBPath;
            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath, true);
            if (ec == null)
            {
                ec = new EventCollection();
            }

            IEvent result;
            try
            {
                result = ec.EventList.Find(x => x.GUID == targetGUID);
            }
            catch
            {
                throw;
            }
            return result;
        }
    }
}
