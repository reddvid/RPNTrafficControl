using Microsoft.Win32;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using RPNTrafficControl.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace RPNTrafficControl
{
    public partial class SettingsPage : Form
    {
        //Startup registry key and value
        public static OBSWebsocket _obs;

        public SettingsPage(OBSWebsocket obs)
        {
            InitializeComponent();

            _obs = obs;

            opd = new OpenFileDialog();
            opd.InitialDirectory = @"C:\Program Files\";
            opd.RestoreDirectory = true;
            opd.Title = "Browse obs64.exe";
            opd.FileName = "obs64.exe";
            opd.Filter = "obs64 (.exe)|*.exe|All Files (*.*)|*.*";
            opd.FilterIndex = 1;

            this.Load += Settings_Load;
            this.FormClosing += Settings_FormClosing;
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

            _obs = new OBSWebsocket();
            _obs.Connected += onConnect;
            _obs.Disconnected += onDisconnect;
            _obs.RecordStateChanged += onRecordStateChanged;


            Application.ThreadException += Application_ThreadException;

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public async Task DoControlOBS()
        {
            EnsureWebsocketConnection();

            await Task.Delay(5_000);

            var timeNow = DateTime.Now.ToString("hh:mm tt");

            Console.WriteLine($"Time Now: {timeNow}");
            Console.WriteLine($"IsRecording: {IsRecording}");

            if (timeNow == Properties.Settings.Default.StartTime)
            {
                _obs.StartRecord();
            }
            else if (timeNow == Properties.Settings.Default.StopTime)
            {
                _obs.StopRecord();
            }
            else if (IsCurrentTimeWithinOperation())
            {
                if (!IsRecording)
                {
                    _obs.StartRecord();
                }
            }
            else if (!IsCurrentTimeWithinOperation())
            {
                if (IsRecording)
                {
                    _obs.StopRecord();
                }
            }
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

        private void onRecordStateChanged(object sender, RecordStateChangedEventArgs e)
        {
            string state = "";
            switch (e.OutputState.State)
            {
                case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
                    state = "Recording starting...";
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                case OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                    state = "Stop recording";
                    IsRecording = true;
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                    state = "Recording stopping...";
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                    state = "Start recording";
                    IsRecording = false;
                    break;
                case OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
                    state = "(P) Stop recording";
                    break;

                default:
                    state = "State unknown";
                    break;
            }

            BeginInvoke((MethodInvoker)delegate
            {
                //
            });
        }

        private void onDisconnect(object sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                btnTest.Text = "Connect";
            }));

            // Try reconnecting
            EnsureWebsocketConnection();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"{((Exception)e.ExceptionObject).Message}", "Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"{((Exception)e.Exception).Message}", "Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public bool IsRecording { get; private set; }

        private void onConnect(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                var recordStatus = _obs.GetRecordStatus();
                if (recordStatus.IsRecording)
                {
                    onRecordStateChanged(_obs, new RecordStateChangedEventArgs(new RecordStateChanged() { IsActive = true, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STARTED) }));
                }
                else
                {
                    onRecordStateChanged(_obs, new RecordStateChangedEventArgs(new RecordStateChanged() { IsActive = false, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED) }));
                }

                btnTest.Text = "Disconnect";
            }));
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
                        Properties.Settings.Default.OBSExeLocation = opd.FileName;
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
                Properties.Settings.Default.OBSExeLocation = opd.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void LoadSettings()
        {
            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.StartTime))
                Properties.Settings.Default.StartTime = "3:30 am";

            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.StopTime))
                Properties.Settings.Default.StopTime = "10:00 pm";

            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.OBSExeLocation))
                Properties.Settings.Default.OBSExeLocation = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe";

            this.checkBox1.Checked = Properties.Settings.Default.IsStartupEnabled;
            this.textBox1.Text = Properties.Settings.Default.OBSExeLocation;
            this.startTimePicker.Value = DateTime.Parse(Properties.Settings.Default.StartTime);
            this.stopTimePicker.Value = DateTime.Parse(Properties.Settings.Default.StopTime);

            Properties.Settings.Default.Save();
        }

        private void startTimePicker_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.StartTime = startTimePicker.Value.ToString("hh:mm tt");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();

            DoControlOBS();
        }

        private void stopTimePicker_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.StopTime = stopTimePicker.Value.ToString("hh:mm tt");
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                if (_obs.IsConnected)
                {
                    _obs.Disconnect();
                }
                else
                {
                    EnsureWebsocketConnection();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
            }
        }

        public void EnsureWebsocketConnection()
        {
            if (!_obs.IsConnected)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            btnTest.Text = "Connecting...";
                        }));

                        _obs.ConnectAsync(
                            url: "ws://127.0.0.1:4455",
                            password: "rpn0629");
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            MessageBox.Show($"Connect failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }));
                    }
                });
            }
            else
            {
                BeginInvoke((MethodInvoker)(() =>
                {
                    btnTest.Text = "Disconnect";
                }));
            }
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
            Properties.Settings.Default.IsStartupEnabled = checkBox1.Checked;
        }
    }
}
