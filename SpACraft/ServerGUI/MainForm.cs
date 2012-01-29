using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpACraft.Player;
using SpACraft;
using System.Threading;
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
        }

        #region startup
        Thread startupThread;

        #endregion

        #region shutdown

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (startupThread != null && !shutdownComplete)
            {
                Shutdown(SpACraft.Server.ShutdownReason.ProcessClosing, true);
                e.Cancel = true;
            }
            else
            {
                base.OnFormClosing(e);
            }
        }


        void Shutdown(SpACraft.Server.ShutdownReason reason, bool quit)
        {
            if (shutdownPending) return;
            shutdownPending = true;
            ConsoleInput.Enabled = false;
            ConsoleInput.Text = "Shutting down...";
            Text = "SpACraft " + SpACraft.SpACraft.version + " - shutting down...";
            this.ServerURL.Enabled = false;
            if (!startupComplete)
            {
                startupThread.Join();
            }
            //Server.Shutdown(new SpACraft.Server.ShutdownParams(reason, TimeSpan.Zero, quit, false), false);
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
                            //if (Server.HasArg(ArgKey.ExitOnCrash))
                            //{
                            //    Application.Exit();
                            //}
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

        public void logMessage(String message, Boolean toConsole)
        {
            if (!toConsole) return;
            try
            {
                if (shutdownComplete) return;
                ConsoleOutput.AppendText(message + Environment.NewLine);
                if (ConsoleOutput.Lines.Length > MaxLinesInLog)
                {
                    ConsoleOutput.Text = "----- cut off, see fCraft.log for complete log -----" +
                        Environment.NewLine +
                        ConsoleOutput.Text.Substring(ConsoleOutput.GetFirstCharIndexFromLine(50));
                }
                ConsoleOutput.SelectionStart = ConsoleOutput.Text.Length;
                ConsoleOutput.ScrollToCaret();
                //if (!Server.IsRunning || shutdownPending) ConsoleOutput.Refresh();

            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException) { }
        }

        public void ChangeHeartBeatUri(String newUri)
        {
            ServerURL.Text = newUri;
        }

        public void ChangePlayerList(Player[] newPlayers)
        {

        }

        public void ConsoleInput_OnCommand()
        {
            string[] separator = { Environment.NewLine };
            string[] lines = ConsoleInput.Text.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
#if !DEBUG
                try
                {
#endif
                    if (line.Equals("/Clear", StringComparison.OrdinalIgnoreCase))
                    {
                        ConsoleOutput.Clear();
                        logMessage("Cleared console!", true);
                    }else if (line.Equals("/About", StringComparison.OrdinalIgnoreCase) || line.Equals("/Credits", StringComparison.OrdinalIgnoreCase))
                    {
                        logMessage("Showing credits...", true);
                        About box = new About(SpACraft.SpACraft.version);
                        box.ShowDialog();
                    }
                    else if (line.Equals("/Help", StringComparison.OrdinalIgnoreCase))
                    {
                        logMessage("------------Help----------", true);
                        logMessage("/clear - Clear the console",true);
                        logMessage("/credits - Show the credits window", true);
                        //temp^
                    }
                    else if (line.Equals("/stop", StringComparison.OrdinalIgnoreCase))
                    {
                        logMessage("Stopping the server...", true);
                        shutdownComplete = true;
                        shutdownPending = true;
                        
                        //temp^
                    }
                    else
                    {
                        // TODO
                        //Player.Console.ParseMessage(line, true);
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
            ConsoleInput.Text = "";
        }
    }
}