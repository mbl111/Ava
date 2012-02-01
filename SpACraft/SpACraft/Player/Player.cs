using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading;
using SpACraft.Drawing;
using SpACraft.Events;
using JetBrains.Annotations;

namespace SpACraft
{
    public delegate void SelectionCallback(Player player, Vector3I[] marks, object tag);

    public delegate void ConfirmationCallback(Player player, object tag, bool fromConsole);


    public sealed partial class Player : IClassy
    {

        public static Player Console, AutoRank;


        #region Properties

        public readonly bool IsSuper;


        public SessionState State { get; private set; }

        public bool HasRegistered { get; internal set; }

        public bool HasFullyConnected { get; private set; }

        public bool IsOnline
        {
            get
            {
                return State == SessionState.Online;
            }
        }

        public bool IsVerified { get; private set; }

        public PlayerInfo Info { get; private set; }

        public bool IsPainting { get; set; }

        public bool IsDeaf { get; set; }


        [CanBeNull]
        public World World { get; private set; }

        [NotNull]
        public Map WorldMap
        {
            get
            {
                World world = World;
                if (world == null) PlayerOpException.ThrowNoWorld(this);
                return world.LoadMap();
            }
        }

        public Position Position;


        public DateTime LoginTime { get; private set; }

        public DateTime LastActiveTime { get; private set; }

        public DateTime LastPatrolTime { get; set; }


        [CanBeNull]
        public Command LastCommand { get; private set; }


        [NotNull]
        public string Name
        {
            get { return Info.Name; }
        }

        [NotNull]
        public string ListName
        {
            get
            {
                string displayedName = Name;
                if (ConfigKey.RankPrefixesInList.Enabled())
                {
                    displayedName = Info.Rank.Prefix + displayedName;
                }
                if (ConfigKey.RankColorsInChat.Enabled() && Info.Rank.Color != Color.White)
                {
                    displayedName = Info.Rank.Color + displayedName;
                }
                if 
                return displayedName;
            }
        }

        [NotNull]
        public string ClassyName
        {
            get { return Info.ClassyName; }
        }

        public bool IsUsingWoM { get; private set; }


        [NotNull]
        public MetadataCollection<object> Metadata { get; private set; }

        #endregion


