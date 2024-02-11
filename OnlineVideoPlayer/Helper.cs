using System.IO;
using System.Text.RegularExpressions;

namespace OnlineVideoPlayer
{
    internal class Helper
    {
        public static bool IsHttpsLink(string Link)
        {
            if (Link == null) return false;

            return (Link.ToLower().StartsWith("http") & Link.ToLower().Contains("://"));
        }
        public static bool IsYoutubeLink(string Link)
        {
            if (Link == null) return false;

            return IsHttpsLink(Link) & Link.Contains("youtu") & (Link.Contains(".com") | Link.Contains(".be"));
        }
        public static string Removeillegal(string var)
        {
            return new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())))).Replace(var, "");
        }
    }
}
