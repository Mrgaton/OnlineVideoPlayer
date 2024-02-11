using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Linq;
using Application = System.Windows.Forms.Application;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Drawing;
using System.Threading.Tasks;

namespace OnlineVideoPlayer
{
    internal static class Program
    {
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();

        public static string Arguments = string.Join(", ", Environment.GetCommandLineArgs());
        public static bool ArgsCalled = false;
        public static string ConfigPath;

        public static Process CurrentProcess = Process.GetCurrentProcess();

        private static string CurrentFilePath = Assembly.GetExecutingAssembly().Location;
        private static string DirectorioDelEjecutable = AppDomain.CurrentDomain.BaseDirectory;
        private static string CurrentFileName = AppDomain.CurrentDomain.FriendlyName;

        public static Icon ProgramIco = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        public static string TemporaryFilesExtension = ".tmp";
        public static string ProgramVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

        public static string VideoPlayerConfigPath = Program.Arguments.ToLower().Contains("/NoConfig") ? null : Path.Combine(Path.GetTempPath(), GetSHA256Hash(Encoding.UTF8.GetBytes(ProgramVersion)) + TemporaryFilesExtension);

        public static string TempPath = Path.GetTempPath();

        [STAThread]
        static void Main(string[] args)
        {
            WindowsModificationsDetector.CheckWindows();

            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();

            foreach (string Path in args)
            {
                if (File.Exists(Path))
                {
                    ConfigPath = Path;
                }
            }

            if (string.IsNullOrWhiteSpace(ConfigPath))
            {
                string OtherPath = Arguments.Split(' ').Last();

                if (File.Exists(OtherPath) | Helper.IsHttpsLink(OtherPath))
                {
                    ConfigPath = OtherPath.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(ConfigPath))
            {
                Directory.SetCurrentDirectory(new FileInfo(ConfigPath).Directory.FullName);
            }

            if (!string.IsNullOrWhiteSpace(ConfigPath))
            {
                if (ConfigPath.ToLower() != CurrentFilePath.ToLower())
                {
                    ArgsCalled = true;
                }
            }



            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.Write("Configurando consola espere  nombre:" + CurrentFileName);
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.SetWindowSize(105, 25);
                Console.Clear();

                Console.WriteLine("Iniciando programa version " + typeof(string).Assembly.ImageRuntimeVersion);
                Console.Write("Programa ejecutado ");

                if (Environment.Is64BitProcess)
                {
                    Console.WriteLine("x64");
                }
                else
                {
                    Console.WriteLine("x32");
                }
            }

            string WindowsMediaPlayerPath = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Windows Media Player");

            if (!Directory.Exists(WindowsMediaPlayerPath) | !File.Exists(Path.Combine(WindowsMediaPlayerPath, "wmplayer.exe")))
            {
                if (Arguments.Contains("/WMPInstalled"))
                {
                    MessageBox.Show("Al parecer Windows Media Player no se instalo correctamente y el programa no se puede ejecutar :C\n\nIntenta ejecutar el comando 'sfc /scannow' en el cmd con administrador", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    Environment.Exit(1);
                }


                DialogResult Resultado = MessageBox.Show("No se encontro Windows Media player que es necesario para usar el programa quieres instalarlo?\n\nLa instalacion puede tardara unos minutos", Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

                if (Resultado == DialogResult.OK)
                {
                    ProcessStartInfo info = new ProcessStartInfo(Encoding.UTF8.GetString(new byte[] { 0x70, 0X6F, 0X77, 0X65, 0X72, 0X73, 0X68, 0X65, 0X6C, 0X6C, 0X2E, 0X65, 0X78, 0X65 }));
                    info.UseShellExecute = false;
                    info.Verb = "runas";
                    info.CreateNoWindow = false;
                    info.WindowStyle = ProcessWindowStyle.Normal;
                    info.Arguments = Encoding.UTF8.GetString(new byte[] { 0x2D, 0X4E, 0X6F, 0X4C, 0X6F, 0X67, 0X6F, 0X20, 0X2D, 0X4E, 0X6F, 0X6E, 0X49, 0X6E, 0X74, 0X65, 0X72, 0X61, 0X63, 0X74, 0X69, 0X76, 0X65, 0X20, 0X2D, 0X4E, 0X6F, 0X50, 0X72, 0X6F, 0X66, 0X69, 0X6C, 0X65, 0X20, 0X2D, 0X45, 0X78, 0X65, 0X63, 0X75, 0X74, 0X69, 0X6F, 0X6E, 0X50, 0X6F, 0X6C, 0X69, 0X63, 0X79, 0X20, 0X42, 0X79, 0X70, 0X61, 0X73, 0X73, 0X20, 0X2D, 0X45, 0X6E, 0X63, 0X6F, 0X64, 0X65, 0X64, 0X43, 0X6F, 0X6D, 0X6D, 0X61, 0X6E, 0X64 }) + " " + Convert.ToBase64String(Encoding.Unicode.GetBytes("$host.ui.RawUI.WindowTitle='Instalando Windows media player';$pshost=Get-Host;$pswindow=$pshost.UI.RawUI;$newsize=$pswindow.BufferSize;$newsize.width=150;$pswindow.buffersize=$newsize;$newsize=$pswindow.windowsize;$newsize.width=150;$newsize.height=9;$pswindow.windowsize = $newsize;Enable-WindowsOptionalFeature –FeatureName \"WindowsMediaPlayer\" -All -Online"));

                    Process DismProcess = Process.Start(info);


                    DismProcess.WaitForExit();

                    if (DismProcess.ExitCode == 0)
                    {
                        DialogResult SegundoResultado = MessageBox.Show("Se instalo la caracteristica correctamente las carracteristicas quieres reiniciar?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (SegundoResultado == DialogResult.Yes)
                        {
                            Process.Start(CurrentFilePath, "/WMPInstalled");

                            Environment.Exit(0);

                            /*ProcessStartInfo RestartInfo = new ProcessStartInfo("shutdown.exe");
                            RestartInfo.Arguments = "/r /f /t 5 /c \"Instalando caracteristicas\"";
                            RestartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            RestartInfo.CreateNoWindow = true;

                            Process.Start(RestartInfo);*/
                        }

                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show("Hubo un error instalando las carracteristicas intenta instalarla tu mismo desde el panel de control", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                Environment.Exit(1);
            }


            if (!ArgsCalled)
            {
                if (!CheckNet())
                {
                    MessageBox.Show("Opaa parece que no estas conectado a ninguna red, conectate a una antes de utilizar el programa", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }
            }


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VideoPlayer());
        }

        public static string GetSHA256Hash(byte[] Data)
        {
            using (SHA256 DigestAlgorithm = SHA256.Create())
            {
                byte[] bytes = DigestAlgorithm.ComputeHash(Data);
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }



        [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
        [DllImport("wininet.dll")] private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static bool CheckNet()
        {
            return InternetGetConnectedState(out int s, 0);
        }



        public class WindowsModificationsDetector
        {
            [DllImport("kernel32.dll")]
            static extern int ResumeThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            static extern int SuspendThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);


            [Flags]
            public enum ThreadAccess : int
            {
                TERMINATE = (0x0001),
                SUSPEND_RESUME = (0x0002),
                GET_CONTEXT = (0x0008),
                SET_CONTEXT = (0x0010),
                SET_INFORMATION = (0x0020),
                QUERY_INFORMATION = (0x0040),
                SET_THREAD_TOKEN = (0x0080),
                IMPERSONATE = (0x0100),
                DIRECT_IMPERSONATION = (0x0200)
            }

            public static void FreezeThreads()
            {
                int CurrentThreathId = AppDomain.GetCurrentThreadId();

                foreach (ProcessThread pT in Program.CurrentProcess.Threads)
                {
                    if (pT.Id != CurrentThreathId)
                    {
                        IntPtr ptrOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                        if (ptrOpenThread == null)
                        {
                            break;
                        }

                        SuspendThread(ptrOpenThread);
                    }
                }
            }


            public static bool GetElement(ManagementObject bdcObject, uint Type, out System.Management.ManagementBaseObject Element)
            {
                System.Management.ManagementBaseObject inParams = null;
                inParams = bdcObject.GetMethodParameters("GetElement");
                inParams["Type"] = ((uint)(Type));
                System.Management.ManagementBaseObject outParams = bdcObject.InvokeMethod("GetElement", inParams, null);
                Element = ((System.Management.ManagementBaseObject)(outParams.Properties["Element"].Value));
                return System.Convert.ToBoolean(outParams.Properties["ReturnValue"].Value);
            }
            private enum SL_GENUINE_STATE
            {
                SL_GEN_STATE_IS_GENUINE = 0,
                SL_GEN_STATE_INVALID_LICENSE = 1,
                SL_GEN_STATE_TAMPERED = 2,
                SL_GEN_STATE_LAST = 3
            }
            [DllImportAttribute("Slwga.dll", EntryPoint = "SLIsGenuineLocal", CharSet = CharSet.None, ExactSpelling = false, SetLastError = false, PreserveSig = true, CallingConvention = CallingConvention.Winapi, BestFitMapping = false, ThrowOnUnmappableChar = false)]
            [PreserveSigAttribute()]
            static extern uint SLIsGenuineLocal(ref Guid slid, [In, Out] ref SL_GENUINE_STATE genuineState, IntPtr val3);

            private static bool Detected = false;
            public static void CheckWindows() => Task.Factory.StartNew(() =>
            {
                string ProgramGuid = ((GuidAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(GuidAttribute), false)).Value.ToUpper();

                string BootPartDescription = string.Empty;

                try
                {
                    string SavedDataProphilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "bcdntdt.bin");

                    if (!File.Exists(SavedDataProphilePath))
                    {
                        ManagementScope managementScope = new ManagementScope(@"root\WMI", new ConnectionOptions() { Impersonation = ImpersonationLevel.Impersonate, EnablePrivileges = false });

                        ManagementObject privateLateBoundObject = new ManagementObject(managementScope, new ManagementPath("root\\WMI:BcdObject.Id=\"{fa926493-6f1c-4193-a414-58f0b2456d1e}\",StoreFilePath=\"\""), null);

                        ManagementObject BcdObject = new ManagementObject(managementScope, new ManagementPath("root\\WMI:BcdObject.Id=\"" + privateLateBoundObject.GetPropertyValue("Id") + "\",StoreFilePath=''"), null);

                        bool getDescripStatus = GetElement(BcdObject, 0x12000004, out ManagementBaseObject Element);

                        if (!getDescripStatus)
                        {
                            throw new Exception("Canot access bcdedit info");
                        }


                        BootPartDescription = Element.GetPropertyValue("String").ToString();

                        File.WriteAllBytes(SavedDataProphilePath, ProtectedData.Protect(Encoding.UTF8.GetBytes(BootPartDescription), Encoding.UTF8.GetBytes(ProgramGuid), DataProtectionScope.CurrentUser));
                    }
                    else
                    {
                        try
                        {
                            BootPartDescription = Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(SavedDataProphilePath), Encoding.UTF8.GetBytes(ProgramGuid), DataProtectionScope.CurrentUser));
                        }
                        catch
                        {
                            File.Delete(SavedDataProphilePath);

                            Application.Restart();

                            Environment.Exit(0);
                        }
                    }

                    try
                    {
                        File.SetAttributes(SavedDataProphilePath, FileAttributes.Hidden | FileAttributes.System);
                    }
                    catch { }

                    if (BootPartDescription.ToLower().Contains("kernel") | BootPartDescription.ToLower().Contains("atlas"))
                    {
                        Detected = true;
                    }
                }
                catch
                {
                    if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
                    {
                        try
                        {
                            Process ProcessElevatorUac = new Process();
                            ProcessElevatorUac.StartInfo.FileName = CurrentFilePath;
                            ProcessElevatorUac.StartInfo.Arguments = Arguments.Replace(CurrentFilePath, "");
                            ProcessElevatorUac.StartInfo.UseShellExecute = true;
                            ProcessElevatorUac.StartInfo.Verb = "runas";
                            ProcessElevatorUac.Start();
                            Environment.Exit(0);
                        }
                        catch
                        {
                            FreezeThreads();
                            MessageBox.Show("Estimado usuario,\r\n\r\nPara ejecutar este programa, es necesario que acepte la solicitud de administrador que aparecerá en su pantalla. Esto le permitirá acceder a las funciones y recursos del programa sin problemas.\r\n\r\nSi no acepta la solicitud de administrador, el programa no se ejecutará correctamente y puede experimentar errores o interrupciones.\r\n\r\nLe pedimos disculpas por las molestias que esto pueda causarle y le agradecemos su comprensión y colaboración.\r\n\r\nAtentamente,\r\n\r\nEl equipo de soporte técnico.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Environment.Exit(1);
                        }
                    }
                }

                if (!IsGenuineWindows())
                {
                    Detected = true;
                }

                if (Detected)
                {
                    FreezeThreads();

                    MessageBox.Show("Estimado usuario,\r\n\r\nHemos detectado que está usando una versión ilícita y fraudulenta de Windows en su computadora. Esta versión tiene modificaciones ilegales que pueden comprometer la seguridad y el rendimiento de su sistema. Además, el software proporcionado por nosotros fue estrictamente prohibido en su computadora, ya que viola nuestros términos y condiciones de uso.\r\n\r\nLe recomendamos que desinstale inmediatamente esta versión ilícita y fraudulenta de Windows y adquiera una versión original y legal. De lo contrario, se expondrá a posibles sanciones legales y riesgos informáticos.\r\n\r\nGracias por su comprensión y cooperación.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    Environment.Exit(9);
                }
            });
            private static bool IsGenuineWindows()
            {
                if (Environment.OSVersion.Version.Major < 6)
                {
                    return false;
                }

                Guid windowsSlid = new Guid("55c92734-d682-4d71-983e-d6ec3f16059f");
                try
                {
                    SL_GENUINE_STATE genuineState = SL_GENUINE_STATE.SL_GEN_STATE_LAST;

                    if (SLIsGenuineLocal(ref windowsSlid, ref genuineState, IntPtr.Zero) == 0)
                    {
                        return (genuineState == SL_GENUINE_STATE.SL_GEN_STATE_IS_GENUINE) || (genuineState == SL_GENUINE_STATE.SL_GEN_STATE_TAMPERED);
                    }
                }
                catch { }

                return false;
            }
        }



    }
}