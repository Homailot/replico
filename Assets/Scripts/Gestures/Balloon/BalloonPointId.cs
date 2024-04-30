using UnityEngine;

namespace Gestures.Balloon
{
    public class BalloonPointId
    {
        public ulong playerId { get; }
        public Vector3 position { get; }

        public BalloonPointId(ulong playerId, Vector3 position)
        {
            this.playerId = playerId;
            this.position = position;
        }
    }
}