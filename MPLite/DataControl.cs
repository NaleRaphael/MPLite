using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MPLite
{
    public static class DataControl
    {
        // WriteJsonToFile
        public static void SaveData(string filePath, PlaylistCollection obj)
        {
            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                ConvertToJson<PlaylistCollection>(obj).WriteTo(jsonWriter);
            }
        }

        public static void UpdateDatabase(string filePath, Playlist obj)
        {
            // STEP_01: convert JSON file to a collection
            JObject db = ReadFromJson(filePath);
            if (db == null)
            {
                PlaylistCollection plc = new PlaylistCollection();
                plc.TrackLists.Add(obj);
                SaveData(filePath, plc);
            }
            else
            {
                PlaylistCollection plc = db.ToObject<PlaylistCollection>();
                Playlist pl = plc.TrackLists.Find(x => x.ListName == obj.ListName);
                foreach (TrackInfo track in obj.Soundtracks)
                {
                    pl.Soundtracks.Add(track);
                }
                SaveData(filePath, plc);
                //pl.Soundtracks.Add(obj.Soundtracks);
                //pl.Soundtracks.Add();
            }

            // STEP_02: add data into it


            // STEP_03: convert collection to JSON file
        }

        public static JObject ReadFromJson(string filePath)
        {
            JObject result;
            try
            {
                using (StreamReader sr = File.OpenText(filePath))
                using (JsonTextReader jsonReader = new JsonTextReader(sr))
                {
                    result = (JObject)JToken.ReadFrom(jsonReader);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return result;
        }

        //public static JObject ConvertFromJson()
        //{
        //    return;
        //}

        // http://www.newtonsoft.com/json/help/html/FromObject.htm
        public static JObject ConvertToJson<T>(Object obj)
        {
            return (JObject)JToken.FromObject((T)obj);
        }
    }
}
