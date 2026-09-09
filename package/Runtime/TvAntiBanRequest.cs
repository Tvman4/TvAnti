using System;

namespace TvAnti
{
    [Serializable]
    public sealed class TvAntiBanRequest
    {
        public string titleId;
        public string playerId;
        public string playerName;

        public string reason;
        public string detection;

        public string roomCode;

        public bool permanent = true;

        public string clientVersion;
        public string packageVersion;

        public long timestampUnix;
    }
}
