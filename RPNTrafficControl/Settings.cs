using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPNTrafficControl
{
    public partial class Settings : Form
    {


        public Settings()
        {
            InitializeComponent();

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

        private void Settings_Load(object sender, EventArgs e)
        {
            // Load Settings
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.startTime))
                Properties.Settings.Default.startTime = "3:30 am";

            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.stopTime))
                Properties.Settings.Default.stopTime = "10:00 pm";

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
            this.Hide();
        }

        private void stopTimePicker_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.stopTime = stopTimePicker.Value.ToString("hh:mm tt");
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                Process[] obs = Process.GetProcessesByName("obs64");
                if (obs.Length != 0)
                {
                    obs[0].Kill();
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = @"C:\Program Files\obs-studio\bin\64bit\";
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

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            Process.Start(@"https://reddavid.me");
        }
    }
}
