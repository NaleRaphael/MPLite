using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MPLite.Core
{
    public static class DataControl
    {
        /*
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
        }*/

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
            /*
            if (usingTypeNameHandling)
            {
                return ReadFromJson_Serializer<T>(filePath);
            }
            else
            {
                return ReadFromJson_Linq<T>(filePath);
            }*/

            
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
        
        /*
        private static T ReadFromJson_Linq<T>(string filePath) where T : class
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

        private static T ReadFromJson_Serializer<T>(string filePath) where T : class
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
        }*/
    }

    public class EmptyJsonFileException : Exception
    {
        public EmptyJsonFileException(string message) : base(message)
        {
        }
    }
}
