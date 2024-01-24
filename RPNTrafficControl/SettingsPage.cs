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
        private readonly OBSContext _obs;

        public SettingsPage()
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
        }

        public SettingsPage(OBSContext obs) : base()
        {
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
        }

        private void stopTimePicker_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.StopTime = stopTimePicker.Value.ToString("hh:mm tt");
        }


        private void btnTest_Click(object sender, EventArgs e)
        {
           
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
