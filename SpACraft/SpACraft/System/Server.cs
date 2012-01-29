﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpACraft;
using JetBrains.Annotations;

namespace SpACraft
{
    public partial class Server
    {


        /// <summary> Describes the circumstances of server shutdown. </summary>
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

            /// <summary> Delay before shutting down. </summary>
            public TimeSpan Delay { get; private set; }

            /// <summary> Whether fCraft should try to forcefully kill the current process. </summary>
            public bool KillProcess { get; private set; }

            /// <summary> Whether the server is expected to restart itself after shutting down. </summary>
            public bool Restart { get; private set; }

            /// <summary> Player who initiated the shutdown. May be null or Console. </summary>
            [CanBeNull]
            public Player InitiatedBy { get; private set; }
        }

        /// <summary> Categorizes conditions that lead to server shutdowns. </summary>
        public enum ShutdownReason
        {
            Unknown,

            /// <summary> Use for mod- or plugin-triggered shutdowns. </summary>
            Other,

            /// <summary> InitLibrary or InitServer failed. </summary>
            FailedToInitialize,

            /// <summary> StartServer failed. </summary>
            FailedToStart,

            /// <summary> Server is restarting, usually because someone called /Restart. </summary>
            Restarting,

            /// <summary> Server has experienced a non-recoverable crash. </summary>
            Crashed,

            /// <summary> Server is shutting down, usually because someone called /Shutdown. </summary>
            ShuttingDown,

            /// <summary> Server process is being closed/killed. </summary>
            ProcessClosing
        }
    }
}
