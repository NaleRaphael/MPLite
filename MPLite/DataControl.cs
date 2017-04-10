using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public static void SaveData<T>(string filePath, T obj) where T : class
        {
            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                ConvertToJson<T>(obj).WriteTo(jsonWriter);
            }
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
            catch (Exception ex)    // Empty content
            {
                return null;
            }
            return result;
        }

        public static T ReadFromJson<T>(string filePath) where T : class
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
            catch (Exception ex)    // Empty content
            {
                return null;
            }
            return result.ToObject<T>();
        }

        //public static JObject ConvertFromJson()
        //{
        //    return;
        //}

        // ref: http://www.newtonsoft.com/json/help/html/FromObject.htm
        public static JObject ConvertToJson<T>(Object obj)
        {
            return (JObject)JToken.FromObject((T)obj);
        }
    }

    public class EmptyJsonFileException : Exception
    {
        public EmptyJsonFileException(string message) : base(message)
        {
        }
    }
}
