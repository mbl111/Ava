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

namespace ServerGUI
{
    public sealed partial class MainForm : Form
    {
        volatile bool shutdownPending, startupComplete, shutdownComplete;
        const int MaxLinesInLog = 2000;

        string version = "0.1";

        public MainForm()
        {
            InitializeComponent();
        }

        #region startup

        #endregion

        #region shutdown

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
                        About box = new About(version);
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

        private void ServerURL_TextChanged(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!shutdownComplete)
            {
                MessageBox.Show("Server still running!", "Warning");
                e.Cancel = true;
            }else
            if (!shutdownComplete || !shutdownPending)
            {
                MessageBox.Show("Server is shutting down!", "Warning");
                e.Cancel = true;
            }
        }
    }
}