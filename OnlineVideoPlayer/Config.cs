using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace OnlineVideoPlayer
{
    internal class Config
    {
        private static void WriteConfig(byte[] FileData) => File.WriteAllBytes(Program.VideoPlayerConfigPath, VideoPlayer.Compress(FileData));
        private static byte[] ReadConfig() => VideoPlayer.Decompress(File.ReadAllBytes(Program.VideoPlayerConfigPath));

        public static T GetConfig<T>(string KeyName, T DefaultValue = default)
        {
            if (!File.Exists(Program.VideoPlayerConfigPath))
            {
                return DefaultValue;
            }

            try
            {
                var jsonObj = JsonNode.Parse(ReadConfig()).AsObject();

                if (!jsonObj.Any(Key => Key.Key == KeyName))
                {
                    return DefaultValue;
                }

                return JsonSerializer.Deserialize<T>(jsonObj[KeyName].ToString(), JsonSerializerOptions);
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
        public static void SaveConfig(string KeyName, object ObjectData)
        {
            if (!File.Exists(Program.VideoPlayerConfigPath))
            {
                File.WriteAllBytes(Program.VideoPlayerConfigPath, VideoPlayer.Compress(Encoding.UTF8.GetBytes("{\"Test\":\"True\"}")));
            }

            try
            {
                var jsonObj = JsonNode.Parse(ReadConfig()).AsObject();

                if (jsonObj[KeyName] == null)
                {
                    jsonObj.Add(KeyName, "");
                }

                jsonObj[KeyName] = JsonSerializer.Serialize(ObjectData, JsonSerializerOptions);

                WriteConfig(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(jsonObj)));
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


        private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = null,
            WriteIndented = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}
