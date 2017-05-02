using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MPLite.Core
{
    public static class DataControl
    {
        public static void SaveData<T>(string filePath, Object obj, bool usingTypeNameHandling) where T : class
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.TypeNameHandling = usingTypeNameHandling ? TypeNameHandling.All : TypeNameHandling.None;
            serializer.Formatting = usingTypeNameHandling ? Formatting.Indented : Formatting.None;

            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, obj);
            }
        }

        public static T ReadFromJson<T>(string filePath, bool usingTypeNameHandling) where T : class
        {
            T result;
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.TypeNameHandling = usingTypeNameHandling ? TypeNameHandling.All : TypeNameHandling.None;

                using (StreamReader sr = File.OpenText(filePath))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    result = (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                JObject temp;
                try
                {
                    using (StreamReader sr = File.OpenText(filePath))
                    using (JsonTextReader jsonReader = new JsonTextReader(sr))
                    {
                        temp = (JObject)JToken.ReadFrom(jsonReader);
                    }
                }
                catch (Exception ex2)    // Empty content
                {
                    return null;
                }
                return temp.ToObject<T>();
            }
            return result;
        }
    }

    public class EmptyJsonFileException : Exception
    {
        public EmptyJsonFileException(string message) : base(message)
        {
        }
    }
}
