using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SpACraft.AutoRank;
using SpACraft.Drawing;
using SpACraft.Events;
using JetBrains.Annotations;
using ThreadState = System.Threading.ThreadState;

namespace SpACraft
{
    public static partial class Server
    {

        public static DateTime StartTime { get; private set; }

        internal static int MaxUploadSpeed,
                            BlockUpdateThrottling;

        internal const int MaxSessionPacketsPerTick = 128,
                           MaxBlockUpdatesPerTick = 100000;
        internal static float TicksPerSecond;


        static TcpListener listener;
        public static IPAddress InternalIP { get; private set; }
        public static IPAddress ExternalIP { get; private set; }

        public static int Port { get; private set; }

        public static Uri Uri { get; internal set; }


        #region Command-line args

        static readonly Dictionary<ArgKey, string> Args = new Dictionary<ArgKey, string>();


        [CanBeNull]
        public static string GetArg(ArgKey key)
        {
            if (Args.ContainsKey(key))
            {
                return Args[key];
            }
            else
            {
                return null;
            }
        }


        public static bool HasArg(ArgKey key)
        {
            return Args.ContainsKey(key);
        }


        public static string GetArgString()
        {
            return String.Join(" ", GetArgList());
        }


        public static string[] GetArgList()
        {
            List<string> argList = new List<string>();
            foreach (var pair in Args)
            {
                if (pair.Value != null)
                {
                    argList.Add(String.Format("--{0}=\"{1}\"", pair.Key.ToString().ToLower(), pair.Value));
                }
                else
                {
                    argList.Add(String.Format("--{0}", pair.Key.ToString().ToLower()));
                }
            }
            return argList.ToArray();
        }

        #endregion


        #region Initialization and Startup

        static bool libraryInitialized,
                    serverInitialized;
        public static bool IsRunning { get; private set; }

