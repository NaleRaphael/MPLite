using System;
<<<<<<< HEAD
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
=======
using System.IO;
using Newtonsoft.Json;
>>>>>>> rev8d0858e

namespace Jarloo.Calendar
{
    internal static class DataControl
    {
        public static void SaveData<T>(string filePath, Object obj) where T : class
        {
<<<<<<< HEAD
            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                ConvertToJson<T>(obj).WriteTo(jsonWriter);
=======
            JsonSerializer serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.All;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = File.CreateText(filePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, obj);
>>>>>>> rev8d0858e
            }
        }

        public static T ReadFromJson<T>(string filePath) where T : class
        {
<<<<<<< HEAD
            JObject result;
            try
            {
                using (StreamReader sr = File.OpenText(filePath))
                using (JsonReader jsonReader = new JsonTextReader(sr))
                {
                    result = (JObject)JToken.ReadFrom(jsonReader);
=======
            T result;
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.TypeNameHandling = TypeNameHandling.All;

                using (StreamReader sr = File.OpenText(filePath))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    result = (T)serializer.Deserialize(reader);
>>>>>>> rev8d0858e
                }
            }
            catch (Exception ex)
            {
                return null;
            }
<<<<<<< HEAD
            return result.ToObject<T>();
        }

        public static JObject ConvertToJson<T>(Object obj)
        {
            return (JObject)JToken.FromObject((T)obj);
=======
            return result;
>>>>>>> rev8d0858e
        }
    }
}
