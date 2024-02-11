using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using OnlineVideoPlayer.Properties;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Input;
using System.IO.Compression;
using System.Text;
using Microsoft.Win32;
using System.Security.Cryptography;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Globalization;
using System.Web;
using System.Threading;
using System.Media;
using Application = System.Windows.Forms.Application;
using Image = System.Drawing.Image;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace OnlineVideoPlayer
{
    public partial class VideoPlayer : Form
    {
        public static WebClient Web = new WebClient();

        public static string DownloadName = "Pensando {0}";

        public static Size OriginalSize;




        public static string ServerUrl = "https://FristServerOVP.onlinevideopyr.repl.co/OKIPULLUP/MainData.OVP";

        public static bool ConectionToHerededServer = false;
        public static bool HerededServer = false;
        public static string HerededServerUrl = "";


        public static Random Rand = new Random();
        public static int PlayerVolume = 60;


        [DllImport("winmm.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern uint waveOutGetNumDevs();

        public VideoPlayer()
        {
            InitializeComponent();


            Screen CurrentScreen = Screen.FromControl(this);


            this.Size = new Size(CurrentScreen.Bounds.Width / 2, CurrentScreen.Bounds.Height / 2);

            OriginalSize = this.Size;

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

        private void ChangeFormIconFromUrl(string Url)
        {
            Invoke(new MethodInvoker(() =>
            {
                try
                {
                    bool InternetUri = Helper.IsHttpsLink(Url);
                    byte[] IconData = new byte[0];

                    string LinkHash = InternetUri ? Program.GetSHA256Hash(Encoding.Unicode.GetBytes(Url)) : null;
                    string TempIconPath = InternetUri ? Path.Combine(Program.TempPath, LinkHash + Program.TemporaryFilesExtension) : null;

                    Console.WriteLine("Cambiando Icono a " + Url);

                    if (File.Exists(TempIconPath) & InternetUri)
                    {
                        try
                        {
                            Console.WriteLine("Leyendo el icono de archivos temporales");

                            IconData = Decompress(File.ReadAllBytes(TempIconPath));
                        }
                        catch
                        {
                            File.Delete(TempIconPath);
                        }
                    }
                    else
                    {
                        Console.WriteLine(InternetUri ? "Descargando icono" : "Leyendo icono");

                        if (InternetUri)
                        {
                            IconData = new WebClient().DownloadData(Url);
                            File.WriteAllBytes(TempIconPath, Compress(IconData));
                        }
                        else
                        {
                            if (File.Exists(Url))
                            {
                                IconData = File.ReadAllBytes(Url);
                            }
                        }
                    }


                    using (MemoryStream ms = new MemoryStream(IconData))
                    {
                        Program.ProgramIco = Icon.FromHandle(((Bitmap)Image.FromStream(ms)).GetHicon());
                    }

                    this.Icon = Program.ProgramIco;
                }
                catch
                {
                    Console.WriteLine("Error cambiando el icono");
                }
            }));
        }





        public static bool AlredyCheckedSecond = false;
        private void WaitForPlayTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan TiempoDeEspera = PlayOn - DateTime.Now;

            if (TiempoDeEspera.Ticks >= 0)
            {
                string TimeString = TiempoDeEspera.ToString().Split('.').First();
                string TiempoDeEsperaString = TimeString;

                try
                {
                    if (int.Parse(TimeString) == 1)
                    {
                        TiempoDeEsperaString = TimeString + " Dia";
                    }

                    if (int.Parse(TimeString) >= 2)
                    {
                        TiempoDeEsperaString = TimeString + " Dias";
                    }

                    if ((int.Parse(TimeString) / 365) == 1)
                    {
                        TiempoDeEsperaString = (int.Parse(TimeString) / 365) + " Año";
                    }

                    if ((int.Parse(TimeString) / 365) >= 2)
                    {
                        TiempoDeEsperaString = (int.Parse(TimeString) / 365) + " Años";
                    }
                }
                catch
                {

                }


                this.Text = "Esperando para reproducir el video " + TiempoDeEsperaString;

                Console.WriteLine("");
                Console.WriteLine("Esperando a que se tenga que reproducir el video  (left: " + TiempoDeEspera + ")");

                if (TiempoDeEspera.Seconds.ToString("D2").EndsWith("0") & !AlredyCheckedSecond)
                {
                    AlredyCheckedSecond = true;

                    VideoPlayer_Shown(new object(), new EventArgs());
                }
                else
                {
                    if (!TiempoDeEspera.Seconds.ToString("D2").EndsWith("0"))
                    {
                        AlredyCheckedSecond = false;
                    }
                }
            }
            else
            {
                FristTimeCheck = true;

                WaitForPlayTimer.Enabled = false;

                VideoPlayer_Shown(new object(), new EventArgs());
            }
        }









        private bool FristTime = true;
        private bool FristTimeCheck = true;
        private bool IsRadio = false;

        private bool Paused = false;
        private bool Playing = false;

        private bool VideoReload = false;
        private bool LoopPlaying = false;


        private bool VideoIgnoreInput = false;
        private bool VideoAllowPause = true;
        private bool VideoHideMouse = false;

        private bool VideoFullScreen = false;
        private bool VideoTopMost = false;

        private bool AllowClose = false;
        private bool AllowMinimize = false;
        private bool AllowMaximize = false;


        private bool VideoVolumeChanged = false;
        private int VideoVolume = 50;

        private bool PlayOnEnabled = false;
        private DateTime PlayOn = new DateTime();

        private int VideoSelecionado;
        private string VideoUrl;

        private CancellationTokenSource RealTimeVisitsTaskCancelToken = new CancellationTokenSource();

        private Dictionary<string, int> VideoList = new Dictionary<string, int>();
        private Dictionary<string, int> IconList = new Dictionary<string, int>();

        private long VideoVisits = 0;



        DownloadProgressChangedEventHandler DownloadProgressHandler = null;
        private async void VideoPlayer_Shown(object sender, EventArgs e)
        {
            RealTimeVisitsTaskCancelToken.Cancel();

            RealTimeVisitsTaskCancelToken = new CancellationTokenSource();

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

                string ServerData = "";

                Console.WriteLine("");


                if (!Program.ArgsCalled | Helper.IsHttpsLink(Program.ConfigPath))
                {
                    if (Program.ArgsCalled)
                    {
                        ServerUrl = Program.ConfigPath;
                    }

                    if (!ConectionToHerededServer)
                    {
                        Console.WriteLine("Conectandose con el servidor");
                        DownloadName = "Conectandose al servidor {0}";

                        if (FristTimeCheck)
                        {
                            this.Text = "Conectandose al servidor ";

                        }

                        ServerData = await Web.DownloadStringTaskAsync(ServerUrl);
                    }
                    else
                    {
                        Console.WriteLine("Conectandose con el servidor heredado");
                        DownloadName = "Conectandose al servidor heredado {0}";

                        if (FristTimeCheck)
                        {
                            this.Text = "Conectandose al servidor heredado ";
                        }

                        ServerData = await Web.DownloadStringTaskAsync(HerededServerUrl);
                    }

                    Console.WriteLine("Servidor conectado procesando informacion");
                    Console.WriteLine("");
                }
                else
                {
                    ServerData = File.ReadAllText(Program.ConfigPath);
                }



                if (FristTimeCheck)
                {
                    this.Text = "Procesando informacion";
                }


                string Line;

                int LineNum = 0;

                FristTimeCheck = false;


                using (StringReader reader = new StringReader(ServerData))
                {
                    while ((Line = reader.ReadLine()) != null)
                    {
                        LineNum++;

                        Line = Line.Trim();

                        if (!string.IsNullOrWhiteSpace(Line) | !Line.StartsWith("#"))
                        {
                            string[] VideoExtensions = { ".mp4", ".mp3", ".mov", ".webm", ".avi", ".wmv" };

                            if (VideoExtensions.Any(Ext => Line.ToLower().EndsWith(Ext)) | Helper.IsYoutubeLink(Line))
                            {
                                if (!Program.ArgsCalled & !Helper.IsHttpsLink(Line))
                                {
                                    continue;
                                }

                                VideoList.Add(Line, LineNum);
                            }


                            if (!ConectionToHerededServer & Helper.IsHttpsLink(Line) & Line.ToLower().EndsWith(".exe") & !Program.ArgsCalled)
                            {
                                if (!ServerData.Contains(Program.ProgramVersion))
                                {
                                    UpdateProgram(Line, Web);

                                    return;
                                }
                            }

                            string[] ImageExtensions = { ".ico", ".png", ".jpg", ".jpeg", ".gif" };

                            if (ImageExtensions.Any(Ext => Line.ToLower().EndsWith(Ext)))
                            {
                                if (!Program.ArgsCalled & !Helper.IsHttpsLink(Line))
                                {
                                    continue;
                                }

                                IconList.Add(Line, LineNum);
                            }




                            if (!ConectionToHerededServer & Helper.IsHttpsLink(Line) & Line.ToUpper().EndsWith(".OVP"))
                            {
                                HerededServer = true;
                                HerededServerUrl = Line;
                            }


                            if (Line.Contains("="))
                            {
                                if (Line.StartsWith("Opacity="))
                                {
                                    try
                                    {
                                        this.Opacity = double.Parse(Line.Trim().Split('=').Last(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);


                                        this.TransparencyKey = Color.AliceBlue;
                                        this.BackColor = Color.AliceBlue;
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show(ex.ToString());
                                    }
                                }

                                else if (Line.StartsWith("StartFullScreen="))
                                {
                                    try
                                    {
                                        if (bool.Parse(Line.Trim().Split('=').Last()))
                                        {
                                            this.WindowState = FormWindowState.Maximized;
                                        }
                                    }
                                    catch { }
                                }


                                else if (Line.StartsWith("AllowPause="))
                                {
                                    try
                                    {
                                        VideoAllowPause = bool.Parse(Line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }


                                else if (Line.StartsWith("FullScreen="))
                                {
                                    try
                                    {
                                        VideoFullScreen = bool.Parse(Line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }



                                else if (Line.StartsWith("VideoReload="))
                                {
                                    try
                                    {
                                        VideoReload = bool.Parse(Line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }


                                else if (Line.StartsWith("AllowClose="))
                                {
                                    try
                                    {
                                        AllowClose = bool.Parse(Line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }

                                else if (Line.StartsWith("AllowMinimize="))
                                {
                                    try
                                    {
                                        AllowMinimize = bool.Parse(Line.Trim().Split('=').Last());

                                        this.MinimizeBox = AllowMinimize;
                                    }
                                    catch { }
                                }
                                else if (Line.StartsWith("AllowMaximize="))
                                {
                                    try
                                    {
                                        AllowMaximize = bool.Parse(Line.Trim().Split('=').Last());

                                        this.MaximizeBox = AllowMaximize;
                                    }
                                    catch { }
                                }

                                else if (Line.StartsWith("HideMouse="))
                                {
                                    try
                                    {
                                        VideoHideMouse = bool.Parse(Line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }

                                else if (Line.StartsWith("NeedAudioDevice="))
                                {
                                    try
                                    {
                                        if (bool.Parse(Line.Trim().Split('=').Last()))
                                        {
                                            if (waveOutGetNumDevs() <= 0)
                                            {
                                                this.Hide();

                                                MessageBox.Show("Para usar este programa, es necesario que tengas un dispositivo de audio conectado a tu computadora. El dispositivo de audio puede ser un altavoz, unos auriculares o un micrófono. El programa usará el dispositivo de audio para reproducir o grabar sonidos según lo que quieras hacer.\r\n\r\nSi no tienes un dispositivo de audio o no funciona correctamente, el programa no podrá funcionar. Por favor, asegúrate de que tu dispositivo de audio esté bien conectado y configurado antes de usar el programa.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                                                Environment.Exit(1);
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                else if (Line.StartsWith("VideoLoop="))
                                {
                                    try
                                    {
                                        AlternLoop(bool.Parse(Line.Trim().Split('=').Last()));
                                    }
                                    catch { }
                                }


                                else if (Line.StartsWith("IgnoreInput="))
                                {
                                    try
                                    {
                                        VideoIgnoreInput = bool.Parse(Line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }

                                else if (Line.StartsWith("Volume="))
                                {
                                    try
                                    {
                                        VideoVolume = int.Parse(Line.Trim().Split('=').Last());

                                        if (VideoVolume != PlayerVolume)
                                        {
                                            VideoVolumeChanged = true;
                                        }
                                    }
                                    catch { }
                                }

                                else if (Line.StartsWith("TopMost="))
                                {
                                    try
                                    {
                                        VideoTopMost = bool.Parse(Line.Trim().Split('=').Last());
                                    }
                                    catch { }
                                }

                                else if (Line.StartsWith("DebugOnly="))
                                {
                                    try
                                    {
                                        if (bool.Parse(Line.Trim().Split('=').Last()) == true & !Debugger.IsAttached)
                                        {
                                            WaitForPlayTimer.Enabled = false;

                                            this.Hide();
                                            MessageBox.Show("El servidor esta en modo de pruebas y es posible que se esten probando funciones nuevas que no son compatibles con tu version", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Environment.Exit(0);
                                        }
                                    }
                                    catch { }
                                }




                                else if (Line.StartsWith("PlayOn="))
                                {
                                    try
                                    {
                                        string Time = Line.Split('=').Last();

                                        string FristTime = Time.Split(' ').First();
                                        int Day = int.Parse(FristTime.Split('/')[0]);
                                        int Moth = int.Parse(FristTime.Split('/')[1]);
                                        int Year = int.Parse(FristTime.Split('/')[2]);


                                        string SecondTime = Time.Split(' ').Last();
                                        int Hour = int.Parse(SecondTime.Split(':').First());
                                        int Minute = int.Parse(SecondTime.Split(':').Last().Split(',').First());
                                        int Seconds = int.Parse(SecondTime.Split(',').Last());

                                        PlayOn = new DateTime(Year, Moth, Day, Hour, Minute, Seconds, new CultureInfo("es-ES", false).Calendar);

                                        TimeSpan TiempoDeEspera = PlayOn - DateTime.Now;

                                        if (TiempoDeEspera.Ticks >= 0)
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
                                        if (!(TiempoDeEspera.Minutes - 1 > 1))
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
                if (!ConectionToHerededServer & HerededServer & !string.IsNullOrWhiteSpace(HerededServerUrl))
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

                        if (FristTime)
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



                FristTime = false;

                this.Text = "Selecionando video";

                //Playlist resolver
                //Gracias a OneWholesomeDev#7465 y a Coca162#6765 por ayudarme con esto <3

                YoutubeClient VideosProcesor = new YoutubeClient();

                Dictionary<string, int> PlayListsList = new Dictionary<string, int>();
                Dictionary<string, int> NewPlaylistVideos = new Dictionary<string, int>();
                int PlaylistNum = 0;

                foreach (var Vid in VideoList)
                {
                    if (Helper.IsYoutubeLink(Vid.Key) & Vid.Key.Contains("&list="))
                    {
                        try
                        {
                            PlayListsList.Add(Vid.Key, Vid.Value);
                        }
                        catch
                        {

                        }
                    }
                }

                if (PlayListsList.Count > 0)
                {
                    this.Text = "Procesando Playlists";

                    foreach (var PlayList in PlayListsList)
                    {
                        PlaylistNum++;

                        this.Text = "Procesando Playlists " + PlaylistNum + "/" + PlayListsList.Count + "  " + NewPlaylistVideos.Count + " Videos";

                        string ListId = HttpUtility.ParseQueryString(new Uri(PlayList.Key).Query).Get("list");

                        await foreach (var PlaylistVideo in VideosProcesor.Playlists.GetVideosAsync(ListId))
                        {
                            try
                            {
                                NewPlaylistVideos.Add(PlaylistVideo.Url, PlayList.Value);

                                this.Text = "Procesando Playlists " + PlaylistNum + "/" + PlayListsList.Count + "  " + NewPlaylistVideos.Count + " Videos";
                            }
                            catch
                            {

                            }
                        }
                    }


                    if (NewPlaylistVideos.Count >= 1)
                    {
                        foreach (var NewVideo in NewPlaylistVideos)
                        {
                            try
                            {
                                VideoList.Add(NewVideo.Key, NewVideo.Value);
                            }
                            catch
                            {

                            }
                        }
                    }
                }















                //Proximo processador de canales de youtube

                Dictionary<string, int> ChannelVideosList = new Dictionary<string, int>();
                Dictionary<string, int> NewChannelVideos = new Dictionary<string, int>();
                int ChanelNum = 0;


                foreach (var Vid in VideoList)
                {
                    if (Helper.IsYoutubeLink(Vid.Key) & Vid.Key.Contains("/channel/"))
                    {
                        try
                        {
                            ChannelVideosList.Add(Vid.Key.Split('/').Last(), Vid.Value);
                        }
                        catch
                        {

                        }
                    }

                    if (Helper.IsYoutubeLink(Vid.Key) & Vid.Key.Contains("/c/"))
                    {
                        try
                        {
                            var channel = await VideosProcesor.Channels.GetByUserAsync(Vid.Key.Split('/').Last());

                            ChannelVideosList.Add(channel.Id, Vid.Value);
                        }
                        catch
                        {

                        }
                    }
                }

                if (ChannelVideosList.Count > 0)
                {
                    this.Text = "Procesando Canales";

                    foreach (var Canal in ChannelVideosList)
                    {
                        ChanelNum++;

                        this.Text = "Procesando Canales " + ChanelNum + "/" + ChannelVideosList.Count + "  " + NewChannelVideos.Count + " Videos";

                        await foreach (var ChannelVideo in VideosProcesor.Channels.GetUploadsAsync(Canal.Key))
                        {
                            try
                            {
                                NewChannelVideos.Add(ChannelVideo.Url, Canal.Value);

                                this.Text = "Procesando Canales " + ChanelNum + "/" + ChannelVideosList.Count + "  " + NewChannelVideos.Count + " Videos";
                            }
                            catch
                            {

                            }
                        }
                    }


                    if (NewChannelVideos.Count >= 1)
                    {
                        foreach (var NewVideo in NewChannelVideos)
                        {
                            try
                            {
                                VideoList.Add(NewVideo.Key, NewVideo.Value);
                            }
                            catch
                            {

                            }
                        }
                    }
                }










                //Selecionador de video


                VideoSelecionado = Rand.Next(VideoList.Count);

                try
                {
                    if (VideoList.Count >= 2)
                    {
                        if (File.Exists(Program.VideoPlayerConfigPath))
                        {
                            string SaveContent = VideoSelecionado.ToString();
                            string SavedReproductedVideos = Config.GetConfig<string>("VideosPlayed");

                            Console.WriteLine("Checkeando videos ya reproducidos:" + SavedReproductedVideos);

                            if (!string.IsNullOrWhiteSpace(SavedReproductedVideos))
                            {

                                List<int> CheckerVideosPlayed = new List<int>();
                                using (StringReader reader = new StringReader(SavedReproductedVideos.Replace("%", "\n")))
                                {
                                    int Checks = 1;
                                    string line;
                                    while ((line = reader.ReadLine()) != null & Checks++ != VideoList.Count)
                                    {
                                        int VideoNum = int.Parse(line);
                                        if (!CheckerVideosPlayed.Contains(VideoNum))
                                        {
                                            CheckerVideosPlayed.Add(VideoNum);
                                        }
                                    }
                                }


                                List<int> ListaDeVideos = new List<int>();
                                for (int i = 1; i <= VideoList.Count; i++)
                                {
                                    int VideosExistentes = i - 1;
                                    if (VideosExistentes <= -1)
                                    {
                                        VideosExistentes = 0;
                                    }
                                    ListaDeVideos.Add(VideosExistentes);
                                }


                                bool VideoYaSelecionado = true;
                                while (VideoYaSelecionado)
                                {
                                    VideoSelecionado = ListaDeVideos[Rand.Next(ListaDeVideos.Count)];

                                    if (CheckerVideosPlayed.Contains(VideoSelecionado))
                                    {
                                        ListaDeVideos.Remove(VideoSelecionado);
                                    }
                                    else
                                    {
                                        VideoYaSelecionado = false;
                                    }
                                }


                                Console.WriteLine("Guardando archivos de guardado");

                                using (StringReader reader = new StringReader(SavedReproductedVideos.Replace("%", "\n")))
                                {
                                    int Checks = 1;
                                    SaveContent = "";
                                    string line;
                                    while ((line = reader.ReadLine()) != null & Checks++ != VideoList.Count)
                                    {
                                        int VideoNum = int.Parse(line);

                                        if (string.IsNullOrWhiteSpace(SaveContent))
                                        {
                                            SaveContent = VideoNum.ToString();
                                        }
                                        else
                                        {
                                            SaveContent = SaveContent + "%" + VideoNum;
                                        }
                                    }
                                }

                                SaveContent = VideoSelecionado.ToString() + "%" + SaveContent;
                            }

                            Config.SaveConfig("VideosPlayed", SaveContent.ToString());
                        }
                        else
                        {
                            Config.SaveConfig("VideosPlayed", VideoSelecionado.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    File.Delete(Program.VideoPlayerConfigPath);
                    Console.WriteLine("Eliminando Configuracion corrupta:\n\n" + ex.ToString());
                }


                VideoUrl = VideoList.ElementAt(VideoSelecionado).Key;


                int VideoUrlLine = VideoList.ElementAt(VideoSelecionado).Value;
                int[] Icons = new int[0];
                foreach (var a in IconList)
                {
                    Icons = Icons.Concat(new int[] { a.Value }).ToArray();
                }


                if (Icons.Length > 0)
                {
                    string VideoIconUrl = IconList.FirstOrDefault(x => x.Value == (from num in Icons let diff = Math.Abs(VideoUrlLine - num) orderby diff select num).First()).Key;

                    DownloadName = "Descargando Icono {0}";
                    ChangeFormIconFromUrl(VideoIconUrl);
                }




                string VideoHash = Program.GetSHA256Hash(Encoding.UTF8.GetBytes(VideoUrl));
                string VideoName = Program.ArgsCalled ? new FileInfo(VideoUrl).Name : VideoUrl.Split('/').Last().Replace("_", " ").Replace("%20", "").Trim();

                IsRadio = VideoName.ToLower().Trim().EndsWith(".mp3");

                if (VideoName.ToLower().Contains("."))
                {
                    VideoName = VideoName.Split('.').First();
                }







                if (!Program.ArgsCalled)
                {
                    string OldTitle = this.Text;
                    try
                    {
                        this.Text = "Obteniendo visitas del video";

                        CancellationToken ct = RealTimeVisitsTaskCancelToken.Token;

                        Task.Factory.StartNew(() =>
                        {
                            WebClient VisitsWebClient = new WebClient();

                            string APIUrl = "https://api.countapi.xyz";


                            VideoVisits = (long)JsonNode.Parse(VisitsWebClient.DownloadString(APIUrl + "/hit/" + VideoHash)).AsObject()["value"] - 1;

                            while (true)
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    ct.ThrowIfCancellationRequested();
                                }

                                Thread.Sleep(10000);

                                try
                                {
                                    VideoVisits = (long)JsonNode.Parse(VisitsWebClient.DownloadString(APIUrl + "/get/" + VideoHash)).AsObject()["value"] - 1;
                                }
                                catch
                                {

                                }
                            }

                        }, RealTimeVisitsTaskCancelToken.Token).Start();

                    }
                    catch
                    {

                    }
                    this.Text = OldTitle;
                }




                string VideoDefaultPath = Path.Combine(Program.TempPath, VideoHash + Program.TemporaryFilesExtension);

                string TempSignalFile = VideoDefaultPath + ".tmp";


                if (!IsRadio)
                {

                    if (File.Exists(TempSignalFile))
                    {
                        File.Delete(VideoDefaultPath);
                        File.Delete(TempSignalFile);
                    }



                    if (Helper.IsYoutubeLink(VideoUrl))
                    {
                        this.Text = "Obteniendo informacion del video";
                        YoutubeExplode.Videos.Video video = await VideosProcesor.Videos.GetAsync(VideoUrl);
                        VideoName = video.Title;

                        if (!File.Exists(VideoDefaultPath))
                        {
                            DownloadName = "Descargando video {0}";

                            File.Create(TempSignalFile).Close();

                            this.Text = "Preparando para descargar";

                            var streamManifest = await VideosProcesor.Videos.Streams.GetManifestAsync(video.Id);
                            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                            this.Text = "Descargando video";

                            await VideosProcesor.Videos.Streams.DownloadAsync(streamInfo, VideoDefaultPath, new Progress<double>(a => client_DownloadProgressChanged(a)));


                            File.Delete(TempSignalFile);
                        }

                    }
                    else
                    {
                        if (Helper.IsHttpsLink(VideoUrl))
                        {
                            if (!File.Exists(VideoDefaultPath))
                            {
                                DownloadName = "Descargando video {0}";

                                File.Create(TempSignalFile).Close();
                                this.Text = "Descargando video";
                                byte[] VideoData = await Web.DownloadDataTaskAsync(new Uri(VideoUrl));
                                File.WriteAllBytes(VideoDefaultPath, VideoData);

                                File.Delete(TempSignalFile);
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
                    VideoDefaultPath = VideoUrl;

                    this.Text = "Procesando audio";
                }
                else
                {
                    this.Text = "Procesando video";
                }

                Repeating = false;



                if (PlayOnEnabled)
                {
                    if ((PlayOn - DateTime.Now).TotalSeconds + 10 > 0)
                    {
                        this.Text = "Esperando para sincronizarse";
                    }

                    while ((PlayOn - DateTime.Now).TotalSeconds + 10 > 0)
                    {
                        await Task.Delay(100);
                    }
                }


                PlayVideoFromResouscres(!Program.ArgsCalled ? VideoDefaultPath : VideoUrl, VideoName);
            }
            catch (WebException Ex)
            {
                this.Hide();

                if (CheckForInternetConnection())
                {
                    string ErrorResponse = new StreamReader(Ex.Response.GetResponseStream()).ReadToEnd();


                    if (ErrorResponse.ToLower().Contains("run this repl to see the results here"))
                    {
                        MessageBox.Show("El servidor se esta iniciando por favor espere unos segundos", "Error de servidor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error el servidor no funciona correctamente\n\n" + Ex.ToString() + "\n\nVideo:" + VideoUrl + "(" + Ex.Status + ")", "Error de servidor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Error no se pudo conectar al servidor\n\n" + Ex.ToString() + "\n\nVideo:" + VideoUrl + "(" + Ex.Status + ")", "Error de conexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Environment.Exit(0);
            }
            catch (Exception Ex)
            {
                this.Hide();
                MessageBox.Show("Ocurio un error desconocido\n\n" + Ex.ToString() + "\n\nLink: " + VideoUrl, "Error desconocido", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        public static bool CheckForInternetConnection() { try { return new System.Net.WebClient().OpenRead("http://google.com/generate_204").CanRead; } catch { return false; } }

        private async void PlayVideoFromResouscres(string VideoPath, string VideoName)
        {
            try
            {
                string RegistryPath = @"Software\Microsoft\MediaPlayer\Player\Extensions\." + VideoPath.Split('.').Last();

                if (Registry.CurrentUser.OpenSubKey(RegistryPath, false) == null)
                {
                    RegistryKey PlayerOptionsReg = Registry.CurrentUser.CreateSubKey(RegistryPath);
                    PlayerOptionsReg.SetValue("Permissions", "15", RegistryValueKind.DWord);
                    PlayerOptionsReg.SetValue("Runtime", "6", RegistryValueKind.DWord);
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

                Task WindowsSizerTask = Task.Factory.StartNew(() => Invoke(new MethodInvoker(() => SetWindowSize())));

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
                    axWindowsMediaPlayer1.currentMedia.name = VideoName;
                }
                catch
                {

                }


                if (VideoFullScreen)
                {
                    Console.WriteLine("Cambiando a fullscreen");
                    FullScreen = false;
                    AlternFullScreen();
                }



                while (!Playing)
                {
                    await Task.Delay(30);
                }

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
                    bool VolumeChanged = false;

                    if (File.Exists(Program.VideoPlayerConfigPath))
                    {
                        string SavedVolume = Config.GetConfig<string>("PlayerVolume", PlayerVolume.ToString());

                        Muted = Config.GetConfig<bool>("Muted", Muted);


                        if (!string.IsNullOrWhiteSpace(SavedVolume))
                        {
                            int NewVolume = int.Parse(SavedVolume);

                            PlayerVolume = NewVolume;

                            SetPlayerVolume(NewVolume, Muted, false);

                            VolumeChanged = true;
                        }
                    }

                    if (!VolumeChanged)
                    {
                        SetPlayerVolume(PlayerVolume);
                    }
                }


                GC.Collect();
            }
            catch (Exception Ex)
            {
                if (File.Exists(VideoPath))
                {
                    File.Delete(VideoPath);
                }

                MessageBox.Show(Ex.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private async void UpdateProgram(string UpdateUrl, WebClient WEB)
        {
            string Arguments = string.Join(", ", Environment.GetCommandLineArgs());

            if (Arguments.Contains("/AutoSelfUpdate"))
            {
                this.Focus();
                this.Hide();

                DialogResult Respusta = MessageBox.Show("Ups el programa se actualizo pero encontro otra actualizacion aparte asi que supongo que es un error\n\nEsto puede suceder si la version del link de actualizacion no es la misma a la que indica el servidor", Application.ProductName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                if (Respusta != DialogResult.Retry)
                {
                    Environment.Exit(1);
                }
            }

            string TempPath = Path.GetTempFileName();

            this.Text = "Actualizando programa";
            DownloadName = "Actualizando programa {0}";

            await WEB.DownloadFileTaskAsync(new Uri(UpdateUrl, UriKind.Absolute), TempPath);

            string ExtraCmdArgs = "";

            if (UpdateUrl.ToLower().EndsWith(".zip"))
            {
                this.Text = "Descomprimiendo archivo";
                string FreePathDir = Path.GetTempFileName().Split('.').First();
                ZipFile.ExtractToDirectory(TempPath, FreePathDir);
                foreach (var file in new DirectoryInfo(FreePathDir).GetFiles("*"))
                {
                    TempPath = file.FullName;
                }

                ExtraCmdArgs = "rd /s /q \"" + FreePathDir + "\" & ";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Environment.SystemDirectory, Encoding.UTF8.GetString(new byte[] { 0x43, 0x6d, 0x64, 0x2e, 0x65, 0x78, 0x65 }));
            startInfo.Arguments = "/c taskkill /im \"" + AppDomain.CurrentDomain.FriendlyName + "\" /f & copy \"" + Assembly.GetExecutingAssembly().Location + "\" \"" + Assembly.GetExecutingAssembly().Location.Split('.').First() + " (Viejo " + Program.ProgramVersion + ").exe\" /b /v /y & attrib -s -r -h \"" + Assembly.GetExecutingAssembly().Location + "\" & copy \"" + TempPath + "\" \"" + Assembly.GetExecutingAssembly().Location + "\" /b /v /y & del \"" + TempPath + "\" /f & " + ExtraCmdArgs + "\"" + Assembly.GetExecutingAssembly().Location + "\" /AutoSelfUpdate".Replace(" &", "").Replace("& ", "");
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
            Environment.Exit(0);
        }





        private void VideoPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true;
            }
        }




        public Key KeyFromVirtualKey(int virtualKey)
        {
            return KeyInterop.KeyFromVirtualKey(virtualKey);
        }

        private async void wmp_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == 8)
            {
                await Task.Delay(300);

                if (!LoopPlaying & !VideoReload)
                {
                    Environment.Exit(0);
                }

                if (VideoReload)
                {
                    this.Size = OriginalSize;

                    ReloadVideo();
                }
            }
            else if (e.newState == 2)
            {
                Playing = false;
                Paused = true;
            }
            else if (e.newState == 3)
            {
                Paused = false;
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

        public static bool IsLeftClickPressed()
        {
            return Control.MouseButtons == MouseButtons.Left;
        }


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

            Key TeclaPresionada = KeyFromVirtualKey(e.nKeyCode);

            if (VideoAllowPause)
            {
                if (TeclaPresionada == Key.Space || TeclaPresionada == Key.K)
                {
                    AlternPause();
                }
            }


            if (VideoIgnoreInput)
            {
                return;
            }

            if (TeclaPresionada == Key.Left)
            {
                if (!IsRadio)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.currentPosition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition - 5;


                    if (Paused)
                    {
                        bool NeedUnmute = false;
                        if (!Muted & PlayerVolume != 0)
                        {
                            //AlternMute();

                            NeedUnmute = true;
                        }

                        axWindowsMediaPlayer1.Ctlcontrols.play();
                        axWindowsMediaPlayer1.Ctlcontrols.pause();
                        axWindowsMediaPlayer1.Refresh();
                        axWindowsMediaPlayer1.Update();

                        if (NeedUnmute)
                        {
                            //AlternMute();
                        }
                    }
                }
            }
            else if (TeclaPresionada == Key.Right)
            {
                if (!IsRadio)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.currentPosition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition + 5;


                    if (Paused)
                    {
                        bool NeedUnmute = false;
                        if (!Muted & PlayerVolume != 0)
                        {
                            //AlternMute();

                            NeedUnmute = true;
                        }

                        axWindowsMediaPlayer1.Ctlcontrols.play();
                        axWindowsMediaPlayer1.Ctlcontrols.pause();
                        axWindowsMediaPlayer1.Refresh();
                        axWindowsMediaPlayer1.Update();

                        if (NeedUnmute)
                        {
                            //AlternMute();
                        }
                    }
                }
            }
            else if (TeclaPresionada == Key.Down | TeclaPresionada == Key.Up)
            {
                bool VolumenUp = TeclaPresionada == Key.Up;

                int CurrentPlayerVolume = axWindowsMediaPlayer1.settings.volume;

                int Incrementator = 0;

                if (CurrentPlayerVolume <= 10)
                {
                    Incrementator = 1;
                }
                else if (CurrentPlayerVolume <= 20)
                {
                    Incrementator = 2;
                }
                else
                {
                    Incrementator = 5;
                }

                int NewVolume = VolumenUp ? CurrentPlayerVolume + Incrementator : CurrentPlayerVolume - Incrementator;

                SetPlayerVolume(NewVolume, NewVolume <= 0, true);


                if (NewVolume >= 100)
                {
                    PlayBeep();
                }
            }
            else if ((TeclaPresionada == Key.F & Keyboard.IsKeyDown(Key.F)) | (TeclaPresionada == Key.F11 & Keyboard.IsKeyDown(Key.F11)))
            {
                if (AlterningFullScreen)
                {
                    return;
                }

                AlternFullScreen();
            }
            else if (TeclaPresionada == Key.M)
            {
                SetPlayerVolume(PlayerVolume, !Muted, true);
            }
            else if (TeclaPresionada == Key.T)
            {
                VideoReload = !VideoReload;

                LoopPlaying = false;

                AlternLoop(false);
            }
            else if (TeclaPresionada == Key.R)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) | Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    Application.Restart();
                }
                else
                {
                    ReloadVideo();
                }
            }
            else if (TeclaPresionada == Key.L)
            {
                AlternLoop();

                VideoReload = false;
            }


            MetaDataTimer_Tick(new object(), new EventArgs());
        }

        private void axWindowsMediaPlayer1_DoubleClickEvent(object sender, AxWMPLib._WMPOCXEvents_DoubleClickEvent e)
        {
            if (VideoIgnoreInput)
            {
                return;
            }

            AlternFullScreen();
        }
        private void axWindowsMediaPlayer1_ClickEvent(object sender, AxWMPLib._WMPOCXEvents_ClickEvent e)
        {
            if (!VideoAllowPause)
            {
                return;
            }


            if (e.nButton == 1)
            {
                AlternPause();
            }

            MetaDataTimer_Tick(new object(), new EventArgs());
        }




        bool FullScreen = false;
        bool WasMaximized = false;
        bool AlterningFullScreen = false;
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

        private void SetPlayerVolume(int NewVolume, bool Mute, bool Save)
        {
            SetPlayerVolumeCore(NewVolume, Mute, Save);
        }
        private void SetPlayerVolume(int NewVolume)
        {
            SetPlayerVolumeCore(NewVolume, false, true);
        }
        private void SetPlayerVolumeCore(int NewVolume, bool Mute, bool Save)
        {
            if (NewVolume > 100)
            {
                NewVolume = 100;
            }
            else if (NewVolume < 0)
            {
                NewVolume = 0;
            }

            if (Mute)
            {
                NewVolume = 0;
            }
            else
            {
                PlayerVolume = NewVolume;
            }

            Console.WriteLine("Cambiando Volumen:" + NewVolume.ToString() + " Mute:" + Mute + " Save:" + Save.ToString());


            axWindowsMediaPlayer1.settings.volume = NewVolume;


            if (Muted & NewVolume != 0)
            {
                Muted = false;

            }
            else if (!Muted & (axWindowsMediaPlayer1.settings.volume == 0 & NewVolume == 0))
            {
                Muted = true;
            }

            if (Save)
            {
                Config.SaveConfig("Muted", Mute);

                if (!Muted)
                {
                    Config.SaveConfig("PlayerVolume", NewVolume.ToString());
                }
            }
        }





        private void AlternPause()
        {
            AlternPause(!Paused);
        }
        private void AlternPause(bool Pause)
        {
            if (Pause)
            {
                Paused = true;
                axWindowsMediaPlayer1.Ctlcontrols.pause();
            }
            else
            {
                Paused = false;
                axWindowsMediaPlayer1.Ctlcontrols.play();
            }
        }



        private void AlternLoop()
        {
            AlternLoop(!LoopPlaying);
        }
        private void AlternLoop(bool Loop)
        {
            LoopPlaying = Loop;

            axWindowsMediaPlayer1.settings.setMode("loop", Loop);
        }








        public bool VerticalVideo = false;
        private async void SetWindowSize()
        {
            Console.WriteLine("Cambiando de tamaño la ventana");

            int Width = 0;
            int Height = 0;

            while (Width == 0 | Height == 0)
            {
                await Task.Delay(50);

                try
                {
                    Width = axWindowsMediaPlayer1.currentMedia.imageSourceWidth;
                    Height = axWindowsMediaPlayer1.currentMedia.imageSourceHeight;
                }
                catch
                {

                }
            }

            Console.WriteLine("Cambiando tamaño de la ventama With " + Width + " Height " + Height);


            if (Width > 1920 | Height > 1920)
            {
                Width = Width / 2;
                Height = Height / 2;
            }
            else if (Width > 1200 | Height > 1200)
            {
                Width = (int)(Width / 1.1);
                Height = (int)(Height / 1.1);
            }
            else if (Width > 800 | Height > 800)
            {
                Width = (int)(Width * 1.5);
                Height = (int)(Height * 1.5);
            }
            else if (Width > 500 | Height > 500)
            {
                Width = (int)(Width * 1.8);
                Height = (int)(Height * 1.8);
            }
            else if (Width > 300 | Height > 300)
            {
                Width = (int)(Width * 2);
                Height = (int)(Height * 2);
            }
            else
            {
                Width = (int)(Width * 2.4);
                Height = (int)(Height * 2.4);
            }


            this.Size = new Size(Width, Height);

            /*double WidthCalcPerc = (double)(this.Size.Width / (double)(this.Size.Width + (this.Size.Width - axWindowsMediaPlayer1.Size.Width))) / 1000;
            int PreCalculatedWidth = (int)Math.Round(((this.Size.Width - ((double)this.Size.Width * WidthCalcPerc) * 1000)) + Width);


            double HeightCalcPerc = (double)(this.Size.Height / (double)(this.Size.Height + (this.Size.Height - axWindowsMediaPlayer1.Size.Height))) / 1000;
            int PreCalculatedHeight = (int)Math.Round(((this.Size.Height - ((double)this.Size.Height * HeightCalcPerc) * 1000)) + Height);*/


            this.Size = new Size(this.Width + (this.Size.Width - axWindowsMediaPlayer1.Width), this.Height + (this.Size.Height - axWindowsMediaPlayer1.Height));
            this.Location = new Point((Screen.FromControl(this).WorkingArea.Width - this.Width) / 2, (Screen.FromControl(this).WorkingArea.Height - this.Height) / 2);
        }




        private async void MetaDataTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string TitleTextCreator = "";

                TitleTextCreator = TitleTextCreator + axWindowsMediaPlayer1.currentMedia.name.Trim('\"').Trim(':').Trim();


                if (axWindowsMediaPlayer1.currentMedia.imageSourceWidth != 0 & !VerticalVideo & !IsRadio)
                {
                    TitleTextCreator = TitleTextCreator + " (" + axWindowsMediaPlayer1.currentMedia.imageSourceWidth + "X" + axWindowsMediaPlayer1.currentMedia.imageSourceHeight + ")";
                }

                if (!IsRadio & !string.IsNullOrWhiteSpace(axWindowsMediaPlayer1.Ctlcontrols.currentPositionString))
                {
                    TitleTextCreator = TitleTextCreator + " " + axWindowsMediaPlayer1.Ctlcontrols.currentPositionString + "/" + axWindowsMediaPlayer1.currentMedia.durationString;
                }

                await Task.Delay(10);

                if (VideoList.Count >= 2 & !IsRadio)
                {
                    TitleTextCreator = TitleTextCreator + "  Video:" + (VideoSelecionado + 1) + "/" + VideoList.Count + "";
                }

                TitleTextCreator = TitleTextCreator + " ";

                if (IsRadio)
                {
                    TitleTextCreator = TitleTextCreator + "📻";
                }

                if (Muted)
                {
                    TitleTextCreator = TitleTextCreator + "🔇";
                }

                if (Paused)
                {
                    TitleTextCreator = TitleTextCreator + "⏸️ ";
                }

                if (!IsRadio)
                {
                    if (LoopPlaying)
                    {
                        TitleTextCreator = TitleTextCreator + "♾️";
                    }

                    if (VideoReload)
                    {
                        TitleTextCreator = TitleTextCreator + "🔃";
                    }
                }


                TitleTextCreator = Program.ArgsCalled ? TitleTextCreator : TitleTextCreator + (VideoVisits != 0 ? " 👀" + VideoVisits : null);

                this.Text = TitleTextCreator;
            }
            catch
            {

            }
        }


        //Que pereza pensar y programar 
        private static double[] AverageDownloadSpeed = { 0 };
        private static Stopwatch Sw = new Stopwatch();
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
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

        public void client_DownloadProgressChanged(double NewProgress)
        {
            UpdateProgress(NewProgress * 10000);
        }

        public void UpdateProgress(double Porcentaje)
        {
            string Percentaje = ((int)Porcentaje).ToString();

            if (int.Parse(Percentaje) <= 9)
            {
                Percentaje = "0" + Percentaje;
            }

            if (int.Parse(Percentaje) <= 99)
            {
                Percentaje = "0" + Percentaje;
            }

            if (Percentaje != "0")
            {
                Percentaje = Percentaje.Insert(Percentaje.Length - 2, ",");
            }

            this.Text = string.Format(DownloadName, Percentaje + "%");
        }





        public static string CompressString(string Text)
        {
            return CompressString(Text, Encoding.UTF8);
        }
        public static string DecompressString(string Text)
        {
            return DecompressString(Text, Encoding.UTF8);
        }
        public static string CompressString(string Text, Encoding Encoder)
        {
            return Convert.ToBase64String(Compress(Encoder.GetBytes(Text)));
        }
        public static string DecompressString(string Text, Encoding Encoder)
        {
            return Encoder.GetString(Decompress(Convert.FromBase64String(Text)));
        }
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

                Object accentColorObj = dwmKey.GetValue("AccentColor");

                if (accentColorObj is Int32 accentColorDword)
                {
                    var AcentColor = ParseDWordColor(accentColorDword);

                    return Color.FromArgb(AcentColor.a, AcentColor.r, AcentColor.g, AcentColor.b);
                }
                else
                {
                    return DefaultColor;
                }
            }

        }
        private static (Byte r, Byte g, Byte b, Byte a) ParseDWordColor(Int32 color)
        {
            Byte
                a = (byte)((color >> 24) & 0xFF),
                b = (byte)((color >> 16) & 0xFF),
                g = (byte)((color >> 8) & 0xFF),
                r = (byte)((color >> 0) & 0xFF);

            return (r, g, b, a);
        }




        private static bool RandomBool(int Percentage) => new Random().Next(100) <= Percentage;
        private static bool CompresedBool(string Text) { return Text == "1"; }
        private static string CompresedBool(bool Boolean) { return Boolean ? "1" : "0"; }


        private static bool PlayingBeep = false;
        private static async void PlayBeep()
        {
            if (PlayingBeep)
            {
                return;
            }

            PlayingBeep = true;

            SystemSounds.Beep.Play();

            await Task.Delay(100);

            PlayingBeep = false;
        }
    }
}