        internal Player([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            Info = new PlayerInfo(name, RankManager.HighestRank, true, RankChangeType.AutoPromoted);
            spamBlockLog = new Queue<DateTime>(Info.Rank.AntiGriefBlocks);
            IP = IPAddress.Loopback;
            ResetAllBinds();
            State = SessionState.Offline;
            IsSuper = true;
        }


        #region Chat and Messaging

        static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds(60);

        int muteWarnings;
        [CanBeNull]
        string partialMessage;

        public void ParseMessage([NotNull] string rawMessage, bool fromConsole)
        {
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");

            if (rawMessage.Equals("/nvm", StringComparison.OrdinalIgnoreCase))
            {
                if (partialMessage != null)
                {
                    MessageNow("Partial message cancelled.");
                    partialMessage = null;
                }
                else
                {
                    MessageNow("No partial message to cancel.");
                }
                return;
            }

            if (partialMessage != null)
            {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }

            switch (Chat.GetRawMessageType(rawMessage))
            {
                case RawMessageType.Chat:
                    {
                        if (!Can(Permission.Chat)) return;

                        if (Info.IsMuted)
                        {
                            MessageMuted();
                            return;
                        }

                        if (DetectChatSpam()) return;

                        if (rawMessage.StartsWith("//"))
                        {
                            rawMessage = rawMessage.Substring(1);
                        }

                        if (rawMessage.EndsWith("//"))
                        {
                            rawMessage = rawMessage.Substring(0, rawMessage.Length - 1);
                        }

                        if (Can(Permission.UseColorCodes) && rawMessage.Contains("%"))
                        {
                            rawMessage = Color.ReplacePercentCodes(rawMessage);
                        }

                        Chat.SendGlobal(this, rawMessage);
                    } break;


                case RawMessageType.Command:
                    {
                        if (rawMessage.EndsWith("//"))
                        {
                            rawMessage = rawMessage.Substring(0, rawMessage.Length - 1);
                        }
                        Command cmd = new Command(rawMessage);
                        CommandDescriptor commandDescriptor = CommandManager.GetDescriptor(cmd.Name, true);

                        if (commandDescriptor == null)
                        {
                            MessageNow("Unknown command \"{0}\". See &H/Commands", cmd.Name);
                        }
                        else if (Info.IsFrozen && !commandDescriptor.UsableByFrozenPlayers)
                        {
                            MessageNow("&WYou cannot use this command while frozen.");
                        }
                        else
                        {
                            if (!commandDescriptor.DisableLogging)
                            {
                                Logger.Log(LogType.UserCommand,
                                            "{0}: {1}", Name, rawMessage);
                            }
                            if (commandDescriptor.RepeatableSelection)
                            {
                                selectionRepeatCommand = cmd;
                            }
                            SendToSpectators(cmd.RawMessage);
                            CommandManager.ParseCommand(this, cmd, fromConsole);
                            if (!commandDescriptor.NotRepeatable)
                            {
                                LastCommand = cmd;
                            }
                        }
                    } break;


                case RawMessageType.RepeatCommand:
                    {
                        if (LastCommand == null)
                        {
                            Message("No command to repeat.");
                        }
                        else
                        {
                            if (Info.IsFrozen && !LastCommand.Descriptor.UsableByFrozenPlayers)
                            {
                                MessageNow("&WYou cannot use this command while frozen.");
                                return;
                            }
                            LastCommand.Rewind();
                            Logger.Log(LogType.UserCommand,
                                        "{0} repeated: {1}",
                                        Name, LastCommand.RawMessage);
                            Message("Repeat: {0}", LastCommand.RawMessage);
                            SendToSpectators(LastCommand.RawMessage);
                            CommandManager.ParseCommand(this, LastCommand, fromConsole);
                        }
                    } break;


                case RawMessageType.PrivateChat:
                    {
                        if (!Can(Permission.Chat)) return;

                        if (Info.IsMuted)
                        {
                            MessageMuted();
                            return;
                        }

                        if (DetectChatSpam()) return;

                        if (rawMessage.EndsWith("//"))
                        {
                            rawMessage = rawMessage.Substring(0, rawMessage.Length - 1);
                        }

                        string otherPlayerName, messageText;
                        if (rawMessage[1] == ' ')
                        {
                            otherPlayerName = rawMessage.Substring(2, rawMessage.IndexOf(' ', 2) - 2);
                            messageText = rawMessage.Substring(rawMessage.IndexOf(' ', 2) + 1);
                        }
                        else
                        {
                            otherPlayerName = rawMessage.Substring(1, rawMessage.IndexOf(' ') - 1);
                            messageText = rawMessage.Substring(rawMessage.IndexOf(' ') + 1);
                        }

                        if (messageText.Contains("%") && Can(Permission.UseColorCodes))
                        {
                            messageText = Color.ReplacePercentCodes(messageText);
                        }

                        if (otherPlayerName == "-")
                        {
                            if (LastUsedPlayerName != null)
                            {
                                otherPlayerName = LastUsedPlayerName;
                            }
                            else
                            {
                                Message("Cannot repeat player name: you haven't used any names yet.");
                                return;
                            }
                        }

                        Player[] allPlayers = Server.FindPlayers(otherPlayerName, true);

                        if (allPlayers.Length > 1)
                        {
                            allPlayers = Server.FindPlayers(this, otherPlayerName, true);
                        }

                        if (allPlayers.Length == 1)
                        {
                            Player target = allPlayers[0];
                            if (target == this)
                            {
                                MessageNow("Trying to talk to yourself?");
                                return;
                            }
                            if (!target.IsIgnoring(Info) && !target.IsDeaf)
                            {
                                Chat.SendPM(this, target, messageText);
                                SendToSpectators("to {0}&F: {1}", target.ClassyName, messageText);
                            }

                            if (!CanSee(target))
                            {
                                MessageNoPlayer(otherPlayerName);

                            }
                            else
                            {
                                LastUsedPlayerName = target.Name;
                                if (target.IsIgnoring(Info))
                                {
                                    if (CanSee(target))
                                    {
                                        MessageNow("&WCannot PM {0}&W: you are ignored.", target.ClassyName);
                                    }
                                }
                                else if (target.IsDeaf)
                                {
                                    MessageNow("&SCannot PM {0}&S: they are currently deaf.", target.ClassyName);
                                }
                                else
                                {
                                    MessageNow("&Pto {0}: {1}",
                                                target.Name, messageText);
                                }
                            }

                        }
                        else if (allPlayers.Length == 0)
                        {
                            MessageNoPlayer(otherPlayerName);

                        }
                        else
                        {
                            MessageManyMatches("player", allPlayers);
                        }
                    } break;


                case RawMessageType.RankChat:
                    {
                        if (!Can(Permission.Chat)) return;

                        if (Info.IsMuted)
                        {
                            MessageMuted();
                            return;
                        }

                        if (DetectChatSpam()) return;

                        if (rawMessage.EndsWith("//"))
                        {
                            rawMessage = rawMessage.Substring(0, rawMessage.Length - 1);
                        }

                        Rank rank;
                        if (rawMessage[2] == ' ')
                        {
                            rank = Info.Rank;
                        }
                        else
                        {
                            string rankName = rawMessage.Substring(2, rawMessage.IndexOf(' ') - 2);
                            rank = RankManager.FindRank(rankName);
                            if (rank == null)
                            {
                                MessageNoRank(rankName);
                                break;
                            }
                        }

                        string messageText = rawMessage.Substring(rawMessage.IndexOf(' ') + 1);
                        if (messageText.Contains("%") && Can(Permission.UseColorCodes))
                        {
                            messageText = Color.ReplacePercentCodes(messageText);
                        }

                        Player[] spectators = Server.Players.NotRanked(Info.Rank)
                                                            .Where(p => p.spectatedPlayer == this)
                                                            .ToArray();
                        if (spectators.Length > 0)
                        {
                            spectators.Message("[Spectate]: &Fto rank {0}&F: {1}", rank.ClassyName, messageText);
                        }

                        Chat.SendRank(this, rank, messageText);
                    } break;


                case RawMessageType.Confirmation:
                    {
                        if (Info.IsFrozen)
                        {
                            MessageNow("&WYou cannot use any commands while frozen.");
                            return;
                        }
                        if (ConfirmCallback != null)
                        {
                            if (DateTime.UtcNow.Subtract(ConfirmRequestTime) < ConfirmationTimeout)
                            {
                                SendToSpectators("/ok");
                                ConfirmCallback(this, ConfirmArgument, fromConsole);
                                ConfirmCallback = null;
                                ConfirmArgument = null;
                            }
                            else
                            {
                                MessageNow("Confirmation timed out. Enter the command again.");
                            }
                        }
                        else
                        {
                            MessageNow("There is no command to confirm.");
                        }
                    } break;


                case RawMessageType.PartialMessage:
                    partialMessage = rawMessage.Substring(0, rawMessage.Length - 1);
                    MessageNow("Partial: &F{0}", partialMessage);
                    break;

                case RawMessageType.Invalid:
                    MessageNow("Could not parse message.");
                    break;
            }
        }


        public void SendToSpectators([NotNull] string message, [NotNull] params object[] args)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            Player[] spectators = Server.Players.Where(p => p.spectatedPlayer == this).ToArray();
            if (spectators.Length > 0)
            {
                spectators.Message("[Spectate]: &F" + message, args);
            }
        }


