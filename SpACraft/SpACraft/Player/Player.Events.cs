using System;
using System.Net;
using SpACraft.Events;
using JetBrains.Annotations;

namespace SpACraft
{
    partial class Player
    {
        public static event EventHandler<PlayerConnectingEventArgs> Connecting;


        public static event EventHandler<PlayerConnectedEventArgs> Connected;


        public static event EventHandler<PlayerEventArgs> Ready;


        public static event EventHandler<PlayerMovingEventArgs> Moving;


        public static event EventHandler<PlayerMovedEventArgs> Moved;


        public static event EventHandler<PlayerClickingEventArgs> Clicking;


        public static event EventHandler<PlayerClickedEventArgs> Clicked;


        public static event EventHandler<PlayerPlacingBlockEventArgs> PlacingBlock;


        public static event EventHandler<PlayerPlacedBlockEventArgs> PlacedBlock;


        public static event EventHandler<PlayerBeingKickedEventArgs> BeingKicked;


        public static event EventHandler<PlayerKickedEventArgs> Kicked;

        public static event EventHandler<PlayerEventArgs> HideChanged;


        public static event EventHandler<PlayerDisconnectedEventArgs> Disconnected;


        public static event EventHandler<PlayerJoiningWorldEventArgs> JoiningWorld;


        public static event EventHandler<PlayerJoinedWorldEventArgs> JoinedWorld;


        static bool RaisePlayerConnectingEvent([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");
            var h = Connecting;
            if (h == null) return false;
            var e = new PlayerConnectingEventArgs(player);
            h(null, e);
            return e.Cancel;
        }


        static World RaisePlayerConnectedEvent([NotNull] Player player, World world)
        {
            if (player == null) throw new ArgumentNullException("player");
            var h = Connected;
            if (h == null) return world;
            var e = new PlayerConnectedEventArgs(player, world);
            h(null, e);
            return e.StartingWorld;
        }


        static void RaisePlayerReadyEvent([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");
            var h = Ready;
            if (h != null) h(null, new PlayerEventArgs(player));
        }


        static bool RaisePlayerMovingEvent([NotNull] Player player, Position newPos)
        {
            if (player == null) throw new ArgumentNullException("player");
            var h = Moving;
            if (h == null) return false;
            var e = new PlayerMovingEventArgs(player, newPos);
            h(null, e);
            return e.Cancel;
        }


        static void RaisePlayerMovedEvent([NotNull] Player player, Position oldPos)
        {
            if (player == null) throw new ArgumentNullException("player");
            var h = Moved;
            if (h != null) h(null, new PlayerMovedEventArgs(player, oldPos));
        }


        static bool RaisePlayerClickingEvent([NotNull] PlayerClickingEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            var h = Clicking;
            if (h == null) return false;
            h(null, e);
            return e.Cancel;
        }


        static void RaisePlayerClickedEvent(Player player, Vector3I coords,
                                             ClickAction action, Block block)
        {
            var handler = Clicked;
            if (handler != null)
            {
                handler(null, new PlayerClickedEventArgs(player, coords, action, block));
            }
        }


        internal static void RaisePlayerPlacedBlockEvent(Player player, Map map, Vector3I coords,
                                                          Block oldBlock, Block newBlock, BlockChangeContext context)
        {
            var handler = PlacedBlock;
            if (handler != null)
            {
                handler(null, new PlayerPlacedBlockEventArgs(player, map, coords, oldBlock, newBlock, context));
            }
        }


        static void RaisePlayerBeingKickedEvent([NotNull] PlayerBeingKickedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            var h = BeingKicked;
            if (h != null) h(null, e);
        }


        static void RaisePlayerKickedEvent([NotNull] PlayerKickedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            var h = Kicked;
            if (h != null) h(null, e);
        }


        internal static void RaisePlayerHideChangedEvent([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");
            var h = HideChanged;
            if (h != null) h(null, new PlayerEventArgs(player));
        }


        static void RaisePlayerDisconnectedEvent([NotNull] Player player, LeaveReason leaveReason)
        {
            if (player == null) throw new ArgumentNullException("player");
            var h = Disconnected;
            if (h != null) h(null, new PlayerDisconnectedEventArgs(player, leaveReason, false));
        }


        static bool RaisePlayerJoiningWorldEvent([NotNull] Player player, [NotNull] World newWorld, WorldChangeReason reason,
                                                 string textLine1, string textLine2)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (newWorld == null) throw new ArgumentNullException("newWorld");
            var h = JoiningWorld;
            if (h == null) return false;
            var e = new PlayerJoiningWorldEventArgs(player, player.World, newWorld, reason, textLine1, textLine2);
            h(null, e);
            return e.Cancel;
        }


        static void RaisePlayerJoinedWorldEvent(Player player, World oldWorld, WorldChangeReason reason)
        {
            var h = JoinedWorld;
            if (h != null) h(null, new PlayerJoinedWorldEventArgs(player, oldWorld, player.World, reason));
        }
    }
}

namespace SpACraft.Events
{

