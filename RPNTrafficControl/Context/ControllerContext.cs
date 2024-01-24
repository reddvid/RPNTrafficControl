using Microsoft.Win32;
using OBSWebsocketDotNet;
using RPNTrafficControl.Extensions;
using RPNTrafficControl.Renderers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RPNTrafficControl.Context
{
    public class ControllerContext : ApplicationContext
    {

        private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static readonly string StartupValue = "RPN Traffic Control";
        private static readonly string ThemeRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

        private const int VERTICAL_PADDING = 4;

        protected static System.Timers.Timer _timer;
       
        private OBSContext _obs;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _contextMenuStrip;
        private ToolStripMenuItem _settingsStripItem;
        private ToolStripMenuItem _quitStripItem;
        private SettingsPage _settingsPage;
        private System.Windows.Forms.Timer _doubleClickTimer;

        private bool _isFirstClick = true;
        private bool _isDoubleClick = true;
        private int _milliseconds = 0;

        public ControllerContext()
        {
            _settingsPage = new SettingsPage();

            _obs = new OBSContext(_settingsPage);

            _settingsPage.Show();

            ValidateOBSApp();

            InitializeTimer();

            InitializeDoubleClickTimer();

            InitializeSettingsStripItem();

            InitializeQuitStripItem();

            InitializeContextMenuStrip();

            InitializeNotifyTrayIcon();

            EnableRunAtStartup();
        }

        private void ValidateOBSApp()
        {
            Console.WriteLine(Properties.Settings.Default.OBSExeLocation);

            if (!File.Exists(Properties.Settings.Default.OBSExeLocation))
            {
                MessageBox.Show("OBS Studio executable not found on default installation directory.\n\n" +
                    "Please locate the executable.",
                    "Missing obs64.exe",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);

                _settingsPage?.Show();
            }
        }

        private static double Interval => ((60 - DateTime.Now.Second) * 1000 - DateTime.Now.Millisecond);

        private static bool IsOBSRunning()
        {
            Process[] obs = Process.GetProcessesByName("obs64");
            return obs.Length > 0;
        }

        private void InitializeTimer()
        {
            _timer = new System.Timers.Timer();
            _timer.AutoReset = false;
            _timer.Elapsed += new ElapsedEventHandler(ClockTimer_Elapsed);
            _timer.Interval = Interval;
            _timer.Enabled = true;
            _timer.Start();
        }

        private async void ClockTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(DateTime.Now.ToString("hh:mm tt"));
            Console.WriteLine(Properties.Settings.Default.StartTime);

            var timeNow = DateTime.Now.ToString("hh:mm tt");

            if (!IsOBSRunning())
            {
                OpenOBSStudio();

                await Task.Delay(5_000); // Wait for OBS to run/initialize
            }

            await Task.Delay(1_500);
            _obs.EnsureWebsocketConnection();

            try
            {
                if (timeNow == Properties.Settings.Default.StartTime)
                {

                }
                else if (timeNow == Properties.Settings.Default.StopTime)
                {

                }
                else if (IsCurrentTimeWithinOperation())
                {


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            _timer.Interval = Interval;
            _timer.Start();
        }

        private void OpenOBSStudio()
        {
            ProcessStartInfo psi = new();
            psi.WorkingDirectory = Properties.Settings.Default.OBSExeLocation.Replace("obs64.exe", string.Empty);
            psi.FileName = @"obs64.exe";
            psi.UseShellExecute = true;
            psi.Arguments = @"--disable-shutdown-check --startrecording --websocket_port 4455 --websocket_password rpn0629 --websocket_debug true";
            Process.Start(psi);
        }

        private bool IsCurrentTimeWithinOperation()
        {
            var startTime = Properties.Settings.Default.StartTime;
            var stopTime = Properties.Settings.Default.StopTime;

            DateTime start = DateTime.ParseExact(startTime, "hh:mm tt", CultureInfo.InvariantCulture);
            DateTime stop = DateTime.ParseExact(stopTime, "hh:mm tt", CultureInfo.InvariantCulture);
            DateTime now = DateTime.Now;

            if ((now > start) && (now < stop))
            {
                return true;
            }

            return false;
        }

        private void InitializeDoubleClickTimer()
        {
            _doubleClickTimer = new Timer();
            _doubleClickTimer.Interval = 100;
            _doubleClickTimer.Tick += new EventHandler(DoubleClickTimer_Tick);
        }

        private void DoubleClickTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _milliseconds += 50;

                if (_milliseconds >= SystemInformation.DoubleClickTime)
                {
                    _doubleClickTimer.Stop();

                    if (_isDoubleClick) _settingsPage.Show();

                    _isFirstClick = true;
                    _isDoubleClick = false;
                    _milliseconds = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void InitializeSettingsStripItem()
        {
            _settingsStripItem = new ToolStripMenuItem();
            _settingsStripItem.Text = "Settings";
            _settingsStripItem.Click += SettingsStripItem_Click;
            _settingsStripItem.AutoSize = false;
            _settingsStripItem.Size = new Size(120, 30);
            _settingsStripItem.Margin = new Padding(0, 4, 0, 0);
        }

        private void SettingsStripItem_Click(object sender, EventArgs e)
        {
            try
            {
                _settingsPage?.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void InitializeQuitStripItem()
        {
            _quitStripItem = new ToolStripMenuItem();
            _quitStripItem.Text = "Quit";
            _quitStripItem.Click += QuitStripItem_Click;
            _quitStripItem.AutoSize = false;
            _quitStripItem.Size = new Size(120, 30);
            _quitStripItem.Margin = new Padding(0, 0, 0, 4);
        }

        private void QuitStripItem_Click(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Environment.Exit(0);
            Application.Exit();
        }

        private void EnableRunAtStartup()
        {
            var registryKey = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            registryKey.SetValue(StartupValue, Application.ExecutablePath);
            registryKey.Close();
        }

        private void InitializeNotifyTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.ContextMenuStrip = _contextMenuStrip;
            _trayIcon.Icon = Properties.Resources.favicon;
            _trayIcon.Visible = true;
            _trayIcon.Text = "RPN Traffic Control by Red David";

            _trayIcon.BalloonTipText = "Access the app's settings by right-clicking on the system tray icon";
            _trayIcon.BalloonTipTitle = "RPN Traffic Control is active";
            _trayIcon.ShowBalloonTip(timeout: 2_000); // 2 seconds

            _trayIcon.MouseDown += TrayIcon_MouseDown;
            _trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        private async void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                try
                {
                    await CustomizeContextMenuBackgroundAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void TrayIcon_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_isFirstClick)
                {
                    _isFirstClick = false;
                    _doubleClickTimer.Start();
                }
                else
                {
                    if (_milliseconds < SystemInformation.DoubleClickTime)
                    {
                        _isDoubleClick = true;
                    }
                }
            }
        }

        private async void InitializeContextMenuStrip()
        {
            _contextMenuStrip = new ContextMenuStrip();
            _contextMenuStrip.DropShadowEnabled = true;
            _contextMenuStrip.Size = new System.Drawing.Size(310, 170);

            _contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                _settingsStripItem,
                _quitStripItem,
            });

            await CustomizeContextMenuBackgroundAsync();
        }

        private async Task CustomizeContextMenuBackgroundAsync()
        {
            _contextMenuStrip.Items[0].Font = new Font(this._contextMenuStrip.Items[0].Font, FontStyle.Bold);
            bool isAppLightMode = await Task.Run(() => ReadAppThemeRegistry());

            if (isAppLightMode)
            {
                _contextMenuStrip.Renderer = new ContextMenuRenderer
                {
                    VerticalPadding = VERTICAL_PADDING,
                    HighlightColor = Color.White,
                    ImageColor = Color.FromArgb(255, 238, 238, 238)
                };
                _contextMenuStrip.BackColor = Color.White.Lighten();
                _contextMenuStrip.ForeColor = Color.Black;
            }
            else
            {
                _contextMenuStrip.Renderer = new ContextMenuRenderer
                {
                    VerticalPadding = VERTICAL_PADDING,
                    HighlightColor = Color.Black,
                    ImageColor = Color.FromArgb(255, 43, 43, 43)
                };
                _contextMenuStrip.BackColor = Color.Black.Lighten();
                _contextMenuStrip.ForeColor = Color.White;
            }

            _contextMenuStrip.MinimumSize = new Size(120, 30);
            _contextMenuStrip.AutoSize = false;
            _contextMenuStrip.ShowImageMargin = false;
            _contextMenuStrip.ShowCheckMargin = false;
        }

        private bool ReadAppThemeRegistry()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ThemeRegistryKey))
            {
                if (key is null) return true;

                var k = key.GetValue("AppsUseLightTheme");

                if (k is null) return true;

                if (k.ToString().Equals("1"))
                    return true;
                else
                    return false;
            }
        }
    }
}
