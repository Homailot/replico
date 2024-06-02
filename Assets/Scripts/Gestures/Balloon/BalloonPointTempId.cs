using UnityEngine;

namespace Gestures.Balloon
{
    public class BalloonPointTempId
    {
        public ulong playerId { get; }
        public Vector3 position { get; }

        public BalloonPointTempId(ulong playerId, Vector3 position)
        {
            this.playerId = playerId;
            this.position = position;
        }
    }
}