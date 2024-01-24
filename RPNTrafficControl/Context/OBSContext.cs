using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPNTrafficControl.Context
{
    public class OBSContext
    {
        protected static OBSWebsocket _obs;
        public static bool IsConnected { get; }

        private Control ConnectDisconnectButton;
        public OBSContext(SettingsPage page)
        {
            _obs = new OBSWebsocket();
            _obs.Connected += onConnect;
            _obs.Disconnected += onDisconnect;

            Application.ThreadException += Application_ThreadException;

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ConnectDisconnectButton = page.Controls["btnTest"] as Control;
            ConnectDisconnectButton.Click += ConnectDisconnectButton_Click;
        }

        private void ConnectDisconnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_obs is null) return;
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
            ConnectDisconnectButton.Text = "Connecting...";

            if (!_obs.IsConnected)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _obs.ConnectAsync(
                            url: "ws://127.0.0.1:4455",
                            password: "rpn0629");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Connect failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                });
            }
        }

        private void onConnect(object sender, EventArgs e)
        {
            var versionInfo = _obs.GetVersion();

            var streamStatus = _obs.GetStreamStatus();

            var recordStatus = _obs.GetRecordStatus();

            ConnectDisconnectButton.Text = "Disconnect";
        }

        private void onDisconnect(object sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
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

            ConnectDisconnectButton.Text = "Connect";

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
    }
}