    public sealed class PlayerEventArgs : EventArgs, IPlayerEvent
    {
        internal PlayerEventArgs(Player player)
        {
            Player = player;
        }

        public Player Player { get; private set; }
    }


    public sealed class SessionConnectingEventArgs : EventArgs, ICancellableEvent
    {
        internal SessionConnectingEventArgs([NotNull] IPAddress ip)
        {
            if (ip == null) throw new ArgumentNullException("ip");
            IP = ip;
        }

        [NotNull]
        public IPAddress IP { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class SessionDisconnectedEventArgs : EventArgs
    {
        internal SessionDisconnectedEventArgs([NotNull] Player player, LeaveReason leaveReason)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            LeaveReason = leaveReason;
        }

        [NotNull]
        public Player Player { get; private set; }
        public LeaveReason LeaveReason { get; private set; }
    }


    public sealed class PlayerConnectingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent
    {
        internal PlayerConnectingEventArgs([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
        }

        [NotNull]
        public Player Player { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerConnectedEventArgs : EventArgs, IPlayerEvent
    {
        internal PlayerConnectedEventArgs([NotNull] Player player, World startingWorld)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            StartingWorld = startingWorld;
        }

        [NotNull]
        public Player Player { get; private set; }
        public World StartingWorld { get; set; }
    }


    public sealed class PlayerMovingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent
    {
        internal PlayerMovingEventArgs([NotNull] Player player, Position newPos)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            OldPosition = player.Position;
            NewPosition = newPos;
        }

        [NotNull]
        public Player Player { get; private set; }
        public Position OldPosition { get; private set; }
        public Position NewPosition { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerMovedEventArgs : EventArgs, IPlayerEvent
    {
        internal PlayerMovedEventArgs([NotNull] Player player, Position oldPos)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            OldPosition = oldPos;
            NewPosition = player.Position;
        }

        [NotNull]
        public Player Player { get; private set; }
        public Position OldPosition { get; private set; }
        public Position NewPosition { get; private set; }
    }


    public sealed class PlayerClickingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent
    {
        internal PlayerClickingEventArgs([NotNull] Player player, Vector3I coords,
                                         ClickAction action, Block block)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            Coords = coords;
            Action = action;
            Block = block;
        }

        [NotNull]
        public Player Player { get; private set; }
        public Vector3I Coords { get; set; }
        public Block Block { get; set; }
        public ClickAction Action { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerClickedEventArgs : EventArgs, IPlayerEvent
    {
        internal PlayerClickedEventArgs([NotNull] Player player, Vector3I coords, ClickAction action, Block block)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            Coords = coords;
            Block = block;
            Action = action;
        }

        [NotNull]
        public Player Player { get; private set; }
        public Vector3I Coords { get; private set; }
        public Block Block { get; private set; }
        public ClickAction Action { get; private set; }
    }


    public sealed class PlayerPlacingBlockEventArgs : PlayerPlacedBlockEventArgs
    {
        internal PlayerPlacingBlockEventArgs([NotNull] Player player, [NotNull] Map map, Vector3I coords,
                                             Block oldBlock, Block newBlock, BlockChangeContext context, CanPlaceResult result)
            : base(player, map, coords, oldBlock, newBlock, context)
        {
            Result = result;
        }

        public CanPlaceResult Result { get; set; }
    }


    public class PlayerPlacedBlockEventArgs : EventArgs, IPlayerEvent
    {
        internal PlayerPlacedBlockEventArgs([NotNull] Player player, [NotNull] Map map, Vector3I coords,
                                            Block oldBlock, Block newBlock, BlockChangeContext context)
        {
            if (map == null) throw new ArgumentNullException("map");
            Player = player;
            Map = map;
            Coords = coords;
            OldBlock = oldBlock;
            NewBlock = newBlock;
            Context = context;
        }


        [NotNull]
        public Player Player { get; private set; }

        [NotNull]
        public Map Map { get; private set; }

        public Vector3I Coords { get; private set; }
        public Block OldBlock { get; private set; }
        public Block NewBlock { get; private set; }
        public BlockChangeContext Context { get; private set; }
    }


    public sealed class PlayerBeingKickedEventArgs : PlayerKickedEventArgs, ICancellableEvent
    {
        internal PlayerBeingKickedEventArgs([NotNull] Player player, [NotNull] Player kicker, [CanBeNull] string reason,
                                             bool announce, bool recordToPlayerDB, LeaveReason context)
            : base(player, kicker, reason, announce, recordToPlayerDB, context)
        {
        }

        public bool Cancel { get; set; }
    }


    public class PlayerKickedEventArgs : EventArgs, IPlayerEvent
    {
        internal PlayerKickedEventArgs([NotNull] Player player, [NotNull] Player kicker, [CanBeNull] string reason,
                                       bool announce, bool recordToPlayerDB, LeaveReason context)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (kicker == null) throw new ArgumentNullException("kicker");
            Player = player;
            Kicker = kicker;
            Reason = reason;
            Announce = announce;
            RecordToPlayerDB = recordToPlayerDB;
            Context = context;
        }

        [NotNull]
        public Player Player { get; private set; }

        [NotNull]
        public Player Kicker { get; protected set; }

        [CanBeNull]
        public string Reason { get; protected set; }

        public bool Announce { get; private set; }

        public bool RecordToPlayerDB { get; protected set; }

        public LeaveReason Context { get; protected set; }
    }


