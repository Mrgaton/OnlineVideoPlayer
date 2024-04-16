using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HttpMethod = System.Net.Http.HttpMethod;

namespace OnlineVideoPlayer
{
    internal class DiscordAttachements
    {
        public static async Task<string> RefreshUrl(string url) => (await RefreshUrls([url]))[0];

        public static async Task<string[]> RefreshUrls(string[] urls)
        {
            if (urls.Length > 50) throw new Exception("urls length is bigger than 50");

            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "https://gato.ovh/attachments/refresh-urls"))
            {
                var data = JsonSerializer.Serialize(new Dictionary<string, object>() { { "attachment_urls", urls } });

                message.Content = new StringContent(data);

                using (HttpResponseMessage response = await new HttpClient().SendAsync(message))
                {
                    var str = await response.Content.ReadAsStringAsync();

                    //Console.WriteLine(str);

                    return JsonNode.Parse(str)["refreshed_urls"].AsArray().Select(element => (string)element["refreshed"]).ToArray();
                }
            }
        }
    }
}