        const string WoMAlertPrefix = "^detail.user.alert=";
        public void MessageAlt([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (this == Console)
            {
                Logger.LogToConsole(message);
            }
            else if (IsUsingWoM)
            {
                foreach (Packet p in LineWrapper.WrapPrefixed(WoMAlertPrefix, WoMAlertPrefix + Color.Sys + message))
                {
                    Send(p);
                }
            }
            else
            {
                foreach (Packet p in LineWrapper.Wrap(Color.Sys + message))
                {
                    Send(p);
                }
            }
        }

        [StringFormatMethod("message")]
        public void MessageAlt([NotNull] string message, [NotNull] params object[] args)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            MessageAlt(String.Format(message, args));
        }


        public void Message([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (IsSuper)
            {
                Logger.LogToConsole(message);
            }
            else
            {
                foreach (Packet p in LineWrapper.Wrap(Color.Sys + message))
                {
                    Send(p);
                }
            }
        }


        [StringFormatMethod("message")]
        public void Message([NotNull] string message, [NotNull] object arg)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (arg == null) throw new ArgumentNullException("arg");
            Message(String.Format(message, arg));
        }

        [StringFormatMethod("message")]
        public void Message([NotNull] string message, [NotNull] params object[] args)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            Message(String.Format(message, args));
        }


        [StringFormatMethod("message")]
        public void MessagePrefixed([NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args)
        {
            if (prefix == null) throw new ArgumentNullException("prefix");
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            if (args.Length > 0)
            {
                message = String.Format(message, args);
            }
            if (this == Console)
            {
                Logger.LogToConsole(message);
            }
            else
            {
                foreach (Packet p in LineWrapper.WrapPrefixed(prefix, message))
                {
                    Send(p);
                }
            }
        }


        [StringFormatMethod("message")]
        internal void MessageNow([NotNull] string message, [NotNull] params object[] args)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            if (IsDeaf) return;
            if (args.Length > 0)
            {
                message = String.Format(message, args);
            }
            if (this == Console)
            {
                Logger.LogToConsole(message);
            }
            else
            {
                if (Thread.CurrentThread != ioThread)
                {
                    throw new InvalidOperationException("SendNow may only be called from player's own thread.");
                }
                foreach (Packet p in LineWrapper.Wrap(Color.Sys + message))
                {
                    SendNow(p);
                }
            }
        }


        [StringFormatMethod("message")]
        internal void MessageNowPrefixed([NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args)
        {
            if (prefix == null) throw new ArgumentNullException("prefix");
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            if (IsDeaf) return;
            if (args.Length > 0)
            {
                message = String.Format(message, args);
            }
            if (this == Console)
            {
                Logger.LogToConsole(message);
            }
            else
            {
                if (Thread.CurrentThread != ioThread)
                {
                    throw new InvalidOperationException("SendNow may only be called from player's own thread.");
                }
                foreach (Packet p in LineWrapper.WrapPrefixed(prefix, message))
                {
                    Send(p);
                }
            }
        }


        #region Macros

        public void MessageNoPlayer([NotNull] string playerName)
        {
            if (playerName == null) throw new ArgumentNullException("playerName");
            Message("No players found matching \"{0}\"", playerName);
        }


        public void MessageNoWorld([NotNull] string worldName)
        {
            if (worldName == null) throw new ArgumentNullException("worldName");
            Message("No worlds found matching \"{0}\". See &H/Worlds", worldName);
        }


        public void MessageManyMatches([NotNull] string itemType, [NotNull] IEnumerable<IClassy> names)
        {
            if (itemType == null) throw new ArgumentNullException("itemType");
            if (names == null) throw new ArgumentNullException("names");

            string nameList = names.JoinToString(", ", p => p.ClassyName);
            Message("More than one {0} matched: {1}",
                     itemType, nameList);
        }


        public void MessageNoAccess([NotNull] params Permission[] permissions)
        {
            if (permissions == null) throw new ArgumentNullException("permissions");
            Rank reqRank = RankManager.GetMinRankWithAllPermissions(permissions);
            if (reqRank == null)
            {
                Message("None of the ranks have permissions for this command.");
            }
            else
            {
                Message("This command requires {0}+&S rank.",
                         reqRank.ClassyName);
            }
        }


        public void MessageNoAccess([NotNull] CommandDescriptor cmd)
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
            Rank reqRank = cmd.MinRank;
            if (reqRank == null)
            {
                Message("This command is disabled on the server.");
            }
            else
            {
                Message("This command requires {0}+&S rank.",
                         reqRank.ClassyName);
            }
        }


