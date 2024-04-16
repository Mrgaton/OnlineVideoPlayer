using System.IO;
using System.Text.RegularExpressions;

namespace OnlineVideoPlayer
{
    internal static class Helper
    {
        public static bool IsHttpsLink(string link)
        {
            if (link == null) return false;

            return link.ToLower().StartsWith("http", System.StringComparison.InvariantCultureIgnoreCase) && link.ToLower().Contains("://");
        }

        public static bool IsYoutubeLink(string link)
        {
            if (link == null) return false;

            return IsHttpsLink(link) && link.Contains("youtu") && (link.Contains(".com") || link.Contains(".be"));
        }

        public static string Removeillegal(string var)
        {
            return new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())))).Replace(var, "");
        }
    }
}