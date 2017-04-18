using System;
<<<<<<< HEAD
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
=======
using System.Collections.Generic;
>>>>>>> rev8d0858e

namespace Jarloo.Calendar
{
    public class EventCollection
    {
        public List<CustomEvent> EventList { get; set; }

<<<<<<< HEAD
=======
        public delegate void DatabaseIsChangedEventHandler();
        public event DatabaseIsChangedEventHandler DatabaseIsChanged;

>>>>>>> rev8d0858e
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
<<<<<<< HEAD
=======

            DatabaseIsChanged();
>>>>>>> rev8d0858e
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
<<<<<<< HEAD
=======

            // Notify subscriber
            DatabaseIsChanged();
        }

        public void DeleteEvent(Guid targetGUID)
        {
            string dbPath = Properties.Settings.Default.DBPath;

            EventCollection ec = DataControl.ReadFromJson<EventCollection>(dbPath);
            if (ec == null)
            {
                return;
            }

            ec.EventList.Remove(ec.EventList.Find(x => x.GUID == targetGUID));
            this.EventList = ec.EventList;
            DataControl.SaveData<EventCollection>(dbPath, this);
            ec = null;

            // Notify subscriber
            DatabaseIsChanged();
>>>>>>> rev8d0858e
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
<<<<<<< HEAD
        }

        public CustomEvent GetEvent(string eventText)
=======

            // Notify subscriber
            DatabaseIsChanged();
        }

        public CustomEvent GetEvent(Guid targetGUID)
>>>>>>> rev8d0858e
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
<<<<<<< HEAD
                result = ec.EventList.Find(x => x.EventText == eventText);
=======
                result = ec.EventList.Find(x => x.GUID == targetGUID);
>>>>>>> rev8d0858e
            }
            catch
            {
                throw;
            }
            return result;
        }
    }
}
