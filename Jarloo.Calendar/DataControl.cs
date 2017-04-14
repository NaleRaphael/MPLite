using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jarloo.Calendar
{
    internal static class DataControl
    {
        public static void SaveData<T>(string filePath, Object obj) where T : class
        {
            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                ConvertToJson<T>(obj).WriteTo(jsonWriter);
            }
        }

        public static T ReadFromJson<T>(string filePath) where T : class
        {
            //JObject result;
            T result;
            try
            {
                using (StreamReader sr = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    result = (T)serializer.Deserialize(sr, typeof(T));
                }
                /*
                using (JsonReader jsonReader = new JsonTextReader(sr))
                {
                    result = (JObject)JToken.ReadFrom(jsonReader);
                }*/
            }
            catch (Exception ex)
            {
                return null;
            }
            //return result.ToObject<T>();
            return result;
        }

        public static JObject ConvertToJson<T>(Object obj)
        {
            return (JObject)JToken.FromObject((T)obj);
        }
    }
}