        public static void InitLibrary([NotNull] IEnumerable<string> rawArgs)
        {
            if (rawArgs == null) throw new ArgumentNullException("rawArgs");
            if (libraryInitialized)
            {
                throw new InvalidOperationException("SpACraft library is already initialized");
            }

            ServicePointManager.Expect100Continue = false;

            foreach (string arg in rawArgs)
            {
                if (arg.StartsWith("--"))
                {
                    string argKeyName, argValue;
                    if (arg.Contains('='))
                    {
                        argKeyName = arg.Substring(2, arg.IndexOf('=') - 2).ToLower().Trim();
                        argValue = arg.Substring(arg.IndexOf('=') + 1).Trim();
                        if (argValue.StartsWith("\"") && argValue.EndsWith("\""))
                        {
                            argValue = argValue.Substring(1, argValue.Length - 2);
                        }

                    }
                    else
                    {
                        argKeyName = arg.Substring(2);
                        argValue = null;
                    }
                    ArgKey key;
                    if (EnumUtil.TryParse(argKeyName, out key, true))
                    {
                        Args.Add(key, argValue);
                    }
                    else
                    {
                        Console.Error.WriteLine("Unknown argument: {0}", arg);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Unknown argument: {0}", arg);
                }
            }

            Directory.SetCurrentDirectory(Paths.WorkingPath);

            string path = GetArg(ArgKey.Path);
            if (path != null && Paths.TestDirectory("WorkingPath", path, true))
            {
                Paths.WorkingPath = Path.GetFullPath(path);
                Directory.SetCurrentDirectory(Paths.WorkingPath);
            }
            else if (Paths.TestDirectory("WorkingPath", Paths.WorkingPathDefault, true))
            {
                Paths.WorkingPath = Path.GetFullPath(Paths.WorkingPathDefault);
                Directory.SetCurrentDirectory(Paths.WorkingPath);
            }
            else
            {
                throw new IOException("Could not set the working path.");
            }

            string logPath = GetArg(ArgKey.LogPath);
            if (logPath != null && Paths.TestDirectory("LogPath", logPath, true))
            {
                Paths.LogPath = Path.GetFullPath(logPath);
            }
            else if (Paths.TestDirectory("LogPath", Paths.LogPathDefault, true))
            {
                Paths.LogPath = Path.GetFullPath(Paths.LogPathDefault);
            }
            else
            {
                throw new IOException("Could not set the log path.");
            }

            if (HasArg(ArgKey.NoLog))
            {
                Logger.Enabled = false;
            }
            else
            {
                Logger.MarkLogStart();
            }

            string mapPath = GetArg(ArgKey.MapPath);
            if (mapPath != null && Paths.TestDirectory("MapPath", mapPath, true))
            {
                Paths.MapPath = Path.GetFullPath(mapPath);
                Paths.IgnoreMapPathConfigKey = true;
            }
            else if (Paths.TestDirectory("MapPath", Paths.MapPathDefault, true))
            {
                Paths.MapPath = Path.GetFullPath(Paths.MapPathDefault);
            }
            else
            {
                throw new IOException("Could not set the map path.");
            }

            Paths.ConfigFileName = Paths.ConfigFileNameDefault;
            string configFile = GetArg(ArgKey.Config);
            if (configFile != null)
            {
                if (Paths.TestFile("config.xml", configFile, false, FileAccess.Read))
                {
                    Paths.ConfigFileName = new FileInfo(configFile).FullName;
                }
            }

            if (MonoCompat.IsMono)
            {
                Logger.Log(LogType.Debug, "Running on Mono {0}", MonoCompat.MonoVersion);
            }

#if DEBUG_EVENTS
            Logger.PrepareEventTracing();
#endif

            Logger.Log(LogType.Debug, "Working directory: {0}", Directory.GetCurrentDirectory());
            Logger.Log(LogType.Debug, "Log path: {0}", Path.GetFullPath(Paths.LogPath));
            Logger.Log(LogType.Debug, "Map path: {0}", Path.GetFullPath(Paths.MapPath));
            Logger.Log(LogType.Debug, "Config path: {0}", Path.GetFullPath(Paths.ConfigFileName));

            libraryInitialized = true;
        }


        public static void InitServer()
        {
            if (serverInitialized)
            {
                throw new InvalidOperationException("Server is already initialized");
            }
            if (!libraryInitialized)
            {
                throw new InvalidOperationException("Server.InitLibrary must be called before Server.InitServer");
            }
            RaiseEvent(Initializing);

            using (var testMemStream = new MemoryStream())
            {
                using (new DeflateStream(testMemStream, CompressionMode.Compress))
                {
                }
            }

#if DEBUG
                Logger.Log( LogType.Warning, unstableMessage );
#else
                throw new Exception(unstableMessage);
#endif
            

            if (MonoCompat.IsMono && !MonoCompat.IsSGenCapable)
            {
                Logger.Log(LogType.Warning,
                            "You are using a relatively old version of the Mono runtime ({0}). " +
                            "It is recommended that you upgrade to at least 2.8+",
                            MonoCompat.MonoVersion);
            }

#if DEBUG
            Config.RunSelfTest();
#else
            File.Delete(Paths.UpdaterFileName);
            File.Delete("SpACraftUpdater.exe");
#endif

            if (!Config.Load(false, false))
            {
                throw new Exception("SpACraft Config failed to initialize");
            }

            if (ConfigKey.VerifyNames.GetEnum<NameVerificationMode>() == NameVerificationMode.Never)
            {
                Logger.Log(LogType.Warning,
                            "Name verification is currently OFF. Your server is at risk of being hacked. " +
                            "Enable name verification as soon as possible.");
            }

            PlayerDB.Load();
            IPBanList.Load();

            CommandManager.Init();

            BrushManager.Init();

            IRC.Init();

            if (ConfigKey.AutoRankEnabled.Enabled())
            {
                AutoRankManager.Init();
            }

            RaiseEvent(Initialized);

            serverInitialized = true;
        }

        public static bool StartServer()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Server is already running");
            }
            if (!libraryInitialized || !serverInitialized)
            {
                throw new InvalidOperationException("Server.InitLibrary and Server.InitServer must be called before Server.StartServer");
            }

            StartTime = DateTime.UtcNow;
            cpuUsageStartingOffset = Process.GetCurrentProcess().TotalProcessorTime;
            Players = new Player[0];

            RaiseEvent(Starting);

            if (ConfigKey.BackupDataOnStartup.Enabled())
            {
                BackupData();
            }

            Player.Console = new Player(ConfigKey.ConsoleName.GetString());
            Player.AutoRank = new Player("(AutoRank)");

            if (ConfigKey.BlockDBEnabled.Enabled()) BlockDB.Init();

            if (!WorldManager.LoadWorldList()) return false;
            WorldManager.SaveWorldList();

            Port = ConfigKey.Port.GetInt();
            InternalIP = IPAddress.Parse(ConfigKey.IP.GetString());

            try
            {
                listener = new TcpListener(InternalIP, Port);
                listener.Start();

            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error,
                            "Could not start listening on port {0}, stopping. ({1})",
                            Port, ex.Message);
                if (!ConfigKey.IP.IsBlank())
                {
                    Logger.Log(LogType.Warning,
                                "Do not use the \"Designated IP\" setting unless you have multiple NICs or IPs.");
                }
                return false;
            }

