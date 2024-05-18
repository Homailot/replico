using System.Collections.Generic;
using Gestures;
using Player;
using Replica;
using UnityEngine;

namespace Tasks
{
    public class SecondTask : Task
    {
        [SerializeField] private GestureDetector gestureDetector;
        [SerializeField] private List<Vector3> taskPoints;
        [SerializeField] private List<int> unacknowledgedPoints;
        [SerializeField] private PlayerManager playerManager;
        
        private Logger _logger;

        public override void StartTask(Logger logger)
        {
            var playerId = gestureDetector.GetPlayerId();
            var otherPlayerId = playerId == 0 ? 1ul : 0ul;
            gestureDetector.ClearPointsOfInterest();
            
            for (var i = 0; i < taskPoints.Count; i++)
            {
                var balloonPoint = gestureDetector.AddPointOfInterest(taskPoints[i], otherPlayerId, (ulong) i + 1ul);

                if (unacknowledgedPoints.Contains(i)) continue;
                balloonPoint.selectable = false;
                                    
                var line = balloonPoint.GetIndicatorLine();
                line.DisableLine();
                line.DisablePinIndicator();
            }
            playerManager.SetBalloonId((ulong)taskPoints.Count);

            gestureDetector.AddPointAcknowledgedListener(PointSelected);
            
            _logger = logger;
            _logger.StartTask();
        }

        protected override void EndTaskInternal(bool success)
        {
            CleanTask();
            _logger.EndTask(success); 
        }

        public override void CleanTask()
        {
            gestureDetector.ClearPointsOfInterest();
        }

        private void PointSelected(ulong id)
        {
            unacknowledgedPoints.Remove((int)id - 1);
            
            var finished = Next();
            if (finished)
            {
                EndTask(true);
            }
        }
        
        public override bool Next()
        {
            _logger.TaskStep();
            return unacknowledgedPoints.Count == 0;
        } 
    }
}