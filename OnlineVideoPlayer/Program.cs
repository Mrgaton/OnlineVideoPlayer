using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static OnlineVideoPlayer.Program;
using Application = System.Windows.Forms.Application;

namespace OnlineVideoPlayer
{
    internal static class Program
    {
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();

        private static MD5 hashAlg = MD5.Create();

        public static string arguments = string.Join(", ", Environment.GetCommandLineArgs());

        public static bool ArgsCalled = false;

        public static string ConfigPath;

        public static Process CurrentProcess = Process.GetCurrentProcess();

        private static string CurrentFilePath = Assembly.GetExecutingAssembly().Location;

        private static string CurrentFileName = AppDomain.CurrentDomain.FriendlyName;

        public static Icon ProgramIco = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

        public static string TemporaryFilesExtension = ".tmp";

        public static string ProgramVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

        public static string VideoPlayerConfigPath = Program.arguments.ToLower().Contains("/NoConfig") ? null : Path.Combine(Path.GetTempPath(), GetHash(Encoding.UTF8.GetBytes(ProgramVersion)) + TemporaryFilesExtension);

        public static string tempPath = Path.GetTempPath();

        [STAThread]
        private static void Main(string[] args)
        {
            /*RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Drivers32");

            foreach (string keyName in rk.GetValueNames())
            {
                if (keyName.StartsWith("msacm."))
                {
                    Console.WriteLine("Audio Codec: " + keyName);
                }
                else if (keyName.StartsWith("vidc."))
                {
                    Console.WriteLine("Video Codec: " + keyName);
                }
            }

            Console.ReadLine();*/

            //WindowsModificationsDetector.CheckWindows();

            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();

            foreach (string path in args)
            {
                if (File.Exists(path))
                {
                    ConfigPath = path;
                }
            }

            if (string.IsNullOrWhiteSpace(ConfigPath))
            {
                string otherPath = arguments.Split(' ').Last();

                if (File.Exists(otherPath) || Helper.IsHttpsLink(otherPath)) ConfigPath = otherPath.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ConfigPath))
            {
                Directory.SetCurrentDirectory(new FileInfo(ConfigPath).Directory.FullName);
            }

            if (!string.IsNullOrWhiteSpace(ConfigPath) && ConfigPath.ToLower() != CurrentFilePath.ToLower()) ArgsCalled = true;

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

                Console.WriteLine(Environment.Is64BitProcess ? "x64" : "x32");
            }

            string windowsMediaPlayerPath = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Windows Media Player");