        public void MessageNoRank([NotNull] string rankName)
        {
            if (rankName == null) throw new ArgumentNullException("rankName");
            Message("Unrecognized rank \"{0}\". See &H/Ranks", rankName);
        }


        public void MessageUnsafePath()
        {
            Message("&WYou cannot access files outside the map folder.");
        }


        public void MessageNoZone([NotNull] string zoneName)
        {
            if (zoneName == null) throw new ArgumentNullException("zoneName");
            Message("No zones found matching \"{0}\". See &H/Zones", zoneName);
        }


        public void MessageInvalidWorldName([NotNull] string worldName)
        {
            Message("Unacceptible world name: \"{0}\"", worldName);
            Message("World names must be 1-16 characters long, and only contain letters, numbers, and underscores.");
        }


        public void MessageInvalidPlayerName([NotNull] string playerName)
        {
            Message("\"{0}\" is not a valid player name.", playerName);
        }


        public void MessageMuted()
        {
            Message("You are muted for {0} longer.",
                     Info.TimeMutedLeft.ToMiniString());
        }


        public void MessageMaxTimeSpan()
        {
            Message("Specify a time range up to {0:0}d.", DateTimeUtil.MaxTimeSpan.TotalDays);
        }

        #endregion


        #region Ignore

        readonly HashSet<PlayerInfo> ignoreList = new HashSet<PlayerInfo>();
        readonly object ignoreLock = new object();


        public bool IsIgnoring([NotNull] PlayerInfo other)
        {
            if (other == null) throw new ArgumentNullException("other");
            lock (ignoreLock)
            {
                return ignoreList.Contains(other);
            }
        }


