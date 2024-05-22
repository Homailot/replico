using System.Collections.Generic;
using Gestures;
using Player;
using Tables;
using UnityEngine;

namespace Tasks
{
    public class GettingUsedToTask : Task
    {
        [SerializeField] private GestureDetector gestureDetector;
        [SerializeField] private PlayerManager playerManager;
        [SerializeField] private TableManager tableManager;
        [SerializeField] private List<Vector3> pointsOfInterest;
        [SerializeField] private List<int> unacknowledgedPoints;
        [SerializeField] private List<GameObject> playerModels;
        [SerializeField] private Transform otherPlayerTableSpawnPoint;
        [SerializeField] private Transform otherPlayerSpawnPoint;

        private ulong _otherPlayerId;

        public override void StartTask(Logger logger)
        {
            var playerId = gestureDetector.GetPlayerId();
            _otherPlayerId = playerId == 0 ? 1ul : 0ul;

            gestureDetector.ClearPointsOfInterest();
            for (var i = 0; i < pointsOfInterest.Count; i++)
            {
                var balloonPoint = gestureDetector.AddPointOfInterest(pointsOfInterest[i], _otherPlayerId, (ulong) i + 1ul);

                if (unacknowledgedPoints.Contains(i)) continue;
                balloonPoint.selectable = false;

                var line = balloonPoint.GetIndicatorLine();
                line.DisableLine();
                line.DisablePinIndicator();
            }
            playerManager.SetBalloonId((ulong)pointsOfInterest.Count);

            tableManager.CreateNewTableWithPlayer(_otherPlayerId, otherPlayerTableSpawnPoint.position,
                otherPlayerTableSpawnPoint.rotation);
            var otherPlayerPrefab = playerModels[_otherPlayerId == 0 ? 0 : 1];
            var otherPlayer = Instantiate(otherPlayerPrefab, otherPlayerSpawnPoint.position,
                otherPlayerSpawnPoint.rotation);
        }

        protected override void EndTaskInternal(bool success)
        {
            
        }

        public override void CleanTask()
        {
        }
    }
}