            InternalIP = ((IPEndPoint)listener.LocalEndpoint).Address;
            ExternalIP = CheckExternalIP();

            if (ExternalIP == null)
            {
                Logger.Log(LogType.SystemActivity,
                            "Server.Run: now accepting connections on port {0}", Port);
            }
            else
            {
                Logger.Log(LogType.SystemActivity,
                            "Server.Run: now accepting connections at {0}:{1}",
                            ExternalIP, Port);
            }


            WorldManager.UpdateWorldList();
            Logger.Log(LogType.SystemActivity,
                        "All available worlds: {0}",
                        WorldManager.Worlds.JoinToString(", ", w => w.Name));

            Logger.Log(LogType.SystemActivity,
                        "Main world: {0}; default rank: {1}",
                        WorldManager.MainWorld.Name, RankManager.DefaultRank.Name);

            checkConnectionsTask = Scheduler.NewTask(CheckConnections).RunForever(CheckConnectionsInterval);

            checkIdlesTask = Scheduler.NewTask(CheckIdles).RunForever(CheckIdlesInterval);

            try
            {
                MonitorProcessorUsage(null);
                Scheduler.NewTask(MonitorProcessorUsage).RunForever(MonitorProcessorUsageInterval,
                                                                       MonitorProcessorUsageInterval);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error,
                            "Server.StartServer: Could not start monitoring CPU use: {0}", ex);
            }


            PlayerDB.StartSaveTask();

            if (ConfigKey.AnnouncementInterval.GetInt() > 0)
            {
                TimeSpan announcementInterval = TimeSpan.FromMinutes(ConfigKey.AnnouncementInterval.GetInt());
                Scheduler.NewTask(ShowRandomAnnouncement).RunForever(announcementInterval);
            }

            gcTask = Scheduler.NewTask(DoGC).RunForever(GCInterval, TimeSpan.FromSeconds(45));

            Heartbeat.Start();
            if (ConfigKey.HeartbeatToWoMDirect.Enabled())
            {
                if (ExternalIP == null)
                {
                    Logger.Log(LogType.SystemActivity,
                                "WoM Direct heartbeat is enabled. To edit your server's appearence on the server list, " +
                                "see https://direct.worldofminecraft.com/server.php?port={0}&salt={1}",
                                Port, Heartbeat.Salt);
                }
                else
                {
                    Logger.Log(LogType.SystemActivity,
                                "WoM Direct heartbeat is enabled. To edit your server's appearence on the server list, " +
                                "see https://direct.worldofminecraft.com/server.php?ip={0}&port={1}&salt={2}",
                                ExternalIP, Port, Heartbeat.Salt);
                }
            }

            if (ConfigKey.RestartInterval.GetInt() > 0)
            {
                TimeSpan restartIn = TimeSpan.FromSeconds(ConfigKey.RestartInterval.GetInt());
                Shutdown(new ShutdownParams(ShutdownReason.Restarting, restartIn, true, true), false);
                ChatTimer.Start(restartIn, "Automatic Server Restart", Player.Console.Name);
            }

            if (ConfigKey.IRCBotEnabled.Enabled()) IRC.Start();

            Scheduler.NewTask(AutoRankManager.TaskCallback).RunForever(AutoRankManager.TickInterval);

            Scheduler.Start();
            IsRunning = true;

            RaiseEvent(Started);
            return true;
        }

        #endregion


        #region Shutdown

        static readonly object ShutdownLock = new object();
        public static bool IsShuttingDown;
        static readonly AutoResetEvent ShutdownWaiter = new AutoResetEvent(false);
        static Thread shutdownThread;
        static ChatTimer shutdownTimer;


        static void ShutdownNow([NotNull] ShutdownParams shutdownParams)
        {
            if (shutdownParams == null) throw new ArgumentNullException("shutdownParams");
            if (IsShuttingDown) return;
            IsShuttingDown = true;
#if !DEBUG
            try
            {
#endif
                RaiseShutdownBeganEvent(shutdownParams);

                Scheduler.BeginShutdown();

                Logger.Log(LogType.SystemActivity,
                            "Server shutting down ({0})",
                            shutdownParams.ReasonString);

                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }

                lock (SessionLock)
                {
                    if (Sessions.Count > 0)
                    {
                        foreach (Player p in Sessions)
                        {
                            p.Kick("Server shutting down (" + shutdownParams.ReasonString + Color.White + ")", LeaveReason.ServerShutdown);
                        }
                        Thread.Sleep(1000);
                    }
                }

                IRC.Disconnect();

                if (WorldManager.Worlds != null)
                {
                    lock (WorldManager.SyncRoot)
                    {
                        foreach (World world in WorldManager.Worlds)
                        {
                            if (world.BlockDB.IsEnabled) world.BlockDB.Flush();
                            world.SaveMap();
                        }
                    }
                }

                Scheduler.EndShutdown();

                if (PlayerDB.IsLoaded) PlayerDB.Save();
                if (IPBanList.IsLoaded) IPBanList.Save();

                RaiseShutdownEndedEvent(shutdownParams);
#if !DEBUG
            }
            catch (Exception ex)
            {
                Logger.LogAndReportCrash("Error in Server.Shutdown", "SpACraft", ex, true);
            }
