using UnityEngine;

namespace Gestures.Balloon
{
    public class BalloonPointId
    {
        public ulong id { get; }
        public ulong playerId { get; }

        public BalloonPointId(ulong playerId, ulong id)
        {
            this.playerId = playerId;
            this.id = id;
        }
    }
}