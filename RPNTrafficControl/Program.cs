using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPNTrafficControl
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew = true;
            using (Mutex mutex = new Mutex(true, "TrafficControl", out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    Application.Run(new MyCustomApplicationContext());
                }
                else
                {
                    MessageBox.Show("RPN Traffic Control is already running and silently lives on the taskbar notification area.", "RPN Traffic Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public class MyCustomApplicationContext : ApplicationContext
        {
            static System.Timers.Timer t;

            private void InitTimer()
            {
                t = new System.Timers.Timer();
                t.AutoReset = false;
                t.Elapsed += new System.Timers.ElapsedEventHandler(t_Elapsed);
                t.Interval = GetInterval();
                t.Enabled = true;
                t.Start();
            }

            static double GetInterval()
            {
                DateTime now = DateTime.Now;
                return ((60 - now.Second) * 1000 - now.Millisecond);
            }

            static void t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                Debug.WriteLine(DateTime.Now.ToString("hh:mm tt"));
                Debug.WriteLine(Properties.Settings.Default.startTime);

                if (DateTime.Now.ToString("hh:mm tt") == Properties.Settings.Default.startTime) // Start
                {
                    try
                    {
                        Process[] obs = Process.GetProcessesByName("obs64");
                        if (obs.Length != 0)
                        {
                            obs[0].Kill();
                        }

                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.WorkingDirectory = Properties.Settings.Default.obsExeLocation.Replace("obs64.exe", string.Empty);
                        psi.FileName = @"obs64.exe";
                        psi.UseShellExecute = true;
                        psi.Arguments = @"--startrecording";
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error", ex.Message, MessageBoxButtons.OK);
                    }
                }

                if (DateTime.Now.ToString("hh:mm tt") == Properties.Settings.Default.stopTime)
                {
                    try
                    {
                        Process[] obs = Process.GetProcessesByName("obs64");
                        if (obs.Length != 0)
                        {
                            obs[0].Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error", ex.Message, MessageBoxButtons.OK);
                    }
                }

                t.Interval = GetInterval();
                t.Start();
            }

            private NotifyIcon trayIcon;
            private ContextMenuStrip contextMenuStrip;
            private ToolStripMenuItem toolStripSettings = new ToolStripMenuItem();
            private ToolStripMenuItem toolStripQuit = new ToolStripMenuItem();

            private Settings s = new Settings();

            private System.Windows.Forms.Timer doubleClickTimer = new System.Windows.Forms.Timer();
            private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            private static readonly string StartupValue = "RPN Traffic Control";

            public MyCustomApplicationContext()
            {
#if DEBUG
                s.Show();
#endif
                InitTimer();

                doubleClickTimer.Interval = 100;
                doubleClickTimer.Tick += new EventHandler(doubleClickTimer_Tick);

                toolStripSettings.Text = "Settings";
                toolStripSettings.Click += ToolStripOpen_Click;
                toolStripSettings.AutoSize = false;
                toolStripSettings.Size = new Size(120, 30);
                toolStripSettings.Margin = new Padding(0, 4, 0, 0);

                toolStripQuit.Text = "Quit";
                toolStripQuit.Click += ToolStripExit_Click;
                toolStripQuit.AutoSize = false;
                toolStripQuit.Size = new Size(120, 30);
                toolStripQuit.Margin = new Padding(0, 0, 0, 4);

                contextMenuStrip = new ContextMenuStrip()
                {
                    DropShadowEnabled = true,
                    ShowCheckMargin = false,
                    ShowImageMargin = false,
                    Size = new System.Drawing.Size(310, 170)
                };

                contextMenuStrip.Items.AddRange(new ToolStripItem[]
                {
                    toolStripSettings,
                    toolStripQuit
                });

                try
                {
                    CustomizeContextMenuBackground();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Something went wrong, try again.",
                                            "RPN Traffic Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                trayIcon = new NotifyIcon()
                {
                    ContextMenuStrip = contextMenuStrip,
                    Icon = Properties.Resources.favicon,
                    Visible = true,
                    Text = "RPN Traffic Control"
                };

                trayIcon.BalloonTipText = "Access the app's settings by right-clicking on the system tray icon";
                trayIcon.BalloonTipTitle = "RPN Traffic Control is active";
                trayIcon.ShowBalloonTip(2000);

                trayIcon.MouseDown += TrayIcon_MouseDown;
                trayIcon.MouseClick += TrayIcon_MouseClick;

                EnableRunAtStartup();

                try
                {
                    RunObs();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Running OBS failed: " + ex.Message);
                }
            }

            private void EnableRunAtStartup()
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                key.SetValue(StartupValue, Application.ExecutablePath.ToString());
                key.Close();
            }

            private void RunObs()
            {
                if (IsItWithinTheTime())
                {
                    try
                    {
                        Process[] obs = Process.GetProcessesByName("obs64");
                        if (obs.Length != 0)
                        {
                            obs[0].Kill();
                        }

                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.WorkingDirectory = Properties.Settings.Default.obsExeLocation.Replace("obs64.exe", string.Empty);
                        psi.FileName = @"obs64.exe";
                        psi.UseShellExecute = true;
                        psi.Arguments = @"--startrecording";
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error", ex.Message, MessageBoxButtons.OK);
                    }
                }
            }

            private bool IsItWithinTheTime()
            {
                // Compare properties with saved time properties
                bool timeIsInRange = false;
                var startTime = Properties.Settings.Default.startTime;
                var endTime = Properties.Settings.Default.stopTime;

                DateTime start = DateTime.ParseExact(startTime, "hh:mm tt", CultureInfo.InvariantCulture);
                DateTime end = DateTime.ParseExact(endTime, "hh:mm tt", CultureInfo.InvariantCulture);
                DateTime now = DateTime.Now;

                if ((now > start) && (now < end))
                {
                    timeIsInRange = true;
                }
                return timeIsInRange;
            }

            private async void CustomizeContextMenuBackground()
            {
                var verticalPadding = 4;
                contextMenuStrip.Items[0].Font = new Font(this.contextMenuStrip.Items[0].Font, FontStyle.Bold);
                bool appsUseLight = await Task.Run(() => ReadRegistry());

                if (appsUseLight)
                {
                    contextMenuStrip.Renderer = new MyCustomRenderer { VerticalPadding = verticalPadding, HighlightColor = Color.White, ImageColor = Color.FromArgb(255, 238, 238, 238) };
                    contextMenuStrip.BackColor = Lighten(Color.White);
                    contextMenuStrip.ForeColor = Color.Black;
                }
                else
                {
                    contextMenuStrip.Renderer = new MyCustomRenderer { VerticalPadding = verticalPadding, HighlightColor = Color.Black, ImageColor = Color.FromArgb(255, 43, 43, 43) };
                    contextMenuStrip.BackColor = Lighten(Color.Black);
                    contextMenuStrip.ForeColor = Color.White;
                }

                contextMenuStrip.MinimumSize = new Size(120, 30);
                contextMenuStrip.AutoSize = false;
                contextMenuStrip.ShowImageMargin = false;
                contextMenuStrip.ShowCheckMargin = false;
            }

            private bool ReadRegistry()
            {
                bool isUsingLightTheme;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                {
                    if (key != null)
                    {
                        var k = key.GetValue("AppsUseLightTheme");
                        if (k != null)
                        {
                            if (k.ToString() == "1")
                                isUsingLightTheme = true;
                            else
                                isUsingLightTheme = false;
                        }
                        else
                        {
                            isUsingLightTheme = true;
                        }
                    }
                    else
                        isUsingLightTheme = true;
                }

                return isUsingLightTheme;
            }

            private Color Lighten(Color color)
            {
                int r;
                int g;
                int b;

                if (color.R == 0 && color.G == 0 && color.B == 0)
                {
                    r = color.R + 43;
                    g = color.G + 43;
                    b = color.B + 43;
                }
                else
                {
                    r = color.R - 17;
                    g = color.G - 17;
                    b = color.B - 17;
                }

                return Color.FromArgb(r, g, b);
            }

            private bool isFirstClick = true;
            private bool isDoubleClick = false;
            private int milliseconds = 0;

            private void TrayIcon_MouseDown(object sender, MouseEventArgs e)
            {
                // This is the first mouse click.
                if (e.Button == MouseButtons.Left)
                {
                    if (isFirstClick)
                    {
                        isFirstClick = false;

                        // Start the double click timer.
                        doubleClickTimer.Start();
                    }

                    // This is the second mouse click.
                    else
                    {
                        // Verify that the mouse click is within the double click
                        // rectangle and is within the system-defined double 
                        // click period.
                        if (milliseconds < SystemInformation.DoubleClickTime)
                        {
                            isDoubleClick = true;
                        }
                    }
                }
            }

            private void doubleClickTimer_Tick(object sender, EventArgs e)
            {
                try
                {
                    milliseconds += 50;

                    // The timer has reached the double click time limit.
                    if (milliseconds >= SystemInformation.DoubleClickTime)
                    {
                        doubleClickTimer.Stop();

                        if (isDoubleClick)
                        {
                            // Perform Double Click
                            s.Show();
                        }

                        // Allow the MouseDown event handler to process clicks again.
                        isFirstClick = true;
                        isDoubleClick = false;
                        milliseconds = 0;
                    }


                }
                catch (Exception) { }
            }

            private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right)
                {
                    try
                    {
                        CustomizeContextMenuBackground();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Something went wrong, try again.",
                                                "RPN Traffic Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }

            private void ToolStripExit_Click(object sender, EventArgs e)
            {
                // Hide tray icon, otherwise it will remain shown until user mouses over it
                trayIcon.Visible = false;
                Environment.Exit(0);
                Application.Exit();
            }

            private void ToolStripOpen_Click(object sender, EventArgs e)
            {
                try
                {
                    s.Show();
                }
                catch (Exception)
                {
                }
            }

            void Exit(object sender, EventArgs e)
            {
                // Hide tray icon, otherwise it will remain shown until user mouses over it
                trayIcon.Visible = false;

                Application.Exit();
            }
        }

        public class MyColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin
            {
                get { return Color.FromArgb(255, 43, 43, 43); }
            }
            public override Color ToolStripGradientEnd
            {
                get { return Color.FromArgb(255, 43, 43, 43); }
            }
            public override Color MenuItemBorder
            {
                get { return Color.FromArgb(255, 43, 43, 43); }
            }
            public override Color MenuItemSelected
            {
                get { return Color.WhiteSmoke; }
            }
            public override Color ToolStripDropDownBackground
            {
                get { return Color.FromArgb(255, 43, 43, 43); }
            }
            public override Color ImageMarginGradientBegin
            {
                get { return Color.FromArgb(255, 43, 43, 43); }
            }
            public override Color ImageMarginGradientMiddle
            {
                get { return Color.FromArgb(255, 43, 43, 43); }
            }
            public override Color ImageMarginGradientEnd
            {
                get { return Color.FromArgb(255, 43, 43, 43); }
            }
        }

        private class MyCustomRenderer : ToolStripProfessionalRenderer
        {
            public MyCustomRenderer() : base(new MyColorTable())
            {
            }

            public Color ImageColor { get; set; }
            public Color HighlightColor { get; set; }
            public int VerticalPadding { get; set; }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (null == e)
                { return; }
                e.TextFormat &= ~TextFormatFlags.HidePrefix;
                e.TextFormat |= TextFormatFlags.VerticalCenter;
                var rect = e.TextRectangle;
                rect.Offset(24, VerticalPadding);
                e.TextRectangle = rect;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs myMenu)
            {
                if (!myMenu.Item.Selected)
                    base.OnRenderMenuItemBackground(myMenu);
                else
                {
                    if (myMenu.Item.Enabled)
                    {
                        Rectangle menuRectangle = new Rectangle(Point.Empty, myMenu.Item.Size);
                        //Fill Color
                        myMenu.Graphics.FillRectangle(new SolidBrush(RenderHighlight(HighlightColor)), menuRectangle);
                        // Border Color
                        // myMenu.Graphics.DrawRectangle(Pens.Lime, 1, 0, menuRectangle.Width - 2, menuRectangle.Height - 1);
                    }
                    else
                    {
                        Rectangle menuRectangle = new Rectangle(Point.Empty, myMenu.Item.Size);
                        //Fill Color
                        myMenu.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(20, 128, 128, 128)), menuRectangle);
                    }

                }
            }

            private Color RenderHighlight(Color color)
            {
                int r;
                int g;
                int b;

                if (color.R == 0 && color.G == 0 && color.B == 0)
                {
                    r = color.R + 65;
                    g = color.G + 65;
                    b = color.B + 65;
                }
                else
                {
                    r = color.R;
                    g = color.G;
                    b = color.B;
                }

                return Color.FromArgb(r, g, b);
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(e.ImageRectangle.Location, e.ImageRectangle.Size);
                r.Inflate(1, 1);
                e.Graphics.FillRectangle(new SolidBrush(ImageColor), r);
                //r.Inflate(-4, -4);
                e.Graphics.DrawLines(Pens.Gray, new Point[]
                {
                    new Point(r.Left + 4, 10), //2
                    new Point(r.Left - 2 + r.Width / 2,  r.Height / 2 + 4), //3
                    new Point(r.Right - 4, r.Top + 4)
                });
            }
        }

    }


}
