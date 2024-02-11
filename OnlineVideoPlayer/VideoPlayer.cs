﻿using Microsoft.Win32;
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
using System.Text.Json.Nodes;
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
        public static WebClient Web = new WebClient();

        private static string DownloadName = "Pensando {0}";

        private static Size originalSize;

        private static string ServerUrl = "https://FristServerOVP.onlinevideopyr.repl.co/OKIPULLUP/MainData.OVP";

        private static bool ConectionToHerededServer = false;
        private static bool HerededServer = false;
        private static string HerededServerUrl = "";

        private static Random Rand = new Random();

        private static int PlayerVolume = 60;

        [DllImport("winmm.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)] private static extern uint waveOutGetNumDevs();

        public VideoPlayer()
        {
            InitializeComponent();

            Screen CurrentScreen = Screen.FromControl(this);

            this.Size = new Size(CurrentScreen.Bounds.Width / 2, CurrentScreen.Bounds.Height / 2);

            originalSize = this.Size;

            axWindowsMediaPlayer1.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(wmp_PlayStateChange);
        }

        private void VideoPlayer_Load(object sender, EventArgs e)
        {
            VideoPanel.Dock = DockStyle.Fill;
            GifPictureBox.Dock = DockStyle.Fill;

            this.Icon = Program.ProgramIco;

            GifPictureBox.Image = Resources.Loading_Big;

            GifPictureBox.BackColor = GetAccentColor();
        }

        private void ChangeFormIconFromUrl(string url)
        {
            Invoke(new MethodInvoker(() =>
            {
                try
                {
                    bool internetUri = Helper.IsHttpsLink(url);

                    byte[] iconData = null;

                    string linkHash = internetUri ? Program.GetSHA256Hash(Encoding.Unicode.GetBytes(url)) : null;
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
                            iconData = new WebClient().DownloadData(url);

                            File.WriteAllBytes(tempIconPath, Compress(iconData));
                        }
                        else
                        {
                            if (File.Exists(url))
                            {
                                iconData = File.ReadAllBytes(url);
                            }
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

        private bool fristTime { get; set;  }= true;
        private bool FristTimeCheck { get; set; } = true;
        private bool IsRadio { get; set; } = false;

        private bool paused { get; set; } = false;
        private bool Playing { get; set; } = false;

        private bool VideoReload { get; set; } = false;
        private bool LoopPlaying { get; set; } = false;

        private bool VideoIgnoreInput { get; set; } = false;
        private bool VideoAllowPause { get; set; } = true;
        private bool VideoHideMouse { get; set; } = false;

        private bool VideoFullScreen { get; set; } = false;
        private bool VideoTopMost { get; set; } = false;

        private bool AllowClose { get; set; } = false;
        private bool AllowMinimize { get; set; } = false;
        private bool AllowMaximize { get; set; } = false;

        private bool VideoVolumeChanged { get; set; } = false;
        private int VideoVolume { get; set; } = 50;

        private bool PlayOnEnabled { get; set; } = false;
        private DateTime PlayOn { get; set; } = new DateTime();

        private int videoSelecionado;
        private string VideoUrl;

        private CancellationTokenSource realTimeVisitsTaskCancelToken = new CancellationTokenSource();

        private Dictionary<string, int> VideoList = new Dictionary<string, int>();
        private Dictionary<string, int> IconList = new Dictionary<string, int>();

        private long VideoVisits = 0;

        private DownloadProgressChangedEventHandler DownloadProgressHandler = null;

        private async void VideoPlayer_Shown(object sender, EventArgs e)
        {
            realTimeVisitsTaskCancelToken.Cancel();

            realTimeVisitsTaskCancelToken = new CancellationTokenSource();

            GifPictureBox.Visible = true;

            if (FristTimeCheck)
            {
                GifPictureBox.Image = Resources.Loading_Big;

                DownloadProgressHandler = new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);

                Web.DownloadProgressChanged += DownloadProgressHandler;
            }
            else
            {
                Web.DownloadProgressChanged -= DownloadProgressHandler;
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

                        if (FristTimeCheck)
                        {
                            this.Text = "Conectandose al servidor ";
                        }

                        serverData = await Web.DownloadStringTaskAsync(ServerUrl);
                    }
                    else
                    {
                        Console.WriteLine("Conectandose con el servidor heredado");
                        DownloadName = "Conectandose al servidor heredado {0}";

                        if (FristTimeCheck)
                        {
                            this.Text = "Conectandose al servidor heredado ";
                        }

                        serverData = await Web.DownloadStringTaskAsync(HerededServerUrl);
                    }

                    Console.WriteLine("Servidor conectado procesando informacion");
                    Console.WriteLine("");
                }
                else
                {
                    serverData = File.ReadAllText(Program.ConfigPath);
                }

                if (FristTimeCheck)
                {
                    this.Text = "Procesando informacion";
                }

                string line;

                int LineNum = 0;

                FristTimeCheck = false;

                using (StringReader reader = new StringReader(serverData))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        LineNum++;

                        line = line.Trim();

                        if (!string.IsNullOrWhiteSpace(line) || !line.StartsWith("#"))
                        {
                            string[] videoExtensions = { ".mp4", ".mp3", ".mov", ".webm", ".avi", ".wmv" };

                            if (videoExtensions.Any(Ext => line.ToLower().EndsWith(Ext)) || Helper.IsYoutubeLink(line))
                            {
                                if (!Program.ArgsCalled && !Helper.IsHttpsLink(line))
                                {
                                    continue;
                                }

                                VideoList.Add(line, LineNum);
                            }

                            if (!ConectionToHerededServer && Helper.IsHttpsLink(line) && line.ToLower().EndsWith(".exe") && !Program.ArgsCalled)
                            {
                                if (!serverData.Contains(Program.ProgramVersion))
                                {
                                    UpdateProgram(line, Web);

                                    return;
                                }
                            }

                            string[] imageExtensions = { ".ico", ".png", ".jpg", ".jpeg", ".gif" };

                            if (imageExtensions.Any(Ext => line.ToLower().EndsWith(Ext)))
                            {
                                if (!Program.ArgsCalled && !Helper.IsHttpsLink(line))
                                {
                                    continue;
                                }

                                IconList.Add(line, LineNum);
                            }

                            if (!ConectionToHerededServer && Helper.IsHttpsLink(line) && line.ToUpper().EndsWith(".OVP"))
                            {
                                HerededServer = true;
                                HerededServerUrl = line;
                            }

                            if (line.Contains("="))
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
                                    try
                                    {
                                        if (bool.Parse(line.Trim().Split('=').Last()))
                                        {
                                            this.WindowState = FormWindowState.Maximized;
                                        }
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("AllowPause="))
                                {
                                    try
                                    {
                                        VideoAllowPause = bool.Parse(line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("FullScreen="))
                                {
                                    try
                                    {
                                        VideoFullScreen = bool.Parse(line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("VideoReload="))
                                {
                                    try
                                    {
                                        VideoReload = bool.Parse(line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("AllowClose="))
                                {
                                    try
                                    {
                                        AllowClose = bool.Parse(line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("AllowMinimize="))
                                {
                                    try
                                    {
                                        AllowMinimize = bool.Parse(line.Trim().Split('=').Last());

                                        this.MinimizeBox = AllowMinimize;
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("AllowMaximize="))
                                {
                                    try
                                    {
                                        AllowMaximize = bool.Parse(line.Trim().Split('=').Last());

                                        this.MaximizeBox = AllowMaximize;
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("HideMouse="))
                                {
                                    try
                                    {
                                        VideoHideMouse = bool.Parse(line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("NeedAudioDevice="))
                                {
                                    try
                                    {
                                        if (bool.Parse(line.Trim().Split('=').Last()) && waveOutGetNumDevs() <= 0)
                                        {
                                                this.Hide();

                                                MessageBox.Show("Para usar este programa, es necesario que tengas un dispositivo de audio conectado a tu computadora. El dispositivo de audio puede ser un altavoz, unos auriculares o un micrófono. El programa usará el dispositivo de audio para reproducir o grabar sonidos según lo que quieras hacer.\r\n\r\nSi no tienes un dispositivo de audio o no funciona correctamente, el programa no podrá funcionar. Por favor, asegúrate de que tu dispositivo de audio esté bien conectado y configurado antes de usar el programa.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                                                Environment.Exit(1);
                                            
                                        }
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("VideoLoop="))
                                {
                                    try
                                    {
                                        AlternLoop(bool.Parse(line.Trim().Split('=').Last()));
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("IgnoreInput="))
                                {
                                    try
                                    {
                                        VideoIgnoreInput = bool.Parse(line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("Volume="))
                                {
                                    try
                                    {
                                        VideoVolume = int.Parse(line.Trim().Split('=').Last());

                                        if (VideoVolume != PlayerVolume)
                                        {
                                            VideoVolumeChanged = true;
                                        }
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("TopMost="))
                                {
                                    try
                                    {
                                        VideoTopMost = bool.Parse(line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }
                                else if (line.StartsWith("DebugOnly="))
                                {
                                    try
                                    {
                                        if (bool.Parse(line.Trim().Split('=').Last()) == true && !Debugger.IsAttached)
                                        {
                                            WaitForPlayTimer.Enabled = false;

                                            this.Hide();
                                            MessageBox.Show("El servidor esta en modo de pruebas y es posible que se esten probando funciones nuevas que no son compatibles con tu version", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Environment.Exit(0);
                                        }
                                    }
                                    catch { }
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

                                        if (!(tiempoDeEspera.Minutes - 1 > 1))
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

                    foreach (var PlayList in playListsList)
                    {
                        playlistNum++;

                        this.Text = "Procesando Playlists " + playlistNum + "/" + playListsList.Count + "  " + newPlaylistVideos.Count + " Videos";

                        string listId = HttpUtility.ParseQueryString(new Uri(PlayList.Key).Query).Get("list");

                        await foreach (var PlaylistVideo in videosProcesor.Playlists.GetVideosAsync(listId))
                        {
                            try
                            {
                                newPlaylistVideos.Add(PlaylistVideo.Url, PlayList.Value);

                                this.Text = "Procesando Playlists " + playlistNum + "/" + playListsList.Count + "  " + newPlaylistVideos.Count + " Videos";
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

                        this.Text = "Procesando Canales " + chanelNum + "/" + channelVideosList.Count + "  " + newChannelVideos.Count + " Videos";

                        await foreach (var channelVideo in videosProcesor.Channels.GetUploadsAsync(chanel.Key))
                        {
                            try
                            {
                                newChannelVideos.Add(channelVideo.Url, chanel.Value);

                                this.Text = "Procesando Canales " + chanelNum + "/" + channelVideosList.Count + "  " + newChannelVideos.Count + " Videos";
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

                foreach (var a in IconList) icons = icons.Concat(new int[] { a.Value }).ToArray();

                if (icons.Length > 0)
                {
                    string videoIconUrl = IconList.FirstOrDefault(x => x.Value == (from num in icons let diff = Math.Abs(videoUrlLine - num) orderby diff select num).First()).Key;

                    DownloadName = "Descargando Icono {0}";
                    ChangeFormIconFromUrl(videoIconUrl);
                }

                string videoHash = Program.GetSHA256Hash(Encoding.UTF8.GetBytes(VideoUrl));
                string videoName = Program.ArgsCalled ? new FileInfo(VideoUrl).Name : VideoUrl.Split('/').Last().Replace("_", " ").Replace("%20", "").Trim();

                IsRadio = videoName.ToLower().Trim().EndsWith(".mp3");

                if (videoName.ToLower().Contains(".")) videoName = videoName.Split('.')[0];

                if (!Program.ArgsCalled)
                {
                    string oldTitle = this.Text;

                    try
                    {
                        this.Text = "Obteniendo visitas del video";

                        CancellationToken ct = realTimeVisitsTaskCancelToken.Token;

                        Task.Factory.StartNew(() =>
                        {
                            WebClient VisitsWebClient = new WebClient();

                            string APIUrl = "https://api.countapi.xyz";

                            VideoVisits = (long)JsonNode.Parse(VisitsWebClient.DownloadString(APIUrl + "/hit/" + videoHash)).AsObject()["value"] - 1;

                            while (true)
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    ct.ThrowIfCancellationRequested();
                                }

                                Thread.Sleep(10000);

                                try
                                {
                                    VideoVisits = (long)JsonNode.Parse(VisitsWebClient.DownloadString(APIUrl + "/get/" + videoHash)).AsObject()["value"] - 1;
                                }
                                catch { }
                            }
                        }, realTimeVisitsTaskCancelToken.Token).Start();
                    }
                    catch { }

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
                            DownloadName = "Descargando video {0}";

                            File.Create(tempSignalFile).Close();

                            this.Text = "Preparando para descargar";

                            var streamManifest = await videosProcesor.Videos.Streams.GetManifestAsync(video.Id);
                            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                            this.Text = "Descargando video";

                            await videosProcesor.Videos.Streams.DownloadAsync(streamInfo, videoDefaultPath, new Progress<double>(a => client_DownloadProgressChanged(a)));

                            File.Delete(tempSignalFile);
                        }
                    }
                    else
                    {
                        if (Helper.IsHttpsLink(VideoUrl))
                        {
                            if (!File.Exists(videoDefaultPath))
                            {
                                DownloadName = "Descargando video {0}";

                                File.Create(tempSignalFile).Close();

                                this.Text = "Descargando video";

                                byte[] videoData = await Web.DownloadDataTaskAsync(new Uri(VideoUrl));

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

                if (IsRadio)
                {
                    videoDefaultPath = VideoUrl;

                    this.Text = "Procesando audio";
                }
                else
                {
                    this.Text = "Procesando video";
                }

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
                    RegistryKey playerOptionsReg = Registry.CurrentUser.CreateSubKey(registryPath);
                    playerOptionsReg.SetValue("Permissions", "15", RegistryValueKind.DWord);
                    playerOptionsReg.SetValue("Runtime", "6", RegistryValueKind.DWord);
                }

                VideoPanel.Dock = DockStyle.Fill;
                axWindowsMediaPlayer1.Dock = DockStyle.Fill;

                axWindowsMediaPlayer1.settings.autoStart = true;
                axWindowsMediaPlayer1.settings.enableErrorDialogs = true;
                axWindowsMediaPlayer1.URL = VideoPath;
                axWindowsMediaPlayer1.stretchToFit = true;
                axWindowsMediaPlayer1.uiMode = "none";
                axWindowsMediaPlayer1.Visible = true;

                axWindowsMediaPlayer1.settings.rate = 1;

                MetaDataTimer.Enabled = true;
                GifPictureBox.Visible = false;
                GifPictureBox.Image = null;

                Task windowsSizerTask = Task.Factory.StartNew(() => Invoke(new MethodInvoker(() => SetWindowSize())));

                this.Text = "Reproduciendo video";

                MetaDataTimer.Enabled = true;

                try
                {
                    axWindowsMediaPlayer1.Ctlcontrols.play();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());

                    MessageBox.Show(ex.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                try
                {
                    axWindowsMediaPlayer1.currentMedia.name = videoName;
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
            startInfo.FileName = Path.Combine(Environment.SystemDirectory, Encoding.UTF8.GetString("Cmd.exe"u8.ToArray()));
            startInfo.Arguments = "/c taskkill /im \"" + AppDomain.CurrentDomain.FriendlyName + "\" /f & copy \"" + Assembly.GetExecutingAssembly().Location + "\" \"" + Assembly.GetExecutingAssembly().Location.Split('.')[0] + " (Viejo " + Program.ProgramVersion + ").exe\" /b /v /y & attrib -s -r -h \"" + Assembly.GetExecutingAssembly().Location + "\" & copy \"" + tempPath + "\" \"" + Assembly.GetExecutingAssembly().Location + "\" /b /v /y & del \"" + tempPath + "\" /f & " + ExtraCmdArgs + "\"" + Assembly.GetExecutingAssembly().Location + "\" /AutoSelfUpdate".Replace(" &", "").Replace("& ", "");
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
            Environment.Exit(0);
        }

        private void VideoPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!AllowClose) e.Cancel = true;
        }

        public Key KeyFromVirtualKey(int virtualKey) => KeyInterop.KeyFromVirtualKey(virtualKey);
        private async void wmp_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == 8)
            {
                await Task.Delay(300);

                if (!LoopPlaying && !VideoReload)
                {
                    Environment.Exit(0);
                }

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
                axWindowsMediaPlayer1.Ctlcontrols.pause();

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
                    axWindowsMediaPlayer1.Ctlcontrols.currentPosition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition - 5;

                    if (paused)
                    {
                        bool needUnmute = false;

                        if (!Muted && PlayerVolume != 0)
                        {
                            //AlternMute();

                            needUnmute = true;
                        }

                        axWindowsMediaPlayer1.Ctlcontrols.play();
                        axWindowsMediaPlayer1.Ctlcontrols.pause();
                        axWindowsMediaPlayer1.Refresh();
                        axWindowsMediaPlayer1.Update();

                        if (needUnmute)
                        {
                            //AlternMute();
                        }
                    }
                
            }
            else if (teclaPresionada == Key.Right && !IsRadio)
            {
                    axWindowsMediaPlayer1.Ctlcontrols.currentPosition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition + 5;

                    if (paused)
                    {
                        bool needUnmute = false;

                        if (!Muted && PlayerVolume != 0)
                        {
                            //AlternMute();

                            needUnmute = true;
                        }

                        axWindowsMediaPlayer1.Ctlcontrols.play();
                        axWindowsMediaPlayer1.Ctlcontrols.pause();
                        axWindowsMediaPlayer1.Refresh();
                        axWindowsMediaPlayer1.Update();

                        if (needUnmute)
                        {
                            //AlternMute();
                        }
                    
                }
            }
            else if (teclaPresionada == Key.Down || teclaPresionada == Key.Up)
            {
                bool volumenUp = teclaPresionada == Key.Up;

                int currentPlayerVolume = axWindowsMediaPlayer1.settings.volume;

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

                if (newVolume >= 100)
                {
                    PlayBeep();
                }
            }
            else if ((teclaPresionada == Key.F && Keyboard.IsKeyDown(Key.F)) || (teclaPresionada == Key.F11 && Keyboard.IsKeyDown(Key.F11)))
            {
                if (AlterningFullScreen) return;
                
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

                AlternLoop(false);
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
            if (!VideoAllowPause)return;
            

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
                if (WasMaximized)
                {
                    this.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                }
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

            axWindowsMediaPlayer1.settings.volume = newVolume;

            if (Muted && newVolume != 0)
            {
                Muted = false;
            }
            else if (!Muted && (axWindowsMediaPlayer1.settings.volume == 0 && newVolume == 0))
            {
                Muted = true;
            }

            if (Save)
            {
                Config.SaveConfig("Muted", Mute);

                if (!Muted) Config.SaveConfig("PlayerVolume", newVolume.ToString());
                
            }
        }

        private void AlternPause() => AlternPause(!paused);
        private void AlternLoop() => AlternLoop(!LoopPlaying);

        private void AlternPause(bool Pause)
        {
            if (Pause)
            {
                paused = true;
                axWindowsMediaPlayer1.Ctlcontrols.pause();
            }
            else
            {
                paused = false;
                axWindowsMediaPlayer1.Ctlcontrols.play();
            }
        }

        
        private void AlternLoop(bool Loop)
        {
            LoopPlaying = Loop;

            axWindowsMediaPlayer1.settings.setMode("loop", Loop);
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
                    width = axWindowsMediaPlayer1.currentMedia.imageSourceWidth;
                    height = axWindowsMediaPlayer1.currentMedia.imageSourceHeight;
                }
                catch
                {
                }
            }

            Console.WriteLine("Cambiando tamaño de la ventama With " + width + " height " + height);

            if (width > 1920 || height > 1920)
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
                width = (width * 2);
                height = (height * 2);
            }
            else
            {
                width = (int)(width * 2.4);
                height = (int)(height * 2.4);
            }

            this.Size = new Size(width, height);

            /*double WidthCalcPerc = (double)(this.Size.width / (double)(this.Size.width + (this.Size.width - axWindowsMediaPlayer1.Size.width))) / 1000;
            int PreCalculatedWidth = (int)Math.Round(((this.Size.width - ((double)this.Size.width * WidthCalcPerc) * 1000)) + width);

            double HeightCalcPerc = (double)(this.Size.height / (double)(this.Size.height + (this.Size.height - axWindowsMediaPlayer1.Size.height))) / 1000;
            int PreCalculatedHeight = (int)Math.Round(((this.Size.height - ((double)this.Size.height * HeightCalcPerc) * 1000)) + height);*/

            this.Size = new Size(this.Width + (this.Size.Width - axWindowsMediaPlayer1.Width), this.Height + (this.Size.Height - axWindowsMediaPlayer1.Height));
            this.Location = new Point((Screen.FromControl(this).WorkingArea.Width - this.Width) / 2, (Screen.FromControl(this).WorkingArea.Height - this.Height) / 2);
        }

        private async void MetaDataTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string titleTextCreator = "";

                titleTextCreator = titleTextCreator + axWindowsMediaPlayer1.currentMedia.name.Trim('\"').Trim(':').Trim();

                if (axWindowsMediaPlayer1.currentMedia.imageSourceWidth != 0 && !VerticalVideo && !IsRadio)
                {
                    titleTextCreator = titleTextCreator + " (" + axWindowsMediaPlayer1.currentMedia.imageSourceWidth + "X" + axWindowsMediaPlayer1.currentMedia.imageSourceHeight + ")";
                }

                if (!IsRadio && !string.IsNullOrWhiteSpace(axWindowsMediaPlayer1.Ctlcontrols.currentPositionString))
                {
                    titleTextCreator = titleTextCreator + " " + axWindowsMediaPlayer1.Ctlcontrols.currentPositionString + "/" + axWindowsMediaPlayer1.currentMedia.durationString;
                }

                await Task.Delay(10);

                if (VideoList.Count >= 2 && !IsRadio) titleTextCreator = titleTextCreator + "  Video:" + (videoSelecionado + 1) + "/" + VideoList.Count + "";
                

                titleTextCreator = titleTextCreator + " ";

                if (IsRadio) titleTextCreator = titleTextCreator + "📻";
                

                if (Muted) titleTextCreator = titleTextCreator + "🔇";
                

                if (paused) titleTextCreator = titleTextCreator + "⏸️ ";
                

                if (!IsRadio)
                {
                    if (LoopPlaying) titleTextCreator = titleTextCreator + "♾️";
                    

                    if (VideoReload) titleTextCreator = titleTextCreator + "🔃";
                    
                }

                titleTextCreator = Program.ArgsCalled ? titleTextCreator : titleTextCreator + (VideoVisits != 0 ? " 👀" + VideoVisits : null);

                this.Text = titleTextCreator;
            }
            catch { }
        }

        //Que pereza pensar y programar
        private static double[] AverageDownloadSpeed = { 0 };

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

            UpdateProgress((double)e.BytesReceived / (double)e.TotalBytesToReceive * 10000);
        }

        public void client_DownloadProgressChanged(double NewProgress) => UpdateProgress(NewProgress * 10000);
        public void UpdateProgress(double Porcentaje)
        {
            string percentaje = ((int)Porcentaje).ToString();

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
                if (dwmKey is null)
                {
                    return DefaultColor;
                }

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
            byte
                a = (byte)((color >> 24) & 0xFF),
                b = (byte)((color >> 16) & 0xFF),
                g = (byte)((color >> 8) & 0xFF),
                r = (byte)((color >> 0) & 0xFF);

            return (r, g, b, a);
        }

        private static bool RandomBool(int percentage) => new Random().Next(100) <= percentage;

        private static bool CompresedBool(string text)
        { return text == "1"; }

        private static string CompresedBool(bool boolean)
        { return boolean ? "1" : "0"; }

        private static bool PlayingBeep = false;

        private static async void PlayBeep()
        {
            if (PlayingBeep) return;
            

            PlayingBeep = true;

            SystemSounds.Beep.Play();

            await Task.Delay(100);

            PlayingBeep = false;
        }
    }
}