        public bool Ignore([NotNull] PlayerInfo other)
        {
            if (other == null) throw new ArgumentNullException("other");
            lock (ignoreLock)
            {
                if (!ignoreList.Contains(other))
                {
                    ignoreList.Add(other);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public bool Unignore([NotNull] PlayerInfo other)
        {
            if (other == null) throw new ArgumentNullException("other");
            lock (ignoreLock)
            {
                return ignoreList.Remove(other);
            }
        }


        [NotNull]
        public PlayerInfo[] IgnoreList
        {
            get
            {
                lock (ignoreLock)
                {
                    return ignoreList.ToArray();
                }
            }
        }

        #endregion


        #region Confirmation

        [CanBeNull]
        public ConfirmationCallback ConfirmCallback { get; private set; }

        [CanBeNull]
        public object ConfirmArgument { get; private set; }

        static void ConfirmCommandCallback([NotNull] Player player, object tag, bool fromConsole)
        {
            if (player == null) throw new ArgumentNullException("player");
            Command cmd = (Command)tag;
            cmd.Rewind();
            cmd.IsConfirmed = true;
            CommandManager.ParseCommand(player, cmd, fromConsole);
        }

        public DateTime ConfirmRequestTime { get; private set; }

        [StringFormatMethod("message")]
        public void Confirm([NotNull] Command cmd, [NotNull] string message, [NotNull] params object[] args)
        {
            Confirm(ConfirmCommandCallback, cmd, message, args);
        }

        [StringFormatMethod("message")]
        public void Confirm([NotNull] ConfirmationCallback callback, [CanBeNull] object arg, [NotNull] string message, [NotNull] params object[] args)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            ConfirmCallback = callback;
            ConfirmArgument = arg;
            ConfirmRequestTime = DateTime.UtcNow;
            Message("{0} Type &H/ok&S to continue.", String.Format(message, args));
        }

        #endregion


        #region AntiSpam

        public static int AntispamMessageCount = 3;
        public static int AntispamInterval = 4;
        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>(AntispamMessageCount);

        internal bool DetectChatSpam()
        {
            if (IsSuper) return false;
            if (spamChatLog.Count >= AntispamMessageCount)
            {
                DateTime oldestTime = spamChatLog.Dequeue();
                if (DateTime.UtcNow.Subtract(oldestTime).TotalSeconds < AntispamInterval)
                {
                    muteWarnings++;
                    if (muteWarnings > ConfigKey.AntispamMaxWarnings.GetInt())
                    {
                        KickNow("You were kicked for repeated spamming.", LeaveReason.MessageSpamKick);
                        Server.Message("&W{0} was kicked for repeated spamming.", ClassyName);
                    }
                    else
                    {
                        TimeSpan autoMuteDuration = TimeSpan.FromSeconds(ConfigKey.AntispamMuteDuration.GetInt());
                        Info.Mute(Console, autoMuteDuration, false, true);
                        Message("You have been muted for {0} seconds. Slow down.", autoMuteDuration);
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue(DateTime.UtcNow);
            return false;
        }

        #endregion

        #endregion


        #region Placing Blocks

        readonly Queue<DateTime> spamBlockLog = new Queue<DateTime>();

        public Block LastUsedBlockType { get; private set; }

        public static int MaxBlockPlacementRange { get; set; }


        public bool PlaceBlock(Vector3I coord, ClickAction action, Block type)
        {
            if (World == null) PlayerOpException.ThrowNoWorld(this);
            Map map = WorldMap;
            LastUsedBlockType = type;

            Vector3I coordBelow = new Vector3I(coord.X, coord.Y, coord.Z - 1);

            if (Info.IsFrozen ||
                Math.Abs(coord.X * 32 - Position.X) > MaxBlockPlacementRange ||
                Math.Abs(coord.Y * 32 - Position.Y) > MaxBlockPlacementRange ||
                Math.Abs(coord.Z * 32 - Position.Z) > MaxBlockPlacementRange)
            {
                RevertBlockNow(coord);
                return false;
            }

            if (IsSpectating)
            {
                Message("You cannot build or delete while spectating.");
                RevertBlockNow(coord);
                return false;
            }

            if (World.IsLocked)
            {
                RevertBlockNow(coord);
                Message("This map is currently locked (read-only).");
                return false;
            }

            if (CheckBlockSpam()) return true;

            BlockChangeContext context = BlockChangeContext.Manual;
            if (IsPainting && action == ClickAction.Delete)
            {
                context = BlockChangeContext.Replaced;
            }

            bool requiresUpdate = (type != bindings[(byte)type] || IsPainting);
            if (action == ClickAction.Delete && !IsPainting)
            {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            if (SelectionMarksExpected > 0)
            {
                RevertBlockNow(coord);
                SelectionAddMark(coord, true);
                return false;
            }

            CanPlaceResult canPlaceResult;
            if (type == Block.Stair && coord.Z > 0 && map.GetBlock(coordBelow) == Block.Stair)
            {
                canPlaceResult = CanPlace(map, coordBelow, Block.DoubleStair, context);
            }
            else
            {
                canPlaceResult = CanPlace(map, coord, type, context);
            }

            switch (canPlaceResult)
            {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if (type == Block.Stair && coord.Z > 0 && map.GetBlock(coordBelow) == Block.Stair)
                    {

                        blockUpdate = new BlockUpdate(this, coordBelow, Block.DoubleStair);
                        Info.ProcessBlockPlaced((byte)Block.DoubleStair);
                        map.QueueUpdate(blockUpdate);
                        RaisePlayerPlacedBlockEvent(this, World.Map, coordBelow, Block.Stair, Block.DoubleStair, context);
                        SendNow(PacketWriter.MakeSetBlock(coordBelow, Block.DoubleStair));
                        RevertBlockNow(coord);
                        break;

                    }
                    else
                    {
                        blockUpdate = new BlockUpdate(this, coord, type);
                        Info.ProcessBlockPlaced((byte)type);
                        Block old = map.GetBlock(coord);
                        map.QueueUpdate(blockUpdate);
                        RaisePlayerPlacedBlockEvent(this, World.Map, coord, old, type, context);
                        if (requiresUpdate || RelayAllUpdates)
                        {
                            SendNow(PacketWriter.MakeSetBlock(coord, type));
                        }
                    }
                    break;

                case CanPlaceResult.BlocktypeDenied:
                    Message("&WYou are not permitted to affect this block type.");
                    RevertBlockNow(coord);
                    break;

                case CanPlaceResult.RankDenied:
                    Message("&WYour rank is not allowed to build.");
                    RevertBlockNow(coord);
                    break;

                case CanPlaceResult.WorldDenied:
                    switch (World.BuildSecurity.CheckDetailed(Info))
                    {
                        case SecurityCheckResult.RankTooLow:
                        case SecurityCheckResult.RankTooHigh:
                            Message("&WYour rank is not allowed to build in this world.");
                            break;
                        case SecurityCheckResult.BlackListed:
                            Message("&WYou are not allowed to build in this world.");
                            break;
                    }
                    RevertBlockNow(coord);
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = WorldMap.Zones.FindDenied(coord, this);
                    if (deniedZone != null)
                    {
                        Message("&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name);
                    }
                    else
                    {
                        Message("&WYou are not allowed to build here.");
                    }
                    RevertBlockNow(coord);
                    break;

                case CanPlaceResult.PluginDenied:
                    RevertBlockNow(coord);
                    break;

            }
            return false;
        }


        public void RevertBlock(Vector3I coords)
        {
            SendLowPriority(PacketWriter.MakeSetBlock(coords, WorldMap.GetBlock(coords)));
        }


        void RevertBlockNow(Vector3I coords)
        {
            SendNow(PacketWriter.MakeSetBlock(coords, WorldMap.GetBlock(coords)));
        }


        bool CheckBlockSpam()
        {
            if (Info.Rank.AntiGriefBlocks == 0 || Info.Rank.AntiGriefSeconds == 0) return false;
            if (spamBlockLog.Count >= Info.Rank.AntiGriefBlocks)
            {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract(oldestTime).TotalSeconds;
                if (spamTimer < Info.Rank.AntiGriefSeconds)
                {
                    KickNow("You were kicked by antigrief system. Slow down.", LeaveReason.BlockSpamKick);
                    Server.Message("{0}&W was kicked for suspected griefing.", ClassyName);
                    Logger.Log(LogType.SuspiciousActivity,
                                "{0} was kicked for block spam ({1} blocks in {2} seconds)",
                                Name, Info.Rank.AntiGriefBlocks, spamTimer);
                    return true;
                }
            }
            spamBlockLog.Enqueue(DateTime.UtcNow);
            return false;
        }

        #endregion


        #region Binding

        readonly Block[] bindings = new Block[50];

        public void Bind(Block type, Block replacement)
        {
            bindings[(byte)type] = replacement;
        }

        public void ResetBind(Block type)
        {
            bindings[(byte)type] = type;
        }

        public void ResetBind([NotNull] params Block[] types)
        {
            if (types == null) throw new ArgumentNullException("types");
            foreach (Block type in types)
            {
                ResetBind(type);
            }
        }

        public Block GetBind(Block type)
        {
            return bindings[(byte)type];
        }

        public void ResetAllBinds()
        {
            foreach (Block block in Enum.GetValues(typeof(Block)))
            {
                if (block != Block.Undefined)
                {
                    ResetBind(block);
                }
            }
        }

        #endregion


        #region Permission Checks

        public bool Can([NotNull] params Permission[] permissions)
        {
            if (permissions == null) throw new ArgumentNullException("permissions");
            return IsSuper || permissions.All(Info.Rank.Can);
        }


        public bool CanAny([NotNull] params Permission[] permissions)
        {
            if (permissions == null) throw new ArgumentNullException("permissions");
            return IsSuper || permissions.Any(Info.Rank.Can);
        }


        public bool Can(Permission permission)
        {
            return IsSuper || Info.Rank.Can(permission);
        }


        public bool Can(Permission permission, [NotNull] Rank other)
        {
            if (other == null) throw new ArgumentNullException("other");
            return IsSuper || Info.Rank.Can(permission, other);
        }


        public bool CanDraw(int volume)
        {
            if (volume < 0) throw new ArgumentOutOfRangeException("volume");
            return IsSuper || (Info.Rank.DrawLimit == 0) || (volume <= Info.Rank.DrawLimit);
        }


        public bool CanJoin([NotNull] World worldToJoin)
        {
            if (worldToJoin == null) throw new ArgumentNullException("worldToJoin");
            return IsSuper || worldToJoin.AccessSecurity.Check(Info);
        }


        public CanPlaceResult CanPlace([NotNull] Map map, Vector3I coords, Block newBlock, BlockChangeContext context)
        {
            if (map == null) throw new ArgumentNullException("map");
            CanPlaceResult result;

            Block oldBlock = map.GetBlock(coords);
            if (oldBlock == Block.Undefined)
            {
                result = CanPlaceResult.OutOfBounds;
                goto eventCheck;
            }

            if (newBlock == Block.Admincrete && !Can(Permission.PlaceAdmincrete))
            {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }
            else if ((newBlock == Block.Water || newBlock == Block.StillWater) && !Can(Permission.PlaceWater))
            {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }
            else if ((newBlock == Block.Lava || newBlock == Block.StillLava) && !Can(Permission.PlaceLava))
            {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            if (oldBlock == Block.Admincrete && !Can(Permission.DeleteAdmincrete))
            {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            PermissionOverride zoneCheckResult = map.Zones.Check(coords, this);
            if (zoneCheckResult == PermissionOverride.Allow)
            {
                result = CanPlaceResult.Allowed;
                goto eventCheck;
            }
            else if (zoneCheckResult == PermissionOverride.Deny)
            {
                result = CanPlaceResult.ZoneDenied;
                goto eventCheck;
            }

            World mapWorld = map.World;
            if (mapWorld != null)
            {
                switch (mapWorld.BuildSecurity.CheckDetailed(Info))
                {
                    case SecurityCheckResult.Allowed:
                        if ((Can(Permission.Build) || newBlock == Block.Air) &&
                            (Can(Permission.Delete) || oldBlock == Block.Air))
                        {
                            result = CanPlaceResult.Allowed;
                        }
                        else
                        {
                            result = CanPlaceResult.RankDenied;
                        }
                        break;

                    case SecurityCheckResult.WhiteListed:
                        result = CanPlaceResult.Allowed;
                        break;

                    default:
                        result = CanPlaceResult.WorldDenied;
                        break;
                }
            }
            else
            {
                result = CanPlaceResult.Allowed;
            }

        eventCheck:
            var handler = PlacingBlock;
            if (handler == null) return result;

            var e = new PlayerPlacingBlockEventArgs(this, map, coords, oldBlock, newBlock, context, result);
            handler(null, e);
            return e.Result;
        }


        public bool CanSee([NotNull] Player other)
        {
            if (other == null) throw new ArgumentNullException("other");
            return other == this ||
                   IsSuper ||
                   !other.Info.IsHidden ||
                   Info.Rank.CanSee(other.Info.Rank);
        }


        public bool CanSeeMoving([NotNull] Player other)
        {
            if (other == null) throw new ArgumentNullException("other");
            return other == this ||
                   IsSuper ||
                   other.spectatedPlayer == null && !other.Info.IsHidden ||
                   (other.spectatedPlayer != this && Info.Rank.CanSee(other.Info.Rank));
        }


        public bool CanSee([NotNull] World world)
        {
            if (world == null) throw new ArgumentNullException("world");
            return CanJoin(world) && !world.IsHidden;
        }

        #endregion


        #region Undo / Redo

        readonly LinkedList<UndoState> undoStack = new LinkedList<UndoState>();
        readonly LinkedList<UndoState> redoStack = new LinkedList<UndoState>();

        internal UndoState RedoPop()
        {
            if (redoStack.Count > 0)
            {
                var lastNode = redoStack.Last;
                redoStack.RemoveLast();
                return lastNode.Value;
            }
            else
            {
                return null;
            }
        }

        internal UndoState RedoBegin(DrawOperation op)
        {
            LastDrawOp = op;
            UndoState newState = new UndoState(op);
            undoStack.AddLast(newState);
            return newState;
        }

        internal UndoState UndoBegin(DrawOperation op)
        {
            LastDrawOp = op;
            UndoState newState = new UndoState(op);
            redoStack.AddLast(newState);
            return newState;
        }

        public UndoState UndoPop()
        {
            if (undoStack.Count > 0)
            {
                var lastNode = undoStack.Last;
                undoStack.RemoveLast();
                return lastNode.Value;
            }
            else
            {
                return null;
            }
        }

        public UndoState DrawBegin(DrawOperation op)
        {
            LastDrawOp = op;
            UndoState newState = new UndoState(op);
            undoStack.AddLast(newState);
            if (undoStack.Count > ConfigKey.MaxUndoStates.GetInt())
            {
                undoStack.RemoveFirst();
            }
            redoStack.Clear();
            return newState;
        }

        public void UndoClear()
        {
            undoStack.Clear();
        }

        public void RedoClear()
        {
            redoStack.Clear();
        }

        #endregion


        #region Drawing, Selection

        [NotNull]
        public IBrush Brush { get; set; }

        [CanBeNull]
        public DrawOperation LastDrawOp { get; set; }


        public bool IsMakingSelection
        {
            get { return SelectionMarksExpected > 0; }
        }

        public int SelectionMarkCount
        {
            get { return selectionMarks.Count; }
        }

        public int SelectionMarksExpected { get; private set; }

        public bool IsRepeatingSelection { get; set; }

        [CanBeNull]
        Command selectionRepeatCommand;

        [CanBeNull]
        SelectionCallback selectionCallback;

        readonly Queue<Vector3I> selectionMarks = new Queue<Vector3I>();

        [CanBeNull]
        object selectionArgs;

        [CanBeNull]
        Permission[] selectionPermissions;


        public void SelectionAddMark(Vector3I pos, bool executeCallbackIfNeeded)
        {
            if (!IsMakingSelection) throw new InvalidOperationException("No selection in progress.");
            selectionMarks.Enqueue(pos);
            if (SelectionMarkCount >= SelectionMarksExpected)
            {
                if (executeCallbackIfNeeded)
                {
                    SelectionExecute();
                }
                else
                {
                    Message("Last block marked at {0}. Type &H/Mark&S or click any block to continue.", pos);
                }
            }
            else
            {
                Message("Block #{0} marked at {1}. Place mark #{2}.",
                         SelectionMarkCount, pos, SelectionMarkCount + 1);
            }
        }


        public void SelectionExecute()
        {
            if (!IsMakingSelection || selectionCallback == null)
            {
                throw new InvalidOperationException("No selection in progress.");
            }
            SelectionMarksExpected = 0;
            if (selectionPermissions == null || Can(selectionPermissions))
            {
                selectionCallback(this, selectionMarks.ToArray(), selectionArgs);
                if (IsRepeatingSelection && selectionRepeatCommand != null)
                {
                    selectionRepeatCommand.Rewind();
                    CommandManager.ParseCommand(this, selectionRepeatCommand, this == Console);
                }
                selectionMarks.Clear();
            }
            else
            {
                Message("&WYou are no longer allowed to complete this action.");
                MessageNoAccess(selectionPermissions);
            }
        }


        public void SelectionStart(int marksExpected,
                                    [NotNull] SelectionCallback callback,
                                    [CanBeNull] object args,
                                    [CanBeNull] params Permission[] requiredPermissions)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            selectionArgs = args;
            SelectionMarksExpected = marksExpected;
            selectionMarks.Clear();
            selectionCallback = callback;
            selectionPermissions = requiredPermissions;
        }


        public void SelectionResetMarks()
        {
            selectionMarks.Clear();
        }


        public void SelectionCancel()
        {
            selectionMarks.Clear();
            SelectionMarksExpected = 0;
            selectionCallback = null;
            selectionArgs = null;
            selectionPermissions = null;
        }

        #endregion


        #region Copy/Paste

        CopyState[] copyInformation;
        public CopyState[] CopyInformation
        {
            get { return copyInformation; }
        }

        int copySlot;
        public int CopySlot
        {
            get { return copySlot; }
            set
            {
                if (value < 0 || value > Info.Rank.CopySlots)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                copySlot = value;
            }
        }

        internal void InitCopySlots()
        {
            Array.Resize(ref copyInformation, Info.Rank.CopySlots);
            CopySlot = Math.Min(CopySlot, Info.Rank.CopySlots - 1);
        }

        [CanBeNull]
        public CopyState GetCopyInformation()
        {
            return CopyInformation[copySlot];
        }

        public void SetCopyInformation([CanBeNull] CopyState info)
        {
            if (info != null) info.Slot = copySlot;
            CopyInformation[copySlot] = info;
        }

        #endregion


        #region Spectating

        [CanBeNull]
        Player spectatedPlayer;

        [CanBeNull]
        public Player SpectatedPlayer
        {
            get { return spectatedPlayer; }
        }

        [CanBeNull]
        public PlayerInfo LastSpectatedPlayer { get; private set; }

        readonly object spectateLock = new object();

        public bool IsSpectating
        {
            get { return (spectatedPlayer != null); }
        }


        public bool Spectate([NotNull] Player target)
        {
            if (target == null) throw new ArgumentNullException("target");
            lock (spectateLock)
            {
                if (target == this)
                {
                    PlayerOpException.ThrowCannotTargetSelf(this, Info, "spectate");
                }

                if (!Can(Permission.Spectate, target.Info.Rank))
                {
                    PlayerOpException.ThrowPermissionLimit(this, target.Info, "spectate", Permission.Spectate);
                }

                if (spectatedPlayer == target) return false;

                spectatedPlayer = target;
                LastSpectatedPlayer = target.Info;
                Message("Now spectating {0}&S. Type &H/unspec&S to stop.", target.ClassyName);
                return true;
            }
        }


        public bool StopSpectating()
        {
            lock (spectateLock)
            {
                if (spectatedPlayer == null) return false;
                Message("Stopped spectating {0}", spectatedPlayer.ClassyName);
                spectatedPlayer = null;
                return true;
            }
        }

        #endregion


        #region Static Utilities

        static readonly Uri PaidCheckUri = new Uri("http://www.minecraft.net/haspaid.jsp?user=");
        const int PaidCheckTimeout = 5000;


        public static bool CheckPaidStatus([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(PaidCheckUri + Uri.EscapeDataString(name));
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(Server.BindIPEndPointCallback);
            request.Timeout = PaidCheckTimeout;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        string paidStatusString = responseReader.ReadToEnd();
                        bool isPaid;
                        return Boolean.TryParse(paidStatusString, out isPaid) && isPaid;
                    }
                }
            }
            catch (WebException ex)
            {
                Logger.Log(LogType.Warning,
                            "Could not check paid status of player {0}: {1}",
                            name, ex.Message);
                return false;
            }
        }


        public static bool IsValidName([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name.Length < 2 || name.Length > 16) return false;
            return ContainsValidCharacters(name);
        }

        public static bool ContainsValidCharacters([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                if ((ch < '0' && ch != '.') || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z')
                {
                    return false;
                }
            }
            return true;
        }

        #endregion


        public void TeleportTo(Position pos)
        {
            StopSpectating();
            Send(PacketWriter.MakeSelfTeleport(pos));
            Position = pos;
        }


        public TimeSpan IdleTime
        {
            get
            {
                return DateTime.UtcNow.Subtract(LastActiveTime);
            }
        }


        public void ResetIdleTimer()
        {
            LastActiveTime = DateTime.UtcNow;
        }


        #region Kick

        public void Kick([NotNull] Player player, [CanBeNull] string reason, LeaveReason context,
                         bool announce, bool raiseEvents, bool recordToPlayerDB)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (!Enum.IsDefined(typeof(LeaveReason), context))
            {
                throw new ArgumentOutOfRangeException("context");
            }
            if (reason != null && reason.Trim().Length == 0) reason = null;

            if (!player.Can(Permission.Kick))
            {
                PlayerOpException.ThrowPermissionMissing(player, Info, "kick", Permission.Kick);
            }

            if (player == this)
            {
                PlayerOpException.ThrowCannotTargetSelf(player, Info, "kick");
            }

            if (!player.Can(Permission.Kick, Info.Rank))
            {
                PlayerOpException.ThrowPermissionLimit(player, Info, "kick", Permission.Kick);
            }

            PlayerOpException.CheckKickReason(reason, player, Info);

            if (raiseEvents)
            {
                var e = new PlayerBeingKickedEventArgs(this, player, reason, announce, recordToPlayerDB, context);
                RaisePlayerBeingKickedEvent(e);
                if (e.Cancel) PlayerOpException.ThrowCancelled(player, Info);
                recordToPlayerDB = e.RecordToPlayerDB;
            }

            string kickReason;
            if (reason != null)
            {
                kickReason = String.Format("Kicked by {0}: {1}", player.Name, reason);
            }
            else
            {
                kickReason = String.Format("Kicked by {0}", player.Name);
            }
            Kick(kickReason, context);

            Logger.Log(LogType.UserActivity,
                        "{0} kicked {1}. Reason: {2}",
                        player.Name, Name, reason ?? "");
            if (recordToPlayerDB)
            {
                Info.ProcessKick(player, reason);
            }

            if (announce)
            {
                if (reason != null && ConfigKey.AnnounceKickAndBanReasons.Enabled())
                {
                    Server.Message("{0}&W was kicked by {1}&W: {2}",
                                    ClassyName, player.ClassyName, reason);
                }
                else
                {
                    Server.Message("{0}&W was kicked by {1}",
                                    ClassyName, player.ClassyName);
                }
            }

            if (raiseEvents)
            {
                var e = new PlayerKickedEventArgs(this, player, reason, announce, recordToPlayerDB, context);
                RaisePlayerKickedEvent(e);
            }
        }

        #endregion


        [CanBeNull]
        public string LastUsedPlayerName { get; set; }

        [CanBeNull]
        public string LastUsedWorldName { get; set; }


        public override string ToString()
        {
            if (Info != null)
            {
                return String.Format("Player({0})", Info.Name);
            }
            else
            {
                return String.Format("Player({0})", IP);
            }
        }
    }


    sealed class PlayerListSorter : IComparer<Player>
    {
        public static readonly PlayerListSorter Instance = new PlayerListSorter();

        public int Compare(Player x, Player y)
        {
            if (x.Info.Rank == y.Info.Rank)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
            }
            else
            {
                return x.Info.Rank.Index - y.Info.Rank.Index;
            }
        }
    }
}