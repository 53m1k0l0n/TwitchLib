﻿namespace TwitchLib.Events.Services.LiveStreamMonitor
{
    #region using directives
    using System;
    using System.Collections.Generic;

    using Enums;
    #endregion
    /// <summary>Class representing event args for OnChannelMonitorStarted event.</summary>
    public class OnStreamMonitorStartedArgs : EventArgs
    {
        /// <summary>Event property representing channel the service is currently monitoring.</summary>
        public List<long> Channels;
        /// <summary>Event property representing seconds between queries to Twitch Api.</summary>
        public int CheckIntervalSeconds;
    }
}
