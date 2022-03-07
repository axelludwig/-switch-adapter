using internet_macro.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace internet_macro
{
    class Program
    {
        private static NotifyIcon trayIcon;
        public static string lastKeystroke;

        public static string[] shortcutADSL = { "LShiftKey", "Oemtilde" };
        public static string[] shortcut4G = { "LShiftKey", "Oem5" };

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("ADSL", ADSLEvent),
                new MenuItem("4G", GGGGEvent),
                new MenuItem("Exit", Exit)
            }),
                Visible = true
            };

            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Program intcptKys = new Program();

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                string currentKeystroke = ((Keys)vkCode).ToString();

                if (lastKeystroke != null)
                {
                    if (lastKeystroke == shortcutADSL[0] && currentKeystroke == shortcutADSL[1])
                        ADSL();
                    else if (lastKeystroke == shortcut4G[0] && currentKeystroke == shortcut4G[1])
                        GGGG();
                }; lastKeystroke = currentKeystroke;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        static void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        static void ADSLEvent(object sender, EventArgs e)
        {
            ADSL();
        }

        static void GGGGEvent(object sender, EventArgs e)
        {
            GGGG();
        }

        static void ADSL()
        {
            ExecuteCommand("start /MIN ADSL.bat");
            trayIcon.Text = "ADSL";
        }

        static void GGGG()
        {
            ExecuteCommand("start /MIN 4G.bat");
            trayIcon.Text = "4G";
        }

        static void ExecuteCommand(string command)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;

            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            var res = ("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            res += ("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            res += "\r\n" + ("ExitCode: " + exitCode.ToString(), "ExecuteCommand");

            WriteToFile(res);
            process.Close();
        }

        public static void WriteToFile(string content)
        {
            File.WriteAllText("log.txt", content);
        }
    }
}