    public sealed class PlayerDisconnectedEventArgs : EventArgs, IPlayerEvent
    {
        internal PlayerDisconnectedEventArgs([NotNull] Player player, LeaveReason leaveReason, bool isFake)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            LeaveReason = leaveReason;
            IsFake = isFake;
        }

        [NotNull]
        public Player Player { get; private set; }
        public LeaveReason LeaveReason { get; private set; }
        public bool IsFake { get; private set; }
    }


    public sealed class PlayerJoiningWorldEventArgs : EventArgs, IPlayerEvent, ICancellableEvent
    {
        internal PlayerJoiningWorldEventArgs([NotNull] Player player, [CanBeNull] World oldWorld,
                                             [NotNull] World newWorld, WorldChangeReason reason,
                                             string textLine1, string textLine2)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (newWorld == null) throw new ArgumentNullException("newWorld");
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
            Reason = reason;
            TextLine1 = textLine1;
            TextLine2 = textLine2;
        }

        [NotNull]
        public Player Player { get; private set; }

        [CanBeNull]
        public World OldWorld { get; private set; }

        [NotNull]
        public World NewWorld { get; private set; }

        public WorldChangeReason Reason { get; private set; }
        public string TextLine1 { get; set; }
        public string TextLine2 { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerJoinedWorldEventArgs : EventArgs, IPlayerEvent
    {
        public PlayerJoinedWorldEventArgs([NotNull] Player player, World oldWorld, World newWorld, WorldChangeReason reason)
        {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
            Reason = reason;
        }

        [NotNull]
        public Player Player { get; private set; }
        public World OldWorld { get; private set; }
        public World NewWorld { get; private set; }
        public WorldChangeReason Reason { get; private set; }
    }
}