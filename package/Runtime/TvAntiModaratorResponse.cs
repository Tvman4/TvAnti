using System;

namespace TvAnti
{
    [Serializable]
    public sealed class TvAntiModeratorResponse
    {
        public bool accepted;
        public bool banned;
        public string message;
    }
}
