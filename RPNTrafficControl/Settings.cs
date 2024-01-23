using Microsoft.Win32;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace RPNTrafficControl
{
    public partial class Settings : Form
    {
        //Startup registry key and value
        OBSWebsocket _obs;
        public Settings(OBSWebsocket obs)
        {
            InitializeComponent();

            opd = new OpenFileDialog();
            opd.InitialDirectory = @"C:\Program Files\";
            opd.RestoreDirectory = true;
            opd.Title = "Browse obs64.exe";
            opd.FileName = "obs64.exe";
            opd.Filter = "obs64 (.exe)|*.exe|All Files (*.*)|*.*";
            opd.FilterIndex = 1;

            this.Load += Settings_Load;
            this.FormClosing += Settings_FormClosing;

            _obs = obs;
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Settings_FormClosed(object sender, FormClosedEventArgs e)
        {
            //throw new NotImplementedException();
        }


        private async void Settings_Load(object sender, EventArgs e)
        {

            // Load Settings
            LoadSettings();
            Application.ThreadException += Application_ThreadException;

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            await Task.Delay(5000);
            EnsureWebSocketConnection();
        }

        private void onConnect(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                var versionInfo = _obs.GetVersion();

                var streamStatus = _obs.GetStreamStatus();

                var recordStatus = _obs.GetRecordStatus();
            }));
        }

        private void onRecordStateChanged(object sender, RecordStateChangedEventArgs args)
        {
            string state = "";
            switch (args.OutputState.State)
            {
                case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
                    state = "Recording starting...";
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                case OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                    state = "Stop recording";
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                    state = "Recording stopping...";
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                    state = "Start recording";
                    break;
                case OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
                    state = "(P) Stop recording";
                    break;

                default:
                    state = "State unknown";
                    break;
            }


        }
        private void onDisconnect(object sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                if (e.ObsCloseCode == OBSWebsocketDotNet.Communication.ObsCloseCodes.AuthenticationFailed)
                {
                    MessageBox.Show("Authentication failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else if (e.WebsocketDisconnectionInfo != null)
                {
                    if (e.WebsocketDisconnectionInfo.Exception != null)
                    {
                        MessageBox.Show($"Connection failed: CloseCode: {e.ObsCloseCode} Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription} Exception:{e.WebsocketDisconnectionInfo?.Exception?.Message}\nType: {e.WebsocketDisconnectionInfo.Type}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else
                    {
                        MessageBox.Show($"Connection failed: CloseCode: {e.ObsCloseCode} Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription}\nType: {e.WebsocketDisconnectionInfo.Type}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else
                {
                    MessageBox.Show($"Connection failed: CloseCode: {e.ObsCloseCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }));

            // Try reconnecting
            EnsureWebSocketConnection();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"{((Exception)e.ExceptionObject).Message}", "Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"{((Exception)e.Exception).Message}", "Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        protected override void OnLoad(EventArgs e)
        {
            var btn = new Button();
            btn.Size = new Size(25, textBox1.ClientSize.Height + 2);
            btn.Location = new Point(textBox1.ClientSize.Width - btn.Width, -1);
            btn.Cursor = Cursors.Default;
            btn.Text = "...";
            btn.Click += btn_Click;
            textBox1.Controls.Add(btn);

            if (!ExistsOnPath(@"C:\Program Files\obs-studio\bin\64bit\obs64.exe"))
            {
                var mb = MessageBox.Show("OBS Studio executable not found on default installation path. Would you like to browse for it?",
                    "Missing executable file",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (mb == DialogResult.Yes)
                {
                    if (opd.ShowDialog() == DialogResult.OK)
                    {
                        textBox1.Text = opd.FileName;
                        Properties.Settings.Default.obsExeLocation = opd.FileName;
                        Properties.Settings.Default.Save();
                    }
                }
                else
                {
                    textBox1.Text = "";
                }

            }
            // Send EM_SETMARGINS to prevent text from disappearing underneath the button
            SendMessage(textBox1.Handle, 0xd3, (IntPtr)2, (IntPtr)(btn.Width << 16));
            base.OnLoad(e);
        }



        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        System.Windows.Forms.OpenFileDialog opd;
        private void btn_Click(object sender, EventArgs e)
        {
            // Search for obs64.exe

            if (opd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = opd.FileName;
                Properties.Settings.Default.obsExeLocation = opd.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void LoadSettings()
        {
            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.startTime))
                Properties.Settings.Default.startTime = "3:30 am";

            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.stopTime))
                Properties.Settings.Default.stopTime = "10:00 pm";

            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.obsExeLocation))
                Properties.Settings.Default.obsExeLocation = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe";

            this.checkBox1.Checked = Properties.Settings.Default.runAtStart;
            this.textBox1.Text = Properties.Settings.Default.obsExeLocation;
            this.startTimePicker.Value = DateTime.Parse(Properties.Settings.Default.startTime);
            this.stopTimePicker.Value = DateTime.Parse(Properties.Settings.Default.stopTime);

            Properties.Settings.Default.Save();
        }

        private void startTimePicker_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.startTime = startTimePicker.Value.ToString("hh:mm tt");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void stopTimePicker_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.stopTime = stopTimePicker.Value.ToString("hh:mm tt");
        }

        protected bool IsObsConnected;

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                Process[] obs = Process.GetProcessesByName("obs64");
                if (obs.Length != 0)
                {
                    // Turn off recording

                    if (this._obs.IsConnected)
                    {
                        this._obs.StopRecord();
                        // obs[0].Kill();
                    }
                    else
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.WorkingDirectory = Properties.Settings.Default.obsExeLocation.Replace("obs64.exe", string.Empty);
                        psi.FileName = @"obs64.exe";
                        psi.UseShellExecute = true;
                        psi.Arguments = @"--disable-shutdown-check --startrecording ";
                        Process.Start(psi);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message, MessageBoxButtons.OK);
            }
        }

        private void EnsureWebSocketConnection()
        {
            // Password jf2EywIrTpatEoJc 192.168.254.162 4455

            if (!this._obs.IsConnected)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        this._obs.ConnectAsync("ws://192.168.254.162:4455", "jf2EywIrTpatEoJc");
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            MessageBox.Show("Connect failed : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        });
                    }
                });
            }

            _obs.Connected += onConnect;
            _obs.Disconnected += onDisconnect;
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            var ps = new ProcessStartInfo("https://reddavid.me")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.runAtStart = checkBox1.Checked;
        }
    }
}
