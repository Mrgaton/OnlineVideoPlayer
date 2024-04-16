using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnlineVideoPlayer
{
    internal static class DataDownloader
    {
        public static async Task<byte[]> CustomDataDownloadAsync(this WebClient wc, string url)
        {
            wc.Headers.Add("referer", VideoPlayer.ServerUrl);
            wc.Headers.Add("user-agent", Application.ProductName);

            if (url.StartsWith("https://cdn.discordapp.com", StringComparison.InvariantCultureIgnoreCase)) url = await DiscordAttachements.RefreshUrl(url);

            return await wc.DownloadDataTaskAsync(new Uri(url));
        }
    }
}