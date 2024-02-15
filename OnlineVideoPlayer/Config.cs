using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OnlineVideoPlayer
{
    internal static class Config
    {
        private static void WriteConfig(byte[] fileData) => File.WriteAllBytes(Program.VideoPlayerConfigPath, VideoPlayer.Compress(fileData));

        private static byte[] ReadConfig() => VideoPlayer.Decompress(File.ReadAllBytes(Program.VideoPlayerConfigPath));

        public static T GetConfig<T>(string keyName, T defaultValue = default)
        {
            if (!File.Exists(Program.VideoPlayerConfigPath)) return defaultValue;
            

            try
            {
                var jsonObj = JsonNode.Parse(ReadConfig()).AsObject();

                if (!jsonObj.Any(Key => Key.Key == keyName)) return defaultValue;

                return JsonSerializer.Deserialize<T>(jsonObj[keyName].ToString(), JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (File.Exists(Program.VideoPlayerConfigPath))
                {
                    File.Delete(Program.VideoPlayerConfigPath);
                }

                throw ex;
            }
        }

        public static void SaveConfig(string keyName, object objectData)
        {
            if (!File.Exists(Program.VideoPlayerConfigPath)) File.WriteAllBytes(Program.VideoPlayerConfigPath, VideoPlayer.Compress(Encoding.UTF8.GetBytes("{\"test\":\"True\"}")));
            

            try
            {
                var jsonObj = JsonNode.Parse(ReadConfig()).AsObject();

                if (jsonObj[keyName] == null) jsonObj.Add(keyName, "");

                jsonObj[keyName] = JsonSerializer.Serialize(objectData, JsonSerializerOptions);

                WriteConfig(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(jsonObj)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (File.Exists(Program.VideoPlayerConfigPath)) File.Delete(Program.VideoPlayerConfigPath);
                
                throw ex;
            }
        }

        private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = null,
            WriteIndented = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}