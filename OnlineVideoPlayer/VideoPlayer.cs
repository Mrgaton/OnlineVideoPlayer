using AngleSharp.Common;
using Microsoft.Win32;
using OnlineVideoPlayer.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Input;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Application = System.Windows.Forms.Application;
using Image = System.Drawing.Image;

namespace OnlineVideoPlayer
{
    public partial class VideoPlayer : Form
    {
        [DllImport("winmm.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)] private static extern uint waveOutGetNumDevs();

        private static Screen currentScreen = Screen.PrimaryScreen;

        private static WebClient wc = new WebClient();

        private static string DownloadName = "Pensando {0}";

        private static Size originalSize;

        public static string ServerUrl { get; set; } = "https://gato.ovh/programs/ovp/main.OVP";

        private bool ConectionToHerededServer = false;
        private bool HerededServer = false;
        private string HerededServerUrl = "";

        private static Random Rand = new Random();

        private int PlayerVolume = 60;

        public VideoPlayer()
        {
            InitializeComponent();

            Screen CurrentScreen = Screen.FromControl(this);

            this.Size = new Size(CurrentScreen.Bounds.Width / 2, CurrentScreen.Bounds.Height / 2);

            originalSize = this.Size;

            axMediaPlayer.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(wmp_PlayStateChange);
        }

        private void VideoPlayer_Load(object sender, EventArgs e)
        {
            VideoPanel.Dock = DockStyle.Fill;
            GifPictureBox.Dock = DockStyle.Fill;

            this.Icon = Program.ProgramIco;

            GifPictureBox.Image = Resources.Loading;

            GifPictureBox.BackColor = GetAccentColor();
        }

        private void ChangeFormIconFromUrl(string url)
        {
            Invoke(new MethodInvoker(async () =>
            {
                try
                {
                    bool internetUri = Helper.IsHttpsLink(url);

                    byte[] iconData = null;

                    string linkHash = internetUri ? Program.GetHash(Encoding.Unicode.GetBytes(url)) : null;
                    string tempIconPath = internetUri ? Path.Combine(Program.tempPath, linkHash + Program.TemporaryFilesExtension) : null;

                    Console.WriteLine("Cambiando Icono a " + url);

                    if (File.Exists(tempIconPath) && internetUri)
                    {
                        try
                        {
                            Console.WriteLine("Leyendo el icono de archivos temporales");

                            iconData = Decompress(File.ReadAllBytes(tempIconPath));
                        }
                        catch
                        {
                            File.Delete(tempIconPath);
                        }
                    }
                    else
                    {
                        Console.WriteLine(internetUri ? "Descargando icono" : "Leyendo icono");

                        if (internetUri)
                        {
                            iconData = await wc.CustomDataDownloadAsync(url);

                            File.WriteAllBytes(tempIconPath, Compress(iconData));
                        }
                        else
                        {
                            if (File.Exists(url)) iconData = File.ReadAllBytes(url);
                        }
                    }

                    if (iconData != null)
                    {
                        using (MemoryStream ms = new MemoryStream(iconData))
                        {
                            Program.ProgramIco = Icon.FromHandle(((Bitmap)Image.FromStream(ms)).GetHicon());
                        }
                    }

                    this.Icon = Program.ProgramIco;
                }
                catch
                {
                    Console.WriteLine("Error cambiando el icono");
                }
            }));
        }

        private static bool AlredyCheckedSecond = false;

        private void WaitForPlayTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan tiempoDeEspera = PlayOn - DateTime.Now;

            if (tiempoDeEspera.Ticks >= 0)
            {
                string timeString = tiempoDeEspera.ToString().Split('.')[0];
                string tiempoDeEsperaString = timeString;

                try
                {
                    if (int.Parse(timeString) == 1) tiempoDeEsperaString = timeString + " Dia";
                    if (int.Parse(timeString) >= 2) tiempoDeEsperaString = timeString + " Dias";
                    if ((int.Parse(timeString) / 365) == 1) tiempoDeEsperaString = (int.Parse(timeString) / 365) + " Año";
                    if ((int.Parse(timeString) / 365) >= 2) tiempoDeEsperaString = (int.Parse(timeString) / 365) + " Años";
                }
                catch { }

                this.Text = "Esperando para reproducir el video " + tiempoDeEsperaString;

                Console.WriteLine("");
                Console.WriteLine("Esperando a que se tenga que reproducir el video  (left: " + tiempoDeEspera + ")");

                if (tiempoDeEspera.Seconds.ToString("D2").EndsWith("0") && !AlredyCheckedSecond)
                {
                    AlredyCheckedSecond = true;

                    VideoPlayer_Shown(new object(), new EventArgs());
                }
                else
                {
                    if (!tiempoDeEspera.Seconds.ToString("D2").EndsWith("0")) AlredyCheckedSecond = false;
                }
            }
            else
            {
                FristTimeCheck = true;

                WaitForPlayTimer.Enabled = false;

                VideoPlayer_Shown(new object(), new EventArgs());
            }
        }

        private bool fristTime = true;
        private bool FristTimeCheck = true;
        private bool IsRadio = false;

        private bool paused = false;
        private bool Playing = false;

        private bool VideoReload = false;
        private bool LoopPlaying = false;

        private bool VideoIgnoreInput = false;
        private bool VideoAllowPause = true;
        private bool VideoHideMouse = false;

        private bool VideoFullScreen = false;
        private bool VideoTopMost = false;

        private bool AllowClose = true;
        private bool AllowMinimize = false;
        private bool AllowMaximize = false;
        private bool AllowFullScreen = false;

        private bool EnableVideoViews = true;

        private bool VideoVolumeChanged = false;
        private int VideoVolume = 50;

        private int WindowSizePercentage = 60;

        private bool PlayOnEnabled = false;
        private DateTime PlayOn = new DateTime();

        private int videoSelecionado;
        private string VideoUrl;

        private CancellationTokenSource realTimeVisitsTaskCancelToken = new CancellationTokenSource();

        private Dictionary<string, int> VideoList = new Dictionary<string, int>();
        private Dictionary<string, int> IconList = new Dictionary<string, int>();

        private long VideoVisits = 0;

        private DownloadProgressChangedEventHandler DownloadProgressHandler = null;

        private void ParseBoolElement(ref bool element, string line)
        {
            var value = line.Split('=').Last().Trim();
            if (string.IsNullOrEmpty(value)) return;
            if (bool.TryParse(value, out bool parsedValue))
            {
                element = parsedValue;
            }
        }

        private void ParseNumberElement(ref int element, string line)
        {
            var value = line.Split('=').Last().Trim();
            if (string.IsNullOrEmpty(value)) return;
            if (int.TryParse(value, out int parsedValue))
            {
                element = parsedValue;
            }
        }

        private async void VideoPlayer_Shown(object sender, EventArgs e)
        {
            realTimeVisitsTaskCancelToken.Cancel();

            realTimeVisitsTaskCancelToken = new CancellationTokenSource();

            GifPictureBox.Visible = true;

            if (FristTimeCheck)
            {
                GifPictureBox.Image = Resources.Loading;

                DownloadProgressHandler = new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);

                wc.DownloadProgressChanged += DownloadProgressHandler;
            }
            else
            {
                wc.DownloadProgressChanged -= DownloadProgressHandler;
            }

            try
            {
                VideoList.Clear();
                IconList.Clear();

                string serverData = "";

                Console.WriteLine("");

                if (!Program.ArgsCalled || Helper.IsHttpsLink(Program.ConfigPath))
                {
                    if (Program.ArgsCalled) ServerUrl = Program.ConfigPath;

                    if (!ConectionToHerededServer)
                    {
                        Console.WriteLine("Conectandose con el servidor");

                        DownloadName = "Conectandose al servidor {0}";

                        if (FristTimeCheck) this.Text = "Conectandose al servidor";

                        serverData = await wc.DownloadStringTaskAsync(ServerUrl);
                    }
                    else
                    {
                        Console.WriteLine("Conectandose con el servidor heredado");
                        DownloadName = "Conectandose al servidor heredado {0}";

                        if (FristTimeCheck)
                        {
                            this.Text = "Conectandose al servidor heredado";
                        }

                        serverData = await wc.DownloadStringTaskAsync(HerededServerUrl);
                    }

                    Console.WriteLine("Servidor conectado procesando informacion");
                    Console.WriteLine("");
                }
                else
                {
                    serverData = File.ReadAllText(Program.ConfigPath);
                }

                if (FristTimeCheck) this.Text = "Procesando informacion";

                string line;

                int ln = 0;

                FristTimeCheck = false;

                /*foreach (DictionaryEntry a in Environment.GetEnvironmentVariables())
                {
                    Console.WriteLine(a.Key + "=" + a.Value);
                }
                Console.WriteLine(string.Join("\n", (Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User).Keys).ToDictionary().Select(k => k.Key + '=' + k.Value)));*/

                using (StringReader reader = new StringReader(serverData))
                {
                    while ((line = reader.ReadLine()?.Trim()) != null)
                    {
                        ln++;

                        if (!string.IsNullOrWhiteSpace(line) || !line.StartsWith("#"))
                        {
                            if (line.Contains('%')) line = Environment.ExpandEnvironmentVariables(line);

                            string[] videoExtensions = { ".mp4", ".mp3", ".mov", ".webm", ".avi", ".wmv" };

                            if (Helper.IsYoutubeLink(line) || videoExtensions.Any(ext => line.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                if (!Program.ArgsCalled && !Helper.IsHttpsLink(line)) continue;

                                VideoList.Add(line, ln);
                            }

                            if (!ConectionToHerededServer && Helper.IsHttpsLink(line) && line.ToLower().EndsWith(".exe") && !Program.ArgsCalled && !serverData.Contains(Program.ProgramVersion))
                            {
                                await UpdateProgram(line, wc);
                                return;
                            }

                            string[] imageExtensions = [".ico", ".png", ".jpg", ".jpeg", ".gif"];

                            if (imageExtensions.Any(ext => line.ToLower().EndsWith(ext)))
                            {
                                if (!Program.ArgsCalled && !Helper.IsHttpsLink(line))
                                {
                                    continue;
                                }

                                IconList.Add(line, ln);
                            }

                            if (!ConectionToHerededServer && Helper.IsHttpsLink(line) && line.ToUpper().EndsWith(".OVP"))
                            {
                                HerededServer = true;
                                HerededServerUrl = line;
                            }

                            if (line.Contains('='))
                            {
                                if (line.StartsWith("Opacity="))
                                {
                                    try
                                    {
                                        this.Opacity = double.Parse(line.Trim().Split('=').Last(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

                                        this.TransparencyKey = Color.AliceBlue;
                                        this.BackColor = Color.AliceBlue;
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show(ex.ToString());
                                    }
                                }
                                else if (line.StartsWith("StartFullScreen="))
                                {
                                    if (bool.TryParse(line.Trim().Split('=').Last(), out bool startFullScreen) && startFullScreen)
                                    {
                                        this.WindowState = FormWindowState.Maximized;
                                    }
                                }
                                else if (line.StartsWith("AllowPause=")) ParseBoolElement(ref VideoAllowPause, line);
                                else if (line.StartsWith("FullScreen=")) ParseBoolElement(ref FullScreen, line);
                                else if (line.StartsWith("AllowFullScreen=")) ParseBoolElement(ref AllowFullScreen, line);
                                else if (line.StartsWith("VideoReload=")) ParseBoolElement(ref VideoReload, line);
                                else if (line.StartsWith("AllowClose=")) ParseBoolElement(ref AllowClose, line);
                                else if (line.StartsWith("VideoViews=")) ParseBoolElement(ref EnableVideoViews, line);
                                else if (line.StartsWith("AllowMinimize="))
                                {
                                    ParseBoolElement(ref AllowMinimize, line);
                                    this.MinimizeBox = AllowMinimize;
                                }
                                else if (line.StartsWith("AllowMaximize="))
                                {
                                    ParseBoolElement(ref AllowMaximize, line);
                                    this.MaximizeBox = AllowMaximize;
                                }
                                else if (line.StartsWith("HideMouse=")) ParseBoolElement(ref VideoHideMouse, line);
                                else if (line.StartsWith("NeedAudioDevice="))
                                {
                                    if (bool.TryParse(line.Trim().Split('=').Last(), out bool needAudioDevice) && needAudioDevice && waveOutGetNumDevs() <= 0)
                                    {
                                        this.Hide();

                                        MessageBox.Show("Para usar este programa, es necesario que tengas un dispositivo de audio conectado a tu computadora. El dispositivo de audio puede ser un altavoz, unos auriculares o un micrófono. El programa usará el dispositivo de audio para reproducir o grabar sonidos según lo que quieras hacer.\r\n\r\nSi no tienes un dispositivo de audio o no funciona correctamente, el programa no podrá funcionar. Por favor, asegúrate de que tu dispositivo de audio esté bien conectado y configurado antes de usar el programa.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                                        Environment.Exit(1);
                                    }
                                }
                                else if (line.StartsWith("VideoLoop="))
                                {
                                    if (bool.TryParse(line.Split('=').Last().Trim(), out bool videoLoop)) SetLoop(videoLoop);
                                }
                                else if (line.StartsWith("IgnoreInput=")) ParseBoolElement(ref VideoIgnoreInput, line);
                                else if (line.StartsWith("Volume="))
                                {
                                    if (int.TryParse(line.Split('=').Last().Trim(), out VideoVolume) && VideoVolume != PlayerVolume)
                                    {
                                        VideoVolumeChanged = true;
                                    }
                                }
                                else if (line.StartsWith("TopMost=")) ParseBoolElement(ref VideoTopMost, line);
                                else if (line.StartsWith("DebugOnly="))
                                {
                                    if (bool.TryParse(line.Trim().Split('=').Last(), out bool debugOnly) && debugOnly && !Debugger.IsAttached)
                                    {
                                        WaitForPlayTimer.Enabled = false;
                                        this.Hide();
                                        MessageBox.Show("El servidor esta en modo de pruebas y es posible que se esten probando funciones nuevas que no son compatibles con tu version", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        Environment.Exit(0);
                                    }
                                }
                                else if (line.StartsWith("WindowSize="))
                                {
                                    if (int.TryParse(line.Split('=').Last().TrimEnd('%'), out int parsedSize) && parsedSize > 0 && parsedSize < 101)
                                    {
                                        WindowSizePercentage = parsedSize;
                                    }
                                }
                                else if (line.StartsWith("PlayOn="))
                                {
                                    try
                                    {
                                        string time = line.Split('=').Last();

                                        string fristTime = time.Split(' ')[0];
                                        int day = int.Parse(fristTime.Split('/')[0]);
                                        int moth = int.Parse(fristTime.Split('/')[1]);
                                        int year = int.Parse(fristTime.Split('/')[2]);

                                        string secondTime = time.Split(' ').Last();
                                        int hour = int.Parse(secondTime.Split(':')[0]);
                                        int minute = int.Parse(secondTime.Split(':').Last().Split(',')[0]);
                                        int seconds = int.Parse(secondTime.Split(',').Last());

                                        PlayOn = new DateTime(year, moth, day, hour, minute, seconds, new CultureInfo("es-ES", false).Calendar);

                                        TimeSpan tiempoDeEspera = PlayOn - DateTime.Now;

                                        if (tiempoDeEspera.Ticks >= 0)
                                        {
                                            GifPictureBox.Image = Resources.SandClock;
                                            WaitForPlayTimer_Tick(new object(), new EventArgs());
                                            WaitForPlayTimer.Enabled = true;
                                            return;
                                        }
                                        else
                                        {
                                            WaitForPlayTimer.Enabled = false;
                                        }

                                        if (tiempoDeEspera.Minutes - 1 <= 1)
                                        {
                                            PlayOnEnabled = true;
                                        }
                                        else
                                        {
                                            PlayOnEnabled = false;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());

                                        WaitForPlayTimer.Enabled = false;
                                        PlayOn = DateTime.Now;
                                    }
                                }
                            }
                        }
                    }
                }

                //Se reconecta con el sub servidor
                if (!ConectionToHerededServer && HerededServer && !string.IsNullOrWhiteSpace(HerededServerUrl))
                {
                    ConectionToHerededServer = true;

                    VideoPlayer_Shown(new object(), new EventArgs());

                    return;
                }

                if (VideoList.Count <= 0)
                {
                    if (!Program.ArgsCalled)
                    {
                        this.Hide();

                        if (fristTime)
                        {
                            MessageBox.Show("Ahora mismo no hay ningun video para enseñar vuelve mas tarde :)", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Pues vaya se acabaron los video vuelve mas tarde a ver si hay uno :D", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }

                    Environment.Exit(0);
                }

                fristTime = false;

                this.Text = "Selecionando video";

                //Playlist resolver
                //Gracias a OneWholesomeDev#7465 y a Coca162#6765 por ayudarme con esto <3

                YoutubeClient videosProcesor = new YoutubeClient();

                Dictionary<string, int> playListsList = new Dictionary<string, int>();
                Dictionary<string, int> newPlaylistVideos = new Dictionary<string, int>();

                int playlistNum = 0;

                foreach (var vid in VideoList)
                {
                    if (Helper.IsYoutubeLink(vid.Key) && vid.Key.Contains("&list="))
                    {
                        try
                        {
                            playListsList.Add(vid.Key, vid.Value);
                        }
                        catch { }
                    }
                }

                if (playListsList.Count > 0)
                {
                    this.Text = "Procesando Playlists";

                    void UpdateProgress() => this.Text = "Procesando Playlists " + playlistNum + "/" + playListsList.Count + "  " + newPlaylistVideos.Count + " Videos";

                    foreach (var PlayList in playListsList)
                    {
                        playlistNum++;

                        UpdateProgress();

                        string listId = HttpUtility.ParseQueryString(new Uri(PlayList.Key).Query).Get("list");

                        await foreach (var PlaylistVideo in videosProcesor.Playlists.GetVideosAsync(listId))
                        {
                            try
                            {
                                newPlaylistVideos.Add(PlaylistVideo.Url, PlayList.Value);

                                UpdateProgress();
                            }
                            catch { }
                        }
                    }

                    if (newPlaylistVideos.Count >= 1)
                    {
                        foreach (var newVideo in newPlaylistVideos)
                        {
                            try
                            {
                                VideoList.Add(newVideo.Key, newVideo.Value);
                            }
                            catch { }
                        }
                    }
                }

                //Proximo processador de canales de youtube

                Dictionary<string, int> channelVideosList = new Dictionary<string, int>();
                Dictionary<string, int> newChannelVideos = new Dictionary<string, int>();

                int chanelNum = 0;

                foreach (var vid in VideoList)
                {
                    if (Helper.IsYoutubeLink(vid.Key) && vid.Key.Contains("/channel/"))
                    {
                        try
                        {
                            channelVideosList.Add(vid.Key.Split('/').Last(), vid.Value);
                        }
                        catch { }
                    }

                    if (Helper.IsYoutubeLink(vid.Key) && vid.Key.Contains("/c/"))
                    {
                        try
                        {
                            var channel = await videosProcesor.Channels.GetByUserAsync(vid.Key.Split('/').Last());

                            channelVideosList.Add(channel.Id, vid.Value);
                        }
                        catch { }
                    }
                }

                if (channelVideosList.Count > 0)
                {
                    this.Text = "Procesando Canales";

                    foreach (var chanel in channelVideosList)
                    {
                        chanelNum++;

                        this.Text = "Procesando Canales " + chanelNum + "/" + channelVideosList.Count + " " + newChannelVideos.Count + " Videos";

                        await foreach (var channelVideo in videosProcesor.Channels.GetUploadsAsync(chanel.Key))
                        {
                            try
                            {
                                newChannelVideos.Add(channelVideo.Url, chanel.Value);

                                this.Text = "Procesando Canales " + chanelNum + "/" + channelVideosList.Count + " " + newChannelVideos.Count + " Videos";
                            }
                            catch { }
                        }
                    }

                    if (newChannelVideos.Count >= 1)
                    {
                        foreach (var newVideo in newChannelVideos)
                        {
                            try
                            {
                                VideoList.Add(newVideo.Key, newVideo.Value);
                            }
                            catch { }
                        }
                    }
                }

                //Selecionador de video

                videoSelecionado = Rand.Next(VideoList.Count);

                try
                {
                    if (VideoList.Count >= 2)
                    {
                        if (File.Exists(Program.VideoPlayerConfigPath))
                        {
                            string SaveContent = videoSelecionado.ToString();
                            string SavedReproductedVideos = Config.GetConfig<string>("VideosPlayed");

                            Console.WriteLine("Checkeando videos ya reproducidos:" + SavedReproductedVideos);

                            if (!string.IsNullOrWhiteSpace(SavedReproductedVideos))
                            {
                                List<int> CheckerVideosPlayed = new List<int>();

                                using (StringReader reader = new StringReader(SavedReproductedVideos.Replace("%", "\n")))
                                {
                                    int checks = 1;

                                    string l;

                                    while ((l = reader.ReadLine()) != null && checks++ != VideoList.Count)
                                    {
                                        int videoNum = int.Parse(l);

                                        if (!CheckerVideosPlayed.Contains(videoNum)) CheckerVideosPlayed.Add(videoNum);
                                    }
                                }

                                List<int> listaDeVideos = new List<int>();

                                for (int i = 1; i <= VideoList.Count; i++)
                                {
                                    int videosExistentes = i - 1;

                                    if (videosExistentes <= -1) videosExistentes = 0;

                                    listaDeVideos.Add(videosExistentes);
                                }

                                bool videoYaSelecionado = true;

                                while (videoYaSelecionado)
                                {
                                    videoSelecionado = listaDeVideos[Rand.Next(listaDeVideos.Count)];

                                    if (CheckerVideosPlayed.Contains(videoSelecionado))
                                    {
                                        listaDeVideos.Remove(videoSelecionado);
                                    }
                                    else
                                    {
                                        videoYaSelecionado = false;
                                    }
                                }

                                Console.WriteLine("Guardando archivos de guardado");

                                using (StringReader reader = new StringReader(SavedReproductedVideos.Replace("%", "\n")))
                                {
                                    int checks = 1;

                                    SaveContent = "";

                                    string l;

                                    while ((l = reader.ReadLine()) != null && checks++ != VideoList.Count)
                                    {
                                        int videoNum = int.Parse(l);

                                        if (string.IsNullOrWhiteSpace(SaveContent))
                                        {
                                            SaveContent = videoNum.ToString();
                                        }
                                        else
                                        {
                                            SaveContent = SaveContent + "%" + videoNum;
                                        }
                                    }
                                }

                                SaveContent = videoSelecionado.ToString() + "%" + SaveContent;
                            }

                            Config.SaveConfig("VideosPlayed", SaveContent.ToString());
                        }
                        else
                        {
                            Config.SaveConfig("VideosPlayed", videoSelecionado.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    File.Delete(Program.VideoPlayerConfigPath);

                    Console.WriteLine("Eliminando Configuracion corrupta:\n\n" + ex.ToString());
                }

                VideoUrl = VideoList.ElementAt(videoSelecionado).Key;

                int videoUrlLine = VideoList.ElementAt(videoSelecionado).Value;

                int[] icons = [];

                foreach (var a in IconList) icons = icons.Concat([a.Value]).ToArray();

                if (icons.Length > 0)
                {
                    string videoIconUrl = IconList.FirstOrDefault(x => x.Value == (from num in icons let diff = Math.Abs(videoUrlLine - num) orderby diff select num).First()).Key;

                    DownloadName = "Descargando Icono {0}";
                    ChangeFormIconFromUrl(videoIconUrl);
                }

                string videoHash = Program.GetHash(Encoding.UTF8.GetBytes(VideoUrl));

                string videoName = HttpUtility.UrlDecode(Program.ArgsCalled ? new FileInfo(VideoUrl).Name : VideoUrl.Split('/').Last().Split('?')[0].Replace("_", " ")).Trim();

                IsRadio = videoName.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase);

                if (videoName.ToLower().Contains(".")) videoName = videoName.Split('.')[0];

                if (!Program.ArgsCalled)
                {
                    string oldTitle = this.Text;

                    Task.Factory.StartNew(async () =>
                    {
                        WebClient visitsWebClient = new WebClient();

                        string result = await visitsWebClient.DownloadStringTaskAsync("https://visitor-badge.laobi.icu/badge?page_id=" + videoHash);

                        long.TryParse(result.Split(new string[] { "</text><text" }, StringSplitOptions.None)[2].Split('>').Last(), out VideoVisits);
                    });

                    this.Text = oldTitle;
                }

                string videoDefaultPath = Path.Combine(Program.tempPath, videoHash + Program.TemporaryFilesExtension);

                string tempSignalFile = videoDefaultPath + ".tmp";

                if (!IsRadio)
                {
                    if (File.Exists(tempSignalFile))
                    {
                        File.Delete(videoDefaultPath);
                        File.Delete(tempSignalFile);
                    }

                    if (Helper.IsYoutubeLink(VideoUrl))
                    {
                        this.Text = "Obteniendo informacion del video";

                        Video video = await videosProcesor.Videos.GetAsync(VideoUrl);

                        videoName = video.Title;

                        if (!File.Exists(videoDefaultPath))
                        {
                            File.Create(tempSignalFile).Close();

                            this.Text = "Preparando para descargar";

                            var streamManifest = await videosProcesor.Videos.Streams.GetManifestAsync(video.Id);
                            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                            this.Text = "Descargando video";
                            DownloadName = this.Text + " {0}";

                            await videosProcesor.Videos.Streams.DownloadAsync(streamInfo, videoDefaultPath, new Progress<double>(client_DownloadProgressChanged));

                            File.Delete(tempSignalFile);
                        }
                    }
                    else
                    {
                        if (Helper.IsHttpsLink(VideoUrl))
                        {
                            if (!File.Exists(videoDefaultPath))
                            {
                                File.Create(tempSignalFile).Close();

                                this.Text = "Descargando video";
                                DownloadName = this.Text + " {0}";

                                wc.Headers.Add("referer", this.Name);
                                wc.Headers.Add("user-agent", Application.ProductName);

                                byte[] videoData = await wc.CustomDataDownloadAsync(VideoUrl);

                                File.WriteAllBytes(videoDefaultPath, videoData);
                                File.Delete(tempSignalFile);
                            }
                        }
                        else
                        {
                            if (!File.Exists(VideoUrl))
                            {
                                MessageBox.Show("Error el archivo (" + VideoUrl + ") no existe", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Environment.Exit(1);
                            }
                        }
                    }
                }

                if (IsRadio) videoDefaultPath = VideoUrl;

                this.Text = "Procesando " + (IsRadio ? "audio" : "video");

                Repeating = false;

                if (PlayOnEnabled)
                {
                    if ((PlayOn - DateTime.Now).TotalSeconds + 10 > 0) this.Text = "Esperando para sincronizarse";

                    while ((PlayOn - DateTime.Now).TotalSeconds + 10 > 0)
                    {
                        await Task.Delay(100);
                    }
                }

                PlayVideoFromResouscres(!Program.ArgsCalled ? videoDefaultPath : VideoUrl, videoName);
            }
            catch (WebException ex)
            {
                this.Hide();

                if (CheckForInternetConnection())
                {
                    string errorResponse = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();

                    if (errorResponse.ToLower().Contains("run this repl to see the results here"))
                    {
                        MessageBox.Show("El servidor se esta iniciando por favor espere unos segundos", "Error de servidor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error el servidor no funciona correctamente\n\n" + ex.ToString() + "\n\nVideo:" + VideoUrl + "(" + ex.Status + ")", "Error de servidor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Error no se pudo conectar al servidor\n\n" + ex.ToString() + "\n\nVideo:" + VideoUrl + "(" + ex.Status + ")", "Error de conexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                this.Hide();
                MessageBox.Show("Ocurio un error desconocido\n\n" + ex.ToString() + "\n\nLink: " + VideoUrl, "Error desconocido", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        public static bool CheckForInternetConnection()
        { try { return new WebClient().OpenRead("http://google.com/generate_204").CanRead; } catch { return false; } }

        private async Task PlayVideoFromResouscres(string VideoPath, string videoName)
        {
            try
            {
                string registryPath = @"Software\Microsoft\MediaPlayer\Player\Extensions\." + VideoPath.Split('.').Last();

                if (Registry.CurrentUser.OpenSubKey(registryPath, false) == null)
                {
                    using (RegistryKey playerOptionsReg = Registry.CurrentUser.CreateSubKey(registryPath))
                    {
                        playerOptionsReg.SetValue("Permissions", "15", RegistryValueKind.DWord);
                        playerOptionsReg.SetValue("Runtime", "6", RegistryValueKind.DWord);
                    }
                }

                VideoPanel.Dock = axMediaPlayer.Dock = DockStyle.Fill;

                axMediaPlayer.settings.autoStart = true;
                axMediaPlayer.settings.enableErrorDialogs = true;
                axMediaPlayer.URL = VideoPath;
                axMediaPlayer.stretchToFit = true;
                axMediaPlayer.uiMode = "none";
                axMediaPlayer.Visible = true;

                axMediaPlayer.settings.rate = 1;

                MetaDataTimer.Enabled = true;
                GifPictureBox.Visible = false;
                GifPictureBox.Image = null;

                Task.Factory.StartNew(() => Invoke(new MethodInvoker(() => SetWindowSize())));

                this.Text = "Reproduciendo video";

                MetaDataTimer.Enabled = true;

                try
                {
                    axMediaPlayer.Ctlcontrols.play();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());

                    MessageBox.Show(ex.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                try
                {
                    axMediaPlayer.currentMedia.name = videoName;
                }
                catch { }

                if (VideoFullScreen)
                {
                    Console.WriteLine("Cambiando a fullscreen");
                    FullScreen = false;
                    AlternFullScreen();
                }

                while (!Playing) await Task.Delay(30);

                MetaDataTimer_Tick(new object(), new EventArgs());

                if (VideoTopMost)
                {
                    Console.WriteLine("Activando top most");

                    this.TopMost = true;
                }

                if (VideoHideMouse)
                {
                    System.Windows.Forms.Cursor.Hide();
                }
                else
                {
                    System.Windows.Forms.Cursor.Show();
                }

                if (VideoVolumeChanged)
                {
                    Console.WriteLine("Cambiando volumen al predeterminado");

                    SetPlayerVolume(VideoVolume, Muted, false);
                }
                else
                {
                    bool volumeChanged = false;

                    if (File.Exists(Program.VideoPlayerConfigPath))
                    {
                        string SavedVolume = Config.GetConfig<string>("PlayerVolume", PlayerVolume.ToString());

                        Muted = Config.GetConfig<bool>("Muted", Muted);

                        if (!string.IsNullOrWhiteSpace(SavedVolume))
                        {
                            int newVolume = int.Parse(SavedVolume);

                            PlayerVolume = newVolume;

                            SetPlayerVolume(newVolume, Muted, false);

                            volumeChanged = true;
                        }
                    }

                    if (!volumeChanged) SetPlayerVolume(PlayerVolume);
                }

                GC.Collect();
            }
            catch (Exception ex)
            {
                if (File.Exists(VideoPath)) File.Delete(VideoPath);

                MessageBox.Show(ex.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateProgram(string UpdateUrl, WebClient wc)
        {
            string arguments = string.Join(", ", Environment.GetCommandLineArgs());

            if (arguments.Contains("/AutoSelfUpdate"))
            {
                this.Focus();
                this.Hide();

                DialogResult Respusta = MessageBox.Show("Ups el programa se actualizo pero encontro otra actualizacion aparte asi que supongo que es un error\n\nEsto puede suceder si la version del link de actualizacion no es la misma a la que indica el servidor", Application.ProductName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);

                if (Respusta != DialogResult.Retry) Environment.Exit(1);
            }

            string tempPath = Path.GetTempFileName();

            this.Text = "Actualizando programa";

            DownloadName = "Actualizando programa {0}";

            await wc.DownloadFileTaskAsync(new Uri(UpdateUrl, UriKind.Absolute), tempPath);

            string ExtraCmdArgs = "";

            if (UpdateUrl.ToLower().EndsWith(".zip"))
            {
                this.Text = "Descomprimiendo archivo";

                string freePathDir = Path.GetTempFileName().Split('.')[0];

                ZipFile.ExtractToDirectory(tempPath, freePathDir);

                foreach (var file in new DirectoryInfo(freePathDir).GetFiles("*"))
                {
                    tempPath = file.FullName;
                }

                ExtraCmdArgs = "rd /s /q \"" + freePathDir + "\" & ";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Environment.SystemDirectory, Encoding.UTF8.GetString("CmD.eXE"u8.ToArray()));
            startInfo.Arguments = "/c taskkill /im \"" + AppDomain.CurrentDomain.FriendlyName + "\" /f & copy \"" + Assembly.GetExecutingAssembly().Location + "\" \"" + Assembly.GetExecutingAssembly().Location.Split('.')[0] + " (Viejo " + Program.ProgramVersion + ").exe\" /b /v /y & attrib -s -r -h \"" + Assembly.GetExecutingAssembly().Location + "\" & copy \"" + tempPath + "\" \"" + Assembly.GetExecutingAssembly().Location + "\" /b /v /y & del \"" + tempPath + "\" /f & " + ExtraCmdArgs + "\"" + Assembly.GetExecutingAssembly().Location + "\" /AutoSelfUpdate".Replace(" &", "").Replace("& ", "");
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
            Environment.Exit(0);
        }

        private void VideoPlayer_FormClosing(object sender, FormClosingEventArgs e) => e.Cancel = !AllowClose;

        public Key KeyFromVirtualKey(int virtualKey) => KeyInterop.KeyFromVirtualKey(virtualKey);

        private async void wmp_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == 8)
            {
                await Task.Delay(300);

                if (!LoopPlaying && !VideoReload)
                    Environment.Exit(0);

                if (VideoReload)
                {
                    this.Size = originalSize;
                    ReloadVideo();
                }
            }
            else if (e.newState == 2)
            {
                Playing = false;
                paused = true;
            }
            else if (e.newState == 3)
            {
                paused = false;
                Playing = true;
            }
            /*
            else if (e.newState == 10)
            {
                if (IsLeftClickPressed())
                {
                    while (IsLeftClickPressed())
                    {
                        await Task.Delay(60);
                    }
                }

                Task.Factory.StartNew(() => Invoke(new MethodInvoker(() => SetWindowSize())));
            }*/

            Console.WriteLine("Nuevo estado de reproducion:" + e.newState);
        }

        public static bool IsLeftClickPressed() => Control.MouseButtons == MouseButtons.Left;

        private bool Repeating = false;

        public void ReloadVideo()
        {
            if (!Repeating)
            {
                axMediaPlayer.Ctlcontrols.pause();

                Repeating = true;

                FristTimeCheck = true;

                MetaDataTimer.Enabled = false;

                HerededServer = false;

                ConectionToHerededServer = false;

                Task.Factory.StartNew(() => Invoke(new MethodInvoker(() => { VideoPlayer_Shown(new object(), new EventArgs()); })));
            }
        }

        private async void axWindowsMediaPlayer1_KeyDownEvent(object sender, AxWMPLib._WMPOCXEvents_KeyDownEvent e)
        {
            Key teclaPresionada = KeyFromVirtualKey(e.nKeyCode);

            if (VideoAllowPause && teclaPresionada == Key.Space || teclaPresionada == Key.K) AlternPause();

            if (VideoIgnoreInput) return;

            if (teclaPresionada == Key.Left && !IsRadio)
            {
                axMediaPlayer.Ctlcontrols.currentPosition -= IsKeyDown(Keys.Control) ? 10 : 5;

                if (paused) ((WMPLib.IWMPControls2)axMediaPlayer.Ctlcontrols).step(1);
            }
            else if (teclaPresionada == Key.Right && !IsRadio)
            {
                axMediaPlayer.Ctlcontrols.currentPosition += IsKeyDown(Keys.Control) ? 10 : 5;

                if (paused) ((WMPLib.IWMPControls2)axMediaPlayer.Ctlcontrols).step(1);
            }
            else if (teclaPresionada == Key.Down || teclaPresionada == Key.Up)
            {
                bool volumenUp = teclaPresionada == Key.Up;

                int currentPlayerVolume = axMediaPlayer.settings.volume;

                int incrementator;

                if (currentPlayerVolume <= 10)
                {
                    incrementator = 1;
                }
                else if (currentPlayerVolume <= 20)
                {
                    incrementator = 2;
                }
                else
                {
                    incrementator = 5;
                }

                int newVolume = volumenUp ? currentPlayerVolume + incrementator : currentPlayerVolume - incrementator;

                SetPlayerVolume(newVolume, newVolume <= 0, true);

                if (newVolume >= 100) PlayBeep();
            }
            else if ((teclaPresionada == Key.F && Keyboard.IsKeyDown(Key.F)) || (teclaPresionada == Key.F11 && Keyboard.IsKeyDown(Key.F11)))
            {
                if (AlterningFullScreen || !AllowFullScreen) return;

                AlternFullScreen();
            }
            else if (teclaPresionada == Key.M)
            {
                SetPlayerVolume(PlayerVolume, !Muted, true);
            }
            else if (teclaPresionada == Key.T)
            {
                VideoReload = !VideoReload;

                LoopPlaying = false;

                SetLoop(false);
            }
            else if (teclaPresionada == Key.R)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    Application.Restart();
                }
                else
                {
                    ReloadVideo();
                }
            }
            else if (teclaPresionada == Key.L)
            {
                AlternLoop();

                VideoReload = false;
            }

            MetaDataTimer_Tick(new object(), new EventArgs());
        }

        private void axWindowsMediaPlayer1_DoubleClickEvent(object sender, AxWMPLib._WMPOCXEvents_DoubleClickEvent e)
        {
            if (VideoIgnoreInput) return;

            AlternFullScreen();
        }

        private void axWindowsMediaPlayer1_ClickEvent(object sender, AxWMPLib._WMPOCXEvents_ClickEvent e)
        {
            if (!VideoAllowPause) return;

            if (e.nButton == 1) AlternPause();

            MetaDataTimer_Tick(new object(), new EventArgs());
        }

        private bool FullScreen = false;
        private bool WasMaximized = false;
        private bool AlterningFullScreen = false;

        private void AlternFullScreen()
        {
            AlterningFullScreen = true;

            if (!FullScreen)
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    WasMaximized = true;

                    this.WindowState = FormWindowState.Normal;
                }
                else
                {
                    WasMaximized = false;
                }

                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.Focus();
            }
            else
            {
                this.WindowState = WasMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                Task.Factory.StartNew(() => Invoke(new MethodInvoker(() => SetWindowSize())));
            }

            FullScreen = !FullScreen;

            AlterningFullScreen = false;
        }

        private static bool Muted = false;

        private void SetPlayerVolume(int newVolume, bool Mute, bool Save) => SetPlayerVolumeCore(newVolume, Mute, Save);

        private void SetPlayerVolume(int newVolume) => SetPlayerVolumeCore(newVolume, false, true);

        private void SetPlayerVolumeCore(int newVolume, bool Mute, bool Save)
        {
            if (newVolume > 100)
            {
                newVolume = 100;
            }
            else if (newVolume < 0)
            {
                newVolume = 0;
            }

            if (Mute)
            {
                newVolume = 0;
            }
            else
            {
                PlayerVolume = newVolume;
            }

            Console.WriteLine("Cambiando Volumen:" + newVolume.ToString() + " Mute:" + Mute + " Save:" + Save.ToString());

            axMediaPlayer.settings.volume = newVolume;

            if (Muted && newVolume != 0)
            {
                Muted = false;
            }
            else if (!Muted && (axMediaPlayer.settings.volume == 0 && newVolume == 0))
            {
                Muted = true;
            }

            if (Save)
            {
                Config.SaveConfig("Muted", Mute);

                if (!Muted) Config.SaveConfig("PlayerVolume", newVolume.ToString());
            }
        }

        private void AlternLoop() => SetLoop(!LoopPlaying);

        private void SetLoop(bool loop) => axMediaPlayer.settings.setMode("loop", LoopPlaying = loop);

        private void AlternPause() => SetPause(!paused);

        private void SetPause(bool pause)
        {
            if (paused = pause)
            {
                axMediaPlayer.Ctlcontrols.pause();
            }
            else
            {
                axMediaPlayer.Ctlcontrols.play();
            }
        }

        public bool VerticalVideo = false;

        private async Task SetWindowSize()
        {
            Console.WriteLine("Cambiando de tamaño la ventana");

            int width = 0;
            int height = 0;

            while (width == 0 || height == 0)
            {
                await Task.Delay(50);

                try
                {
                    width = axMediaPlayer.currentMedia.imageSourceWidth;
                    height = axMediaPlayer.currentMedia.imageSourceHeight;
                }
                catch { }
            }

            double widthPercentage, heightPercentage;

            if (width > height)
            {
                widthPercentage = 100;
                heightPercentage = ((double)height * 100) / width;
            }
            else if (height > width)
            {
                heightPercentage = 100;
                widthPercentage = ((double)width * 100) / height;
            }
            else
            {
                heightPercentage = widthPercentage = 100;
            }

            widthPercentage = (widthPercentage * WindowSizePercentage) / 100;
            heightPercentage = (heightPercentage * WindowSizePercentage) / 100;

            width = (int)((currentScreen.Bounds.Width / 100) * widthPercentage);
            height = (int)((currentScreen.Bounds.Width / 100) * heightPercentage);

            Console.WriteLine("Cambiando tamaño de la ventama With " + width + " height " + height);

            /*if (width > 1920 || height > 1920)
            {
                width = width / 2;
                height = height / 2;
            }
            else if (width > 1200 || height > 1200)
            {
                width = (int)(width / 1.1);
                height = (int)(height / 1.1);
            }
            else if (width > 800 || height > 800)
            {
                width = (int)(width * 1.5);
                height = (int)(height * 1.5);
            }
            else if (width > 500 || height > 500)
            {
                width = (int)(width * 1.8);
                height = (int)(height * 1.8);
            }
            else if (width > 300 || height > 300)
            {
                width *= 2;
                height *= 2;
            }
            else
            {
                width = (int)(width * 2.4);
                height = (int)(height * 2.4);
            }*/

            /*double WidthCalcPerc = (double)(this.Size.width / (double)(this.Size.width + (this.Size.width - axMediaPlayer.Size.width))) / 1000;
            int PreCalculatedWidth = (int)Math.Round(((this.Size.width - ((double)this.Size.width * WidthCalcPerc) * 1000)) + width);

            double HeightCalcPerc = (double)(this.Size.height / (double)(this.Size.height + (this.Size.height - axMediaPlayer.Size.height))) / 1000;
            int PreCalculatedHeight = (int)Math.Round(((this.Size.height - ((double)this.Size.height * HeightCalcPerc) * 1000)) + height);*/

            this.Size = new Size(width, height);
            this.Size = new Size(this.Width + (this.Size.Width - axMediaPlayer.Width), this.Height + (this.Size.Height - axMediaPlayer.Height));
            this.Location = new Point((Screen.FromControl(this).WorkingArea.Width - this.Width) / 2, (Screen.FromControl(this).WorkingArea.Height - this.Height) / 2);
        }

        private async void MetaDataTimer_Tick(object sender, EventArgs e)
        {
            MetaDataTimer.Enabled = false;

            try
            {
                string titleTextCreator = "";

                if (axMediaPlayer.currentMedia != null)
                {
                    titleTextCreator = titleTextCreator + axMediaPlayer.currentMedia.name.Trim('\"').Trim(':').Trim();

                    if (axMediaPlayer.currentMedia.imageSourceWidth != 0 && !VerticalVideo && !IsRadio)
                    {
                        titleTextCreator = titleTextCreator + " (" + axMediaPlayer.currentMedia.imageSourceWidth + "X" + axMediaPlayer.currentMedia.imageSourceHeight + ")";
                    }

                    if (!IsRadio && !string.IsNullOrWhiteSpace(axMediaPlayer.Ctlcontrols.currentPositionString))
                    {
                        titleTextCreator = titleTextCreator + " " + axMediaPlayer.Ctlcontrols.currentPositionString + "/" + axMediaPlayer.currentMedia.durationString;
                    }

                    await Task.Delay(40);

                    if (VideoList.Count >= 2 && !IsRadio) titleTextCreator = titleTextCreator + "  Video:" + (videoSelecionado + 1) + "/" + VideoList.Count + "";

                    titleTextCreator += " ";

                    if (IsRadio) titleTextCreator += "📻";

                    if (Muted) titleTextCreator += "🔇";

                    if (paused) titleTextCreator += "⏸️ ";

                    if (!IsRadio)
                    {
                        if (LoopPlaying) titleTextCreator += "♾️";

                        if (VideoReload) titleTextCreator += "🔃";
                    }

                    titleTextCreator = Program.ArgsCalled ? titleTextCreator : titleTextCreator + (VideoVisits != 0 ? " 👀" + VideoVisits : null);

                    this.Text = titleTextCreator;
                }
            }
            catch { }

            MetaDataTimer.Enabled = true;
        }

        //Que pereza pensar y programar
        private static readonly double[] AverageDownloadSpeed = { 0 };

        private static Stopwatch Sw = new Stopwatch();

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            /*if ((totalBytes - bytesIn) > 0)
            {
                double bytesReaded = (((totalBytes - bytesIn) * 1000) / Sw.ElapsedMilliseconds) / 1024 / 1024 / 1024 * 8;
                double Averrage = (long)Math.Round(Queryable.Average(AverageDownloadSpeed.AsQueryable()));

                Console.WriteLine(Averrage);

                Sw.Restart();

                AverageDownloadSpeed = AverageDownloadSpeed.Concat(new double[] { (double)bytesReaded }).ToArray();
                if (AverageDownloadSpeed.Length >= 10)
                {
                    AverageDownloadSpeed = AverageDownloadSpeed.Skip(1).ToArray();
                }
            }*/

            UpdateProgress((double)e.BytesReceived / e.TotalBytesToReceive * 10000);
        }

        public void client_DownloadProgressChanged(double newProgress) => UpdateProgress(newProgress * 10000);

        public void UpdateProgress(double porcentaje)
        {
            string percentaje = ((int)porcentaje).ToString();

            if (int.Parse(percentaje) <= 9) percentaje = "0" + percentaje;

            if (int.Parse(percentaje) <= 99) percentaje = "0" + percentaje;

            if (percentaje != "0") percentaje = percentaje.Insert(percentaje.Length - 2, ",");

            this.Text = string.Format(DownloadName, percentaje + "%");
        }

        public static string CompressString(string text) => CompressString(text, Encoding.UTF8);

        public static string DecompressString(string text) => DecompressString(text, Encoding.UTF8);

        public static string CompressString(string text, Encoding Encoder) => Convert.ToBase64String(Compress(Encoder.GetBytes(text)));

        public static string DecompressString(string text, Encoding Encoder) => Encoder.GetString(Decompress(Convert.FromBase64String(text)));

        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                {
                    dstream.Write(data, 0, data.Length);
                }

                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream input = new MemoryStream(data))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
                    {
                        dstream.CopyTo(output);
                    }

                    return output.ToArray();
                }
            }
        }

        private static Color DefaultColor = Color.FromArgb(0x16ffff);

        public static Color GetAccentColor()
        {
            using (RegistryKey dwmKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\DWM", RegistryKeyPermissionCheck.ReadSubTree))
            {
                if (dwmKey is null) return DefaultColor;

                object accentColorObj = dwmKey.GetValue("AccentColor");

                if (accentColorObj is int accentColorDword)
                {
                    var acentColor = ParseDWordColor(accentColorDword);

                    return Color.FromArgb(acentColor.a, acentColor.r, acentColor.g, acentColor.b);
                }
                else
                {
                    return DefaultColor;
                }
            }
        }

        private static (byte r, byte g, byte b, byte a) ParseDWordColor(int color)
        {
            byte a = (byte)((color >> 24) & 0xFF), b = (byte)((color >> 16) & 0xFF), g = (byte)((color >> 8) & 0xFF), r = (byte)((color >> 0) & 0xFF);

            return (r, g, b, a);
        }

        private static bool RandomBool(int percentage) => new Random().Next(100) <= percentage;

        private static bool CompresedBool(string text) => text == "1";

        private static string CompresedBool(bool boolean) => boolean ? "1" : "0";

        private static bool PlayingBeep = false;

        private static async void PlayBeep()
        {
            if (PlayingBeep) return;

            PlayingBeep = true;

            SystemSounds.Beep.Play();

            await Task.Delay(100);

            PlayingBeep = false;
        }

        public static bool IsKeyDown(Keys k) => (Control.ModifierKeys & k) == k;
    }
}