using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using SpACraft.Player;
using SpACraft;
using SpACraft.Events;

namespace ServerGUI
{
    public sealed partial class MainForm : Form
    {
        volatile bool shutdownPending, startupComplete, shutdownComplete;
        const int MaxLinesInLog = 2000;

        public MainForm()
        {
            InitializeComponent();
            Shown += StartUp;
            ConsoleInput.OnCommand += console_Enter;
        }


        #region Startup
        Thread startupThread;

        void StartUp(object sender, EventArgs a)
        {
            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;
            Server.PlayerListChanged += OnPlayerListChanged;
            Server.ShutdownEnded += OnServerShutdownEnded;
            Text = "SpACRaft " + SpACraft.SpACraft.version + " - starting...";
            startupThread = new Thread(StartupThread);
            startupThread.Name = "fCraft ServerGUI Startup";
            startupThread.Start();
        }


        void StartupThread()
        {
#if !DEBUG
            try
            {
#endif
                Server.InitLibrary(Environment.GetCommandLineArgs());
                if (shutdownPending) return;

                Server.InitServer();
                if (shutdownPending) return;

                BeginInvoke((Action)OnInitSuccess);

                //UpdaterResult update = Updater.CheckForUpdates();
                if (shutdownPending) return;

                //if (update.UpdateAvailable)
                //{
                //    new UpdateWindow(update, false).ShowDialog();
                //}

                if (!ConfigKey.ProcessPriority.IsBlank())
                {
                    try
                    {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    }
                    catch (Exception)
                    {
                        Logger.Log(LogType.Warning,
                                    "MainForm.StartServer: Could not set process priority, using defaults.");
                    }
                }

                if (shutdownPending) return;
                if (Server.StartServer())
                {
                    startupComplete = true;
                    BeginInvoke((Action)OnStartupSuccess);
                }
                else
                {
                    BeginInvoke((Action)OnStartupFailure);
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                Logger.LogAndReportCrash("Unhandled exception in ServerGUI.StartUp", "ServerGUI", ex, true);
                Shutdown(ShutdownReason.Crashed, Server.HasArg(ArgKey.ExitOnCrash));
            }
#endif
        }


        void OnInitSuccess()
        {
            Text = "SpACraft " + SpACraft.SpACraft.version + " - " + ConfigKey.ServerName.GetString();
        }


        void OnStartupSuccess()
        {
            if (!ConfigKey.HeartbeatEnabled.Enabled())
            {
                ServerURL.Text = "Heartbeat disabled. See externalurl.txt";
            }
            ConsoleOutput.Enabled = true;
            ConsoleOutput.Text = "";
        }


        void OnStartupFailure()
        {
            Shutdown(ShutdownReason.FailedToStart, Server.HasArg(ArgKey.ExitOnCrash));
        }

        #endregion


        #region Shutdown

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (startupThread != null && !shutdownComplete)
            {
                Shutdown(ShutdownReason.ProcessClosing, true);
                e.Cancel = true;
            }
            else
            {
                base.OnFormClosing(e);
            }
        }


        void Shutdown(ShutdownReason reason, bool quit)
        {
            if (shutdownPending) return;
            shutdownPending = true;
            ConsoleOutput.Enabled = false;
            ConsoleOutput.Text = "Shutting down...";
            Text = "SpACraft " + SpACraft.SpACraft.version + " - shutting down...";
            ServerURL.Enabled = false;
            if (!startupComplete)
            {
                startupThread.Join();
            }
            Server.Shutdown(new ShutdownParams(reason, TimeSpan.Zero, quit, false), false);
        }


        void OnServerShutdownEnded(object sender, ShutdownEventArgs e)
        {
            try
            {
                BeginInvoke((Action)delegate
                {
                    shutdownComplete = true;
                    switch (e.ShutdownParams.Reason)
                    {
                        case SpACraft.Server.ShutdownReason.FailedToInitialize:
                        case SpACraft.Server.ShutdownReason.FailedToStart:
                        case SpACraft.Server.ShutdownReason.Crashed:
                            if (Server.HasArg(ArgKey.ExitOnCrash))
                            {
                                Application.Exit();
                            }
                            break;
                        default:
                            Application.Exit();
                            break;
                    }
                });
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException) { }
        }

        #endregion


        public void OnLogged(object sender, LogEventArgs e)
        {
            if (!e.WriteToConsole) return;
            try
            {
                if (shutdownComplete) return;
                if (ConsoleOutput.InvokeRequired)
                {
                    BeginInvoke((EventHandler<LogEventArgs>)OnLogged, sender, e);
                }
                else
                {
                    ConsoleOutput.AppendText(e.Message + Environment.NewLine);
                    if (ConsoleOutput.Lines.Length > MaxLinesInLog)
                    {
                        ConsoleOutput.Text = "----- cut off, see SpACraft.log for complete log -----" +
                            Environment.NewLine +
                            ConsoleOutput.Text.Substring(ConsoleOutput.GetFirstCharIndexFromLine(50));
                    }
                    ConsoleOutput.SelectionStart = ConsoleOutput.Text.Length;
                    ConsoleOutput.ScrollToCaret();
                    if (!Server.IsRunning || shutdownPending) ConsoleOutput.Refresh();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException) { }
        }


        public void OnHeartbeatUriChanged(object sender, UriChangedEventArgs e)
        {
            try
            {
                if (shutdownPending) return;
                if (ServerURL.InvokeRequired)
                {
                    BeginInvoke((EventHandler<UriChangedEventArgs>)OnHeartbeatUriChanged,
                            sender, e);
                }
                else
                {
                    ServerURL.Text = e.NewUri.ToString();
                    ServerURL.Enabled = true;
                    btnPlay.Enabled = true;
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException) { }
        }


        public void OnPlayerListChanged(object sender, EventArgs e)
        {
            try
            {
                if (shutdownPending) return;
                if (onlinePlayers.InvokeRequired)
                {
                    BeginInvoke((EventHandler)OnPlayerListChanged, null, EventArgs.Empty);
                }
                else
                {
                    onlinePlayers.Items.Clear();
                    Player[] playerListCache = Server.onlinePlayers.OrderBy(p => p.Info.Rank.Index).ToArray();
                    foreach (Player player in playerListCache)
                    {
                        onlinePlayers.Items.Add(player.Info.Rank.Name + " - " + player.Name);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException) { }
        }


        private void console_Enter()
        {
            string[] separator = { Environment.NewLine };
            string[] lines = ConsoleOutput.Text.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
#if !DEBUG
                try
                {
#endif
                    if (line.Equals("/clear", StringComparison.OrdinalIgnoreCase))
                    {
                        ConsoleOutput.Clear();
                    }
                    else if (line.Equals("/credits", StringComparison.OrdinalIgnoreCase))
                    {
                        new About(SpACraft.SpACraft.version).Show();
                    }
                    else
                    {
                        Player.Console.ParseMessage(line, true);
                    }
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Logger.LogToConsole("Error occured while trying to execute last console command: ");
                    Logger.LogToConsole(ex.GetType().Name + ": " + ex.Message);
                    Logger.LogAndReportCrash("Exception executing command from console", "ServerGUI", ex, false);
                }
#endif
            }
            ConsoleOutput.Text = "";
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(btnPlay.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Could not open server URL. Please copy/paste it manually.");
            }
        }

        private void onlinePlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void About_Click_1(object sender, EventArgs e)
        {
            new About(SpACraft.SpACraft.version).Show();
        }

        private void Player_Profile_Click(object sender, EventArgs e)
        {
            ProfileViewer viewer = new ProfileViewer();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new MapViewer();
        }

        
    }
}