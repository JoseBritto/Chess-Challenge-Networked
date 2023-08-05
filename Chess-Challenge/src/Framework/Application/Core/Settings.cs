﻿using System.Numerics;

namespace ChessChallenge.Application
{
    public static class Settings
    {
        public const string Version = "1.16";

        // Game settings
        public const int GameDurationMilliseconds = 60 * 1000;
        public const float MinMoveDelay = 0;
        public static readonly bool RunBotsOnSeparateThread = true;

        // Display settings
        public const bool DisplayBoardCoordinates = true;
        public static readonly Vector2 ScreenSizeSmall = new(1280, 720);
        public static readonly Vector2 ScreenSizeBig = new(1920, 1080);

        // Other settings
        public const int MaxTokenCount = 1024;
        public const LogType MessagesToLog = LogType.All;

        // Network Settings
        public const string NetworkingProtocolVersion = "0.2";
        public const string ServerHostname = "127.0.0.1";
        public const int ServerPort = 4578;

        public enum LogType
        {
            None,
            ErrorOnly,
            All
        }
    }
}