#endif
        }


        public static void Shutdown([NotNull] ShutdownParams shutdownParams, bool waitForShutdown)
        {
            if (shutdownParams == null) throw new ArgumentNullException("shutdownParams");
            lock (ShutdownLock)
            {
                if (!CancelShutdown()) return;
                shutdownThread = new Thread(ShutdownThread)
                {
                    Name = "SpACraft.Shutdown"
                };
                if (shutdownParams.Delay >= ChatTimer.MinDuration)
                {
                    string timerMsg = String.Format("Server {0} ({1})",
                                                     shutdownParams.Restart ? "restart" : "shutdown",
                                                     shutdownParams.ReasonString);
                    string nameOnTimer;
                    if (shutdownParams.InitiatedBy == null)
                    {
                        nameOnTimer = Player.Console.Name;
                    }
                    else
                    {
                        nameOnTimer = shutdownParams.InitiatedBy.Name;
                    }
                    shutdownTimer = ChatTimer.Start(shutdownParams.Delay, timerMsg, nameOnTimer);
                }
                shutdownThread.Start(shutdownParams);
            }
            if (waitForShutdown)
            {
                ShutdownWaiter.WaitOne();
            }
        }


        public static bool CancelShutdown()
        {
            lock (ShutdownLock)
            {
                if (shutdownThread != null)
                {
                    if (IsShuttingDown || shutdownThread.ThreadState != ThreadState.WaitSleepJoin)
                    {
                        return false;
                    }
                    if (shutdownTimer != null)
                    {
                        shutdownTimer.Stop();
                        shutdownTimer = null;
                    }
                    ShutdownWaiter.Set();
                    shutdownThread.Abort();
                    shutdownThread = null;
                }
            }
            return true;
        }


        static void ShutdownThread([NotNull] object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            ShutdownParams param = (ShutdownParams)obj;
            Thread.Sleep(param.Delay);
            ShutdownNow(param);
            ShutdownWaiter.Set();

            bool doRestart = (param.Restart && !HasArg(ArgKey.NoRestart));
            string assemblyExecutable = Assembly.GetEntryAssembly().Location;

            if (Updater.RunAtShutdown && doRestart)
            {
                string args = String.Format("--restart=\"{0}\" {1}",
                                             MonoCompat.PrependMono(assemblyExecutable),
                                             GetArgString());

                MonoCompat.StartDotNetProcess(Paths.UpdaterFileName, args, true);

            }
            else if (Updater.RunAtShutdown)
            {
                MonoCompat.StartDotNetProcess(Paths.UpdaterFileName, GetArgString(), true);

            }
            else if (doRestart)
            {
                MonoCompat.StartDotNetProcess(assemblyExecutable, GetArgString(), true);
            }

            if (param.KillProcess)
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        #endregion


        #region Messaging / Packet Sending

        public static void Message([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            Players.Message(message);
        }


        [StringFormatMethod("message")]
        public static void Message([NotNull] string message, [NotNull] params object[] formatArgs)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (formatArgs == null) throw new ArgumentNullException("formatArgs");
            Players.Message(message, formatArgs);
        }


        public static void Message([CanBeNull] Player except, [NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            Players.Except(except).Message(message);
        }


        [StringFormatMethod("message")]
        public static void Message([CanBeNull] Player except, [NotNull] string message, [NotNull] params object[] formatArgs)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (formatArgs == null) throw new ArgumentNullException("formatArgs");
            Players.Except(except).Message(message, formatArgs);
        }

        #endregion


        #region Scheduled Tasks

        static SchedulerTask checkConnectionsTask;
        static TimeSpan checkConnectionsInterval = TimeSpan.FromMilliseconds(250);
        public static TimeSpan CheckConnectionsInterval
        {
            get { return checkConnectionsInterval; }
            set
            {
                if (value.Ticks < 0) throw new ArgumentException("CheckConnectionsInterval may not be negative.");
                checkConnectionsInterval = value;
                if (checkConnectionsTask != null) checkConnectionsTask.Interval = value;
            }
        }

        static void CheckConnections(SchedulerTask param)
        {
            TcpListener listenerCache = listener;
            if (listenerCache != null && listenerCache.Pending())
            {
                try
                {
                    Player.StartSession(listenerCache.AcceptTcpClient());
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error,
                                "Server.CheckConnections: Could not accept incoming connection: {0}", ex);
                }
            }
        }


        static SchedulerTask checkIdlesTask;
        static TimeSpan checkIdlesInterval = TimeSpan.FromSeconds(30);
        public static TimeSpan CheckIdlesInterval
        {
            get { return checkIdlesInterval; }
            set
            {
                if (value.Ticks < 0) throw new ArgumentException("CheckIdlesInterval may not be negative.");
                checkIdlesInterval = value;
                if (checkIdlesTask != null) checkIdlesTask.Interval = checkIdlesInterval;
            }
        }

        static void CheckIdles(SchedulerTask task)
        {
            Player[] tempPlayerList = Players;
            for (int i = 0; i < tempPlayerList.Length; i++)
            {
                Player player = tempPlayerList[i];
                if (player.Info.Rank.IdleKickTimer <= 0) continue;

                if (player.IdleTime.TotalMinutes >= player.Info.Rank.IdleKickTimer)
                {
                    Message("{0}&S was kicked for being idle for {1} min",
                             player.ClassyName,
                             player.Info.Rank.IdleKickTimer);
                    string kickReason = "Idle for " + player.Info.Rank.IdleKickTimer + " minutes";
                    player.Kick(Player.Console, kickReason, LeaveReason.IdleKick, false, true, false);
                    player.ResetIdleTimer(); // to prevent kick from firing more than once
                }
            }
        }


        static SchedulerTask gcTask;
        static TimeSpan gcInterval = TimeSpan.FromSeconds(60);
        public static TimeSpan GCInterval
        {
            get { return gcInterval; }
            set
            {
                if (value.Ticks < 0) throw new ArgumentException("GCInterval may not be negative.");
                gcInterval = value;
                if (gcTask != null) gcTask.Interval = gcInterval;
            }
        }

        static void DoGC(SchedulerTask task)
        {
            if (!gcRequested) return;
            gcRequested = false;

            Process proc = Process.GetCurrentProcess();
            proc.Refresh();
            long usageBefore = proc.PrivateMemorySize64 / (1024 * 1024);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

            proc.Refresh();
            long usageAfter = proc.PrivateMemorySize64 / (1024 * 1024);

            Logger.Log(LogType.Debug,
                        "Server.DoGC: Collected on schedule ({0}->{1} MB).",
                        usageBefore, usageAfter);
        }


        static void ShowRandomAnnouncement(SchedulerTask task)
        {
            if (!File.Exists(Paths.AnnouncementsFileName)) return;
            string[] lines = File.ReadAllLines(Paths.AnnouncementsFileName);
            if (lines.Length == 0) return;
            string line = lines[new Random().Next(0, lines.Length)].Trim();
            if (line.Length == 0) return;
            foreach (Player player in Players.Where(player => player.World != null))
            {
                player.Message("&R" + ReplaceTextKeywords(player, line));
            }
        }


        public static bool IsMonitoringCPUUsage { get; private set; }
        static TimeSpan cpuUsageStartingOffset;
        public static double CPUUsageTotal { get; private set; }
        public static double CPUUsageLastMinute { get; private set; }

        static TimeSpan oldCPUTime = new TimeSpan(0);
        static readonly TimeSpan MonitorProcessorUsageInterval = TimeSpan.FromSeconds(30);
        static DateTime lastMonitorTime = DateTime.UtcNow;

        static void MonitorProcessorUsage(SchedulerTask task)
        {
            TimeSpan newCPUTime = Process.GetCurrentProcess().TotalProcessorTime - cpuUsageStartingOffset;
            CPUUsageLastMinute = (newCPUTime - oldCPUTime).TotalSeconds /
                                 (Environment.ProcessorCount * DateTime.UtcNow.Subtract(lastMonitorTime).TotalSeconds);
            lastMonitorTime = DateTime.UtcNow;
            CPUUsageTotal = newCPUTime.TotalSeconds /
                            (Environment.ProcessorCount * DateTime.UtcNow.Subtract(StartTime).TotalSeconds);
            oldCPUTime = newCPUTime;
            IsMonitoringCPUUsage = true;
        }

        #endregion


        #region Utilities

        static bool gcRequested;

        public static void RequestGC()
        {
            gcRequested = true;
        }


        public static bool VerifyName([NotNull] string name, [NotNull] string hash, [NotNull] string salt)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (hash == null) throw new ArgumentNullException("hash");
            if (salt == null) throw new ArgumentNullException("salt");
            while (hash.Length < 32)
            {
                hash = "0" + hash;
            }
            MD5 hasher = MD5.Create();
            StringBuilder sb = new StringBuilder(32);
            foreach (byte b in hasher.ComputeHash(Encoding.ASCII.GetBytes(salt + name)))
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString().Equals(hash, StringComparison.OrdinalIgnoreCase);
        }


        public static int CalculateMaxPacketsPerUpdate([NotNull] World world)
        {
            if (world == null) throw new ArgumentNullException("world");
            int packetsPerTick = (int)(BlockUpdateThrottling / TicksPerSecond);
            int maxPacketsPerUpdate = (int)(MaxUploadSpeed / TicksPerSecond * 128);

            int playerCount = world.Players.Length;
            if (playerCount > 0 && !world.IsFlushing)
            {
                maxPacketsPerUpdate /= playerCount;
                if (maxPacketsPerUpdate > packetsPerTick)
                {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            }
            else
            {
                maxPacketsPerUpdate = MaxBlockUpdatesPerTick;
            }

            return maxPacketsPerUpdate;
        }


        static readonly Regex RegexIP = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                                                   RegexOptions.Compiled);

        public static bool IsIP([NotNull] string ipString)
        {
            if (ipString == null) throw new ArgumentNullException("ipString");
            return RegexIP.IsMatch(ipString);
        }


        const string DataBackupFileNameFormat = "SpACraftData_{0:yyyyMMdd'_'HH'-'mm'-'ss}.zip";

        public static void BackupData()
        {
            string backupFileName = String.Format(DataBackupFileNameFormat, DateTime.Now); // localized
            using (FileStream fs = File.Create(backupFileName))
            {
                string fileComment = String.Format("Backup of SpACraft data for server \"{0}\", saved on {1}",
                                                    ConfigKey.ServerName.GetString(),
                                                    DateTime.Now);
                using (ZipStorer backupZip = ZipStorer.Create(fs, fileComment))
                {
                    foreach (string dataFileName in Paths.DataFilesToBackup)
                    {
                        if (File.Exists(dataFileName))
                        {
                            backupZip.AddFile(ZipStorer.Compression.Deflate,
                                               dataFileName,
                                               dataFileName,
                                               "");
                        }
                    }
                }
            }
            Logger.Log(LogType.SystemActivity,
                        "Backed up server data to \"{0}\"",
                        backupFileName);
        }


        public static string ReplaceTextKeywords([NotNull] Player player, [NotNull] string input)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (input == null) throw new ArgumentNullException("input");
            StringBuilder sb = new StringBuilder(input);
            sb.Replace("{SERVER_NAME}", ConfigKey.ServerName.GetString());
            sb.Replace("{RANK}", player.Info.Rank.ClassyName);
            sb.Replace("{PLAYER_NAME}", player.ClassyName);
            sb.Replace("{TIME}", DateTime.Now.ToShortTimeString()); // localized
            if (player.World == null)
            {
                sb.Replace("{WORLD}", "(No World)");
            }
            else
            {
                sb.Replace("{WORLD}", player.World.ClassyName);
            }
            sb.Replace("{PLAYERS}", CountVisiblePlayers(player).ToString());
            sb.Replace("{WORLDS}", WorldManager.Worlds.Length.ToString());
            sb.Replace("{MOTD}", ConfigKey.MOTD.GetString());
            sb.Replace("{VERSION}", Updater.CurrentRelease.VersionString);
            return sb.ToString();
        }



        public static string GetRandomString(int chars)
        {
            RandomNumberGenerator prng = RandomNumberGenerator.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while (sb.Length < chars)
            {
                prng.GetBytes(oneChar);
                if (oneChar[0] >= 48 && oneChar[0] <= 57 ||
                    oneChar[0] >= 65 && oneChar[0] <= 90 ||
                    oneChar[0] >= 97 && oneChar[0] <= 122)
                {
                    //if( oneChar[0] >= 33 && oneChar[0] <= 126 ) {
                    sb.Append((char)oneChar[0]);
                }
            }
            return sb.ToString();
        }

        static readonly Uri IPCheckUri = new Uri("http://checkip.dyndns.org/");
        const int IPCheckTimeout = 30000;

        [CanBeNull]
        static IPAddress CheckExternalIP()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(IPCheckUri);
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(BindIPEndPointCallback);
            request.Timeout = IPCheckTimeout;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        string responseString = responseReader.ReadToEnd();
                        int startIndex = responseString.IndexOf(":") + 2;
                        int endIndex = responseString.IndexOf('<', startIndex) - startIndex;
                        IPAddress result;
                        if (IPAddress.TryParse(responseString.Substring(startIndex, endIndex), out result))
                        {
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                Logger.Log(LogType.Warning,
                            "Could not check external IP: {0}", ex);
                return null;
            }
        }

        public static IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
        {
            return new IPEndPoint(InternalIP, 0);
        }

        #endregion


        #region Player and Session Management

        static readonly SortedDictionary<string, Player> PlayerIndex = new SortedDictionary<string, Player>();
        public static Player[] Players { get; private set; }
        static readonly object PlayerListLock = new object();

        static readonly List<Player> Sessions = new List<Player>();
        static readonly object SessionLock = new object();


        internal static bool RegisterSession([NotNull] Player session)
        {
            if (session == null) throw new ArgumentNullException("session");
            int maxSessions = ConfigKey.MaxConnectionsPerIP.GetInt();
            lock (SessionLock)
            {
                if (!session.IP.Equals(IPAddress.Loopback) && maxSessions > 0)
                {
                    int sessionCount = 0;
                    for (int i = 0; i < Sessions.Count; i++)
                    {
                        Player p = Sessions[i];
                        if (p.IP.Equals(session.IP))
                        {
                            sessionCount++;
                            if (sessionCount >= maxSessions)
                            {
                                return false;
                            }
                        }
                    }
                }
                Sessions.Add(session);
            }
            return true;
        }


        internal static bool RegisterPlayer([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");

            List<Player> sessionsToKick = new List<Player>();
            lock (SessionLock)
            {
                foreach (Player s in Sessions)
                {
                    if (s == player) continue;
                    if (s.Name.Equals(player.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        sessionsToKick.Add(s);
                        Logger.Log(LogType.SuspiciousActivity,
                                    "Server.RegisterPlayer: Player {0} logged in twice. Ghost from {1} was kicked.",
                                    s.Name, s.IP);
                        s.Kick("Connected from elsewhere!", LeaveReason.ClientReconnect);
                    }
                }
            }

            foreach (Player ses in sessionsToKick)
            {
                ses.WaitForDisconnect();
            }

            lock (PlayerListLock)
            {
                if (PlayerIndex.Count >= ConfigKey.MaxPlayers.GetInt() && !player.Info.Rank.ReservedSlot)
                {
                    return false;
                }
                PlayerIndex.Add(player.Name, player);
                player.HasRegistered = true;
            }
            return true;
        }


        public static string MakePlayerConnectedMessage([NotNull] Player player, bool firstTime, [NotNull] World world)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (world == null) throw new ArgumentNullException("world");
            if (firstTime)
            {
                return String.Format("&SPlayer {0}&S connected, joined {1}",
                                      player.ClassyName,
                                      world.ClassyName);
            }
            else
            {
                return String.Format("&SPlayer {0}&S connected again, joined {1}",
                                      player.ClassyName,
                                      world.ClassyName);
            }
        }


        public static void UnregisterPlayer([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");

            lock (PlayerListLock)
            {
                if (!player.HasRegistered) return;
                player.Info.ProcessLogout(player);

                Logger.Log(LogType.UserActivity,
                            "{0} left the server ({1}).", player.Name, player.LeaveReason);
                if (player.HasRegistered && ConfigKey.ShowConnectionMessages.Enabled())
                {
                    Players.CanSee(player).Message("&SPlayer {0}&S left the server.",
                                                      player.ClassyName);
                }

                if (player.World != null)
                {
                    player.World.ReleasePlayer(player);
                }
                PlayerIndex.Remove(player.Name);
                UpdatePlayerList();
            }
        }


        internal static void UnregisterSession([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");
            lock (SessionLock)
            {
                Sessions.Remove(player);
            }
        }


        internal static void UpdatePlayerList()
        {
            lock (PlayerListLock)
            {
                Players = PlayerIndex.Values.Where(p => p.IsOnline)
                                            .OrderBy(player => player.Name)
                                            .ToArray();
                RaiseEvent(PlayerListChanged);
            }
        }


        public static Player[] FindPlayers([NotNull] string name, bool raiseEvent)
        {
            if (name == null) throw new ArgumentNullException("name");
            Player[] tempList = Players;
            List<Player> results = new List<Player>();
            for (int i = 0; i < tempList.Length; i++)
            {
                if (tempList[i] == null) continue;
                if (tempList[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    results.Clear();
                    results.Add(tempList[i]);
                    break;
                }
                else if (tempList[i].Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(tempList[i]);
                }
            }
            if (raiseEvent)
            {
                var h = SearchingForPlayer;
                if (h != null)
                {
                    var e = new SearchingForPlayerEventArgs(null, name, results);
                    h(null, e);
                }
            }
            return results.ToArray();
        }


        public static Player[] FindPlayers([NotNull] Player player, [NotNull] string name, bool raiseEvent)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (name == null) throw new ArgumentNullException("name");
            if (name == "-")
            {
                if (player.LastUsedPlayerName != null)
                {
                    name = player.LastUsedPlayerName;
                }
                else
                {
                    return new Player[0];
                }
            }
            player.LastUsedPlayerName = name;
            List<Player> results = new List<Player>();
            Player[] tempList = Players;
            for (int i = 0; i < tempList.Length; i++)
            {
                if (tempList[i] == null || !player.CanSee(tempList[i])) continue;
                if (tempList[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    results.Clear();
                    results.Add(tempList[i]);
                    break;
                }
                else if (tempList[i].Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(tempList[i]);
                }
            }
            if (raiseEvent)
            {
                var h = SearchingForPlayer;
                if (h != null)
                {
                    var e = new SearchingForPlayerEventArgs(player, name, results);
                    h(null, e);
                }
            }
            if (results.Count == 1)
            {
                player.LastUsedPlayerName = results[0].Name;
            }
            return results.ToArray();
        }


        [CanBeNull]
        public static Player FindPlayerOrPrintMatches([NotNull] Player player, [NotNull] string name, bool includeHidden, bool raiseEvent)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (name == null) throw new ArgumentNullException("name");
            if (name == "-")
            {
                if (player.LastUsedPlayerName != null)
                {
                    name = player.LastUsedPlayerName;
                }
                else
                {
                    player.Message("Cannot repeat player name: you haven't used any names yet.");
                    return null;
                }
            }
            Player[] matches;
            if (includeHidden)
            {
                matches = FindPlayers(name, raiseEvent);
            }
            else
            {
                matches = FindPlayers(player, name, raiseEvent);
            }

            if (matches.Length == 0)
            {
                player.MessageNoPlayer(name);
                return null;

            }
            else if (matches.Length > 1)
            {
                player.MessageManyMatches("player", matches);
                return null;

            }
            else
            {
                player.LastUsedPlayerName = matches[0].Name;
                return matches[0];
            }
        }


        public static int CountPlayers(bool includeHiddenPlayers)
        {
            if (includeHiddenPlayers)
            {
                return Players.Length;
            }
            else
            {
                return Players.Count(player => !player.Info.IsHidden);
            }
        }


        public static int CountVisiblePlayers([NotNull] Player observer)
        {
            if (observer == null) throw new ArgumentNullException("observer");
            return Players.Count(observer.CanSee);
        }

        #endregion
    }


    public sealed class ShutdownParams
    {
        public ShutdownParams(ShutdownReason reason, TimeSpan delay, bool killProcess, bool restart)
        {
            Reason = reason;
            Delay = delay;
            KillProcess = killProcess;
            Restart = restart;
        }

        public ShutdownParams(ShutdownReason reason, TimeSpan delay, bool killProcess,
                               bool restart, [CanBeNull] string customReason, [CanBeNull] Player initiatedBy) :
            this(reason, delay, killProcess, restart)
        {
            customReasonString = customReason;
            InitiatedBy = initiatedBy;
        }

        public ShutdownReason Reason { get; private set; }

        readonly string customReasonString;
        [NotNull]
        public string ReasonString
        {
            get
            {
                return customReasonString ?? Reason.ToString();
            }
        }

        public TimeSpan Delay { get; private set; }

        public bool KillProcess { get; private set; }

        public bool Restart { get; private set; }

        [CanBeNull]
        public Player InitiatedBy { get; private set; }
    }


    public enum ShutdownReason
    {
        Unknown,

        Other,

        FailedToInitialize,

        FailedToStart,

        Restarting,

        Crashed,

        ShuttingDown,

        ProcessClosing
    }
}