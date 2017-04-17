using System;
using System.IO;
using Newtonsoft.Json;

namespace Jarloo.Calendar
{
    internal static class DataControl
    {
        public static void SaveData<T>(string filePath, Object obj) where T : class
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.All;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, obj);
            }
        }

        public static T ReadFromJson<T>(string filePath) where T : class
        {
            T result;
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.All;

                using (StreamReader sr = File.OpenText(filePath))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    result = (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return result;
        }
    }
}