            if (!Directory.Exists(windowsMediaPlayerPath) || !File.Exists(Path.Combine(windowsMediaPlayerPath, "wmplayer.exe")))
            {
                if (arguments.Contains("/WMPInstalled"))
                {
                    MessageBox.Show("Al parecer Windows Media Player no se instalo correctamente y el programa no se puede ejecutar :C\n\nIntenta ejecutar el comando 'sfc /scannow' en el cmd con administrador", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    Environment.Exit(1);
                }

                DialogResult result = MessageBox.Show("No se encontro Windows Media player que es necesario para usar el programa quieres instalarlo?\n\nLa instalacion puede tardara unos minutos", Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

                if (result == DialogResult.OK)
                {
                    ProcessStartInfo info = new ProcessStartInfo(Encoding.UTF8.GetString([0x70, 0X6F, 0X77, 0X65, 0X72, 0X73, 0X68, 0X65, 0X6C, 0X6C, 0X2E, 0X65, 0X78, 0X65]));
                    info.UseShellExecute = false;
                    info.Verb = "runas";
                    info.CreateNoWindow = false;
                    info.WindowStyle = ProcessWindowStyle.Normal;
                    info.Arguments = Encoding.UTF8.GetString("-NoLogo -NonInteractive -NoProfile -ExecutionPolicy Bypass -EncodedCommand"u8.ToArray()) + " " + Convert.ToBase64String(Encoding.Unicode.GetBytes("$host.ui.RawUI.WindowTitle='Instalando Windows media player';$pshost=Get-Host;$pswindow=$pshost.UI.RawUI;$newsize=$pswindow.BufferSize;$newsize.width=150;$pswindow.buffersize=$newsize;$newsize=$pswindow.windowsize;$newsize.width=150;$newsize.height=9;$pswindow.windowsize = $newsize;Enable-WindowsOptionalFeature –FeatureName \"WindowsMediaPlayer\" -All -Online"));

                    Process dismProcess = Process.Start(info);

                    dismProcess.WaitForExit();

                    if (dismProcess.ExitCode == 0)
                    {
                        DialogResult secondResult = MessageBox.Show("Se instalo la caracteristica correctamente las carracteristicas quieres reiniciar?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (secondResult == DialogResult.Yes)
                        {
                            Process.Start(CurrentFilePath, "/WMPInstalled");

                            Environment.Exit(0);

                            /*ProcessStartInfo RestartInfo = new ProcessStartInfo("shutdown.exe");
                            RestartInfo.arguments = "/r /f /t 5 /c \"Instalando caracteristicas\"";
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

            if (!ArgsCalled && !CheckNet())
            {
                MessageBox.Show("Opaa parece que no estas conectado a ninguna red, conectate a una antes de utilizar el programa", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VideoPlayer());
        }

        public static string GetHash(byte[] Data) => ToHex(hashAlg.ComputeHash(Data));

        private static char GetHexValue(int i) => (i < 10) ? ((char)(i + 48)) : ((char)(i - 10 + 65));

        public static string ToHex(byte[] value)
        {
            int l = value.Length * 2;
            char[] array = new char[l];
            int i, di = 0;

            for (i = 0; i < l; i += 2)
            {
                byte b = value[di++];
                array[i] = GetHexValue(b / 16);
                array[i + 1] = GetHexValue(b % 16);
            }

            return new string(array);
        }

        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();

        [DllImport("wininet.dll")] private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);

        public static bool CheckNet() => InternetGetConnectedState(out int s, 0);

        public class WindowsModificationsDetector
        {
            [DllImport("kernel32.dll")]
            private static extern int ResumeThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            private static extern int SuspendThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

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
                int currentThreathId = AppDomain.GetCurrentThreadId();

                foreach (ProcessThread pT in Program.CurrentProcess.Threads)
                {
                    if (pT.Id != currentThreathId)
                    {
                        IntPtr ptrOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                        if (ptrOpenThread == null) break;

                        SuspendThread(ptrOpenThread);
                    }
                }
            }

            public static bool GetElement(ManagementObject bdcObject, uint Type, out ManagementBaseObject element)
            {
                ManagementBaseObject inParams = null;
                inParams = bdcObject.GetMethodParameters("GetElement");
                inParams["Type"] = Type;
                ManagementBaseObject outParams = bdcObject.InvokeMethod("GetElement", inParams, null);
                element = (ManagementBaseObject)(outParams.Properties["element"].Value);
                return Convert.ToBoolean(outParams.Properties["ReturnValue"].Value);
            }

            private enum SL_GENUINE_STATE
            {
                SL_GEN_STATE_IS_GENUINE = 0,
                SL_GEN_STATE_INVALID_LICENSE = 1,
                SL_GEN_STATE_TAMPERED = 2,
                SL_GEN_STATE_LAST = 3
            }

            [DllImportAttribute("Slwga.dll", EntryPoint = "SLIsGenuineLocal", CharSet = CharSet.None, ExactSpelling = false, SetLastError = false, PreserveSig = true, CallingConvention = CallingConvention.Winapi, BestFitMapping = false, ThrowOnUnmappableChar = false)]
            [PreserveSigAttribute()] private static extern uint SLIsGenuineLocal(ref Guid slid, [In, Out] ref SL_GENUINE_STATE genuineState, IntPtr val3);

            private static bool Detected = false;

            public static void CheckWindows() => Task.Factory.StartNew(() =>
            {
                string programGuid = ((GuidAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(GuidAttribute), false)).Value.ToUpper();

                string bootPartDescription = string.Empty;

                try
                {
                    string savedDataProphilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "bcdntdt.bin");

                    if (!File.Exists(savedDataProphilePath))
                    {
                        ManagementScope managementScope = new ManagementScope(@"root\WMI", new ConnectionOptions() { Impersonation = ImpersonationLevel.Impersonate, EnablePrivileges = false });

                        ManagementObject privateLateBoundObject = new ManagementObject(managementScope, new ManagementPath("root\\WMI:bcdObject.Id=\"{fa926493-6f1c-4193-a414-58f0b2456d1e}\",StoreFilePath=\"\""), null);

                        ManagementObject bcdObject = new ManagementObject(managementScope, new ManagementPath("root\\WMI:bcdObject.Id=\"" + privateLateBoundObject.GetPropertyValue("Id") + "\",StoreFilePath=''"), null);

                        bool getDescripStatus = GetElement(bcdObject, 0x12000004, out ManagementBaseObject element);

                        if (!getDescripStatus) throw new Exception("Canot access bcdedit info");

                        bootPartDescription = element.GetPropertyValue("String").ToString();

                        File.WriteAllBytes(savedDataProphilePath, ProtectedData.Protect(Encoding.UTF8.GetBytes(bootPartDescription), Encoding.UTF8.GetBytes(programGuid), DataProtectionScope.CurrentUser));
                    }
                    else
                    {
                        try
                        {
                            bootPartDescription = Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(savedDataProphilePath), Encoding.UTF8.GetBytes(programGuid), DataProtectionScope.CurrentUser));
                        }
                        catch
                        {
                            File.Delete(savedDataProphilePath);

                            Application.Restart();

                            Environment.Exit(0);
                        }
                    }

                    try
                    {
                        File.SetAttributes(savedDataProphilePath, FileAttributes.Hidden | FileAttributes.System);
                    }
                    catch { }

                    if (bootPartDescription.ToLower().Contains("kernel") || bootPartDescription.ToLower().Contains("atlas")) Detected = true;
                }
                catch
                {
                    if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        try
                        {
                            Process processElevatorUac = new Process();
                            processElevatorUac.StartInfo.FileName = CurrentFilePath;
                            processElevatorUac.StartInfo.Arguments = arguments.Replace(CurrentFilePath, "");
                            processElevatorUac.StartInfo.UseShellExecute = true;
                            processElevatorUac.StartInfo.Verb = "runas";
                            processElevatorUac.Start();
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

                if (!IsGenuineWindows()) Detected = true;

                if (Detected)
                {
                    FreezeThreads();

                    MessageBox.Show("Estimado usuario,\r\n\r\nHemos detectado que está usando una versión ilícita y fraudulenta de Windows en su computadora. Esta versión tiene modificaciones ilegales que pueden comprometer la seguridad y el rendimiento de su sistema. Además, el software proporcionado por nosotros fue estrictamente prohibido en su computadora, ya que viola nuestros términos y condiciones de uso.\r\n\r\nLe recomendamos que desinstale inmediatamente esta versión ilícita y fraudulenta de Windows y adquiera una versión original y legal. De lo contrario, se expondrá a posibles sanciones legales y riesgos informáticos.\r\n\r\nGracias por su comprensión y cooperación.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    Environment.Exit(9);
                }
            });

            private static bool IsGenuineWindows()
            {
                if (Environment.OSVersion.Version.Major < 6) return false;

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