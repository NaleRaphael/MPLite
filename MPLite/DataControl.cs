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
        public static void SaveData(string filePath, Playlist obj)
        {
            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                PlaylistCollection playLists = new PlaylistCollection();
                playLists.TrackLists.Add(obj);
                ConvertToJson<PlaylistCollection>(playLists).WriteTo(jsonWriter);
            }
        }

        public static void UpdateDatabase(string filePath, Playlist obj)
        {
            // STEP_01: convert JSON file to a collection
            JObject db = ReadFromJson(filePath);
            if (db == null)
            {
                SaveData(filePath, obj);
            }
            else
            {
                IList<PlaylistCollection> playlist = db.ToObject<IList<PlaylistCollection>>();
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
