using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Tasks
{
    public class Logger : MonoBehaviour
    {
        private struct ReplicaTransform
        {
            public float Time;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        private struct PlayerTransformData
        {
            public float Time;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private struct GestureData
        {
            public float Key;
            public GestureType GestureType;
            public GestureState GestureState;
        }

        private enum GestureType
        {
            Transform,
            VerticalTransform,
            BalloonSelection
        }

        private enum GestureState
        {
            On,
            Off
        }

        [SerializeField] private string outputFilePath = "Assets/Resources/Logs/";
        private Camera _camera;

        private ulong _taskId;
        private ulong _currentTaskId;
        private ulong _playerId;
        private float _taskStartTime;
        private float _taskTime;
    
        private float _timeSpentInTransforms;
        private float _transformStartTime;
        private ulong _transformCount;
    
        private float _timeSpentInVerticalTransforms;
        private float _verticalTransformStartTime;
        private ulong _verticalTransformCount;
    
        private float _replicaTranslationDistance;
        private float _replicaRotationAngle;
        private float _replicaScaleFactor;
    
        private readonly IList<ReplicaTransform> _replicaTransforms = new List<ReplicaTransform>();
    
        private float _timeSpentInBalloonSelection;
        private float _balloonSelectionStartTime;
        private ulong _balloonSelectionCount;
    
        private ulong _pointCreationCount;
        private ulong _teleportationCount;
        private ulong _tableJoinCount; 
        private ulong _pointDeletionCount = 0;
        private ulong _pointAcknowledgementCount = 0; 
        // TODO: task steps
        private ulong _taskSteps;
        
        private ulong _uniqueTouchCount;
        private float _fingerMovement;
    
        private readonly IList<GestureData> _gestures = new List<GestureData>();
    
        private readonly IDictionary<Finger, Vector2> _lastFingerPositions = new Dictionary<Finger, Vector2>(new FingerEqualityComparer());
        private readonly IDictionary<float, IDictionary<int, Vector2>> _fingerPositions = new Dictionary<float, IDictionary<int, Vector2>>();
    
        private float _headRotation;
        private float _headMovement;
        private readonly IList<PlayerTransformData> _playerTransforms = new List<PlayerTransformData>();
        private PlayerTransformData _lastPlayerTransform;
    
        private string _finalDirectoryPath;

        public void Awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        public void EnableLogger(string suffix)
        {
            _taskId = 0;
            var customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
        
            _finalDirectoryPath = $"{outputFilePath}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{suffix}/";
            Directory.CreateDirectory(_finalDirectoryPath); 
        
            var outputAveragePath = $"{_finalDirectoryPath}all.csv";
            var writer = new StreamWriter(outputAveragePath, true);
            writer.WriteLine("TaskId;TaskDuration;TaskSteps;TaskSuccess;TimeSpentInTransforms;TransformCount;TimeSpentInVerticalTransforms;VerticalTransformCount;ReplicaTranslationDistance;ReplicaRotationAngle;ReplicaScaleFactor;TimeSpentInBalloonSelection;BalloonSelectionCount;PointCreationCount;TeleportationCount;TableJoinCount;PointDeletionCount;PointAcknowledgementCount;UniqueTouchCount;FingerMovement;HeadRotation;HeadMovement;PlayerId");
            writer.Close();
        }
    
        public void StartTask()
        {
            _currentTaskId = ++_taskId;
            _taskStartTime = Time.time;
            _taskTime = 0;
            _taskSteps = 0;
        
            _timeSpentInTransforms = 0;
            _transformStartTime = 0;
            _transformCount = 0;
        
            _timeSpentInVerticalTransforms = 0;
            _verticalTransformStartTime = 0;
            _verticalTransformCount = 0;
        
            _replicaTranslationDistance = 0;
            _replicaRotationAngle = 0;
            _replicaScaleFactor = 0;
        
            _replicaTransforms.Clear();
        
            _timeSpentInBalloonSelection = 0;
            _balloonSelectionStartTime = 0;
            _balloonSelectionCount = 0;
        
            _pointCreationCount = 0;
            _teleportationCount = 0;
            _tableJoinCount = 0;
            _pointDeletionCount = 0;
            _pointAcknowledgementCount = 0;
        
            _uniqueTouchCount = 0;
            _fingerMovement = 0;
        
            _lastFingerPositions.Clear();
            _fingerPositions.Clear();
            _camera = Camera.main;
        
            _gestures.Clear();
        
            _headRotation = 0;
            _headMovement = 0;
            _playerTransforms.Clear();
            _lastPlayerTransform = new PlayerTransformData();
        
            var playerNetwork = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            _playerId = playerNetwork.playerId;
            Debug.Log($"Task {_currentTaskId} started.");
        }
        
        public void PauseTask()
        {
            if (_currentTaskId == 0)
            {
                Debug.LogWarning("No task to pause.");
                return;
            }
        
            _taskTime += Time.time - _taskStartTime;
            Debug.Log($"Task {_currentTaskId} paused.");
            Debug.Log($"Task Time: {_taskTime}");
        }
        
        public void ResumeTask()
        {
            if (_currentTaskId == 0)
            {
                Debug.LogWarning("No task to resume.");
                return;
            }
        
            _taskStartTime = Time.time;
            Debug.Log($"Task {_currentTaskId} resumed.");
        }
    
        public void EndTask(bool success)
        {
            if (_currentTaskId == 0)
            {
                Debug.LogWarning("No task to end.");
                return;
            }
        
            var taskEndTime = Time.time;
            _taskTime += taskEndTime - _taskStartTime;
            Debug.Log($"Task {_currentTaskId} {(success ? "completed" : "failed")} in {_taskTime} seconds.");
        
            var outputAveragePath = $"{_finalDirectoryPath}all.csv";
            var outputTaskPath = $"{_finalDirectoryPath}Task_{_currentTaskId}/";
            Directory.CreateDirectory(outputTaskPath);
        
            var writer = new StreamWriter(outputAveragePath, true);
            writer.WriteLine($"{_currentTaskId};{_taskTime};{_taskSteps};{success};{_timeSpentInTransforms};{_transformCount};{_timeSpentInVerticalTransforms};{_verticalTransformCount};{_replicaTranslationDistance};{_replicaRotationAngle};{_replicaScaleFactor};{_timeSpentInBalloonSelection};{_balloonSelectionCount};{_pointCreationCount};{_teleportationCount};{_tableJoinCount};{_pointDeletionCount};{_pointAcknowledgementCount};{_uniqueTouchCount};{_fingerMovement};{_headRotation};{_headMovement};{_playerId}");
            writer.Close();
        
            var taskWriter = new StreamWriter($"{outputTaskPath}finger_movements.csv", true);
            taskWriter.WriteLine("Time;Finger0;Finger1;Finger2;Finger3;Finger4;Finger5;Finger6;Finger7;Finger8;Finger9");
            foreach (var fingerPosition in _fingerPositions)
            {
                taskWriter.Write($"{fingerPosition.Key}");

                for (var i = 0; i < 10; i++)
                {
                    taskWriter.Write(fingerPosition.Value.TryGetValue(i, out var position)
                        ? $";({position.x},{position.y})"
                        : ";");
                }
                taskWriter.WriteLine();
            }
            taskWriter.Close();
        
            var playerTransformWriter = new StreamWriter($"{outputTaskPath}player_transforms.csv", true);
            playerTransformWriter.WriteLine("Time;PositionX;PositionY;PositionZ;RotationX;RotationY;RotationZ;RotationW");
            foreach (var playerTransform in _playerTransforms)
            {
                playerTransformWriter.WriteLine($"{playerTransform.Time};{playerTransform.Position.x};{playerTransform.Position.y};{playerTransform.Position.z};{playerTransform.Rotation.x};{playerTransform.Rotation.y};{playerTransform.Rotation.z};{playerTransform.Rotation.w}");
            }
            playerTransformWriter.Close();
        
            var replicaTransformWriter = new StreamWriter($"{outputTaskPath}replica_transforms.csv", true);
            replicaTransformWriter.WriteLine("Time;PositionX;PositionY;PositionZ;RotationX;RotationY;RotationZ;RotationW;ScaleX;ScaleY;ScaleZ");
            foreach (var replicaTransform in _replicaTransforms)
            {
                replicaTransformWriter.WriteLine($"{replicaTransform.Time};{replicaTransform.Position.x};{replicaTransform.Position.y};{replicaTransform.Position.z};{replicaTransform.Rotation.x};{replicaTransform.Rotation.y};{replicaTransform.Rotation.z};{replicaTransform.Rotation.w};{replicaTransform.Scale.x};{replicaTransform.Scale.y};{replicaTransform.Scale.z}");
            }
            replicaTransformWriter.Close();
        
            var gestureDetectionWriter = new StreamWriter($"{outputTaskPath}gesture_detection.csv", true);
            gestureDetectionWriter.WriteLine("Time;DetectedGesture;GestureState");
            foreach (var gesture in _gestures)
            {
                gestureDetectionWriter.WriteLine($"{gesture.Key};{gesture.GestureType};{gesture.GestureState}");
            }
            gestureDetectionWriter.Close();
        
            _currentTaskId = 0;
        }
    
        public void StartTransform()
        {
            if (_currentTaskId == 0) return;
        
            _transformStartTime = Time.time;
            _transformCount++;
            _gestures.Add( new GestureData {Key = Time.time - _taskStartTime, GestureType = GestureType.Transform, GestureState = GestureState.On });
        }
    
        public void EndTransform()
        {
            if (_currentTaskId == 0) return;
        
            _timeSpentInTransforms += Time.time - _transformStartTime;
            _gestures.Add(new GestureData {Key = Time.time - _taskStartTime, GestureType = GestureType.Transform, GestureState = GestureState.Off });
        }
    
        public void StartVerticalTransform()
        {
            if (_currentTaskId == 0) return;
        
            _verticalTransformStartTime = Time.time;
            _verticalTransformCount++;
            _gestures.Add(new GestureData {Key = Time.time - _taskStartTime, GestureType = GestureType.VerticalTransform, GestureState = GestureState.On });
        }
    
        public void EndVerticalTransform()
        {
            if (_currentTaskId == 0) return;
        
            _timeSpentInVerticalTransforms += Time.time - _verticalTransformStartTime;
            _gestures.Add(new GestureData {Key = Time.time - _taskStartTime, GestureType = GestureType.VerticalTransform, GestureState = GestureState.Off });
        }
    
        public void StartBalloonSelection()
        {
            if (_currentTaskId == 0) return;
        
            _balloonSelectionStartTime = Time.time;
            _balloonSelectionCount++;
            _gestures.Add(new GestureData {Key = Time.time - _taskStartTime, GestureType = GestureType.BalloonSelection, GestureState = GestureState.On });
        }
    
        public void EndBalloonSelection()
        {
            if (_currentTaskId == 0) return;
        
            _timeSpentInBalloonSelection += Time.time - _balloonSelectionStartTime;
            _gestures.Add(new GestureData {Key = Time.time - _taskStartTime, GestureType = GestureType.BalloonSelection, GestureState = GestureState.Off });
        }
    
        public void PointCreation()
        {
            if (_currentTaskId == 0) return;
        
            _pointCreationCount++;
            Debug.Log("Point creation.");
        }
    
        public void Teleportation()
        {
            if (_currentTaskId == 0) return;
        
            _teleportationCount++;
            Debug.Log("Teleportation.");
        }
    
        public void TableJoin()
        {
            if (_currentTaskId == 0) return;
        
            _tableJoinCount++;
            Debug.Log("Table join.");
        }
    
        public void PointDeletion()
        {
            if (_currentTaskId == 0) return;
        
            _pointDeletionCount++;
            Debug.Log("Point deletion.");
        }
    
        public void PointAcknowledgement()
        {
            if (_currentTaskId == 0) return;
        
            _pointAcknowledgementCount++;
            Debug.Log("Point acknowledgement.");
        }
    
        public void TaskStep()
        {
            if (_currentTaskId == 0) return;
        
            _taskSteps++;
        }
    
        public void UpdateReplicaTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (_currentTaskId == 0) return;
        
            var replicaTransform = new ReplicaTransform
            {
                Time = Time.time - _taskStartTime,
                Position = position,
                Rotation = rotation,
                Scale = scale
            };
        
            if (_replicaTransforms.Count >= 1)
            {
                var previousReplicaTransform = _replicaTransforms.ElementAt(_replicaTransforms.Count - 1);
                _replicaTranslationDistance += (previousReplicaTransform.Position - position).magnitude;
                _replicaRotationAngle += Quaternion.Angle(previousReplicaTransform.Rotation, rotation);
                _replicaScaleFactor += (previousReplicaTransform.Scale - scale).magnitude;
            
                if (previousReplicaTransform.Position != position || previousReplicaTransform.Rotation != rotation || previousReplicaTransform.Scale != scale)
                {
                    _replicaTransforms.Add(replicaTransform);
                }
            }
            else
            {
                _replicaTransforms.Add(replicaTransform);
            }
        }

        private void Update()
        {
            if (_currentTaskId == 0) return;
        
            var touches = Touch.activeFingers;
            var touchesDictionary = touches.ToDictionary(finger => finger.index, finger => finger.screenPosition);

            if (touches.Count == 0)
            {
                _lastFingerPositions.Clear();
                return;
            }

            _fingerPositions.Add(Time.time - _taskStartTime, touchesDictionary);

            if (_lastFingerPositions.Count == 0)
            {
                foreach (var finger in touches)
                {
                    _lastFingerPositions.Add(finger, finger.screenPosition);
                    _uniqueTouchCount++;
                }
                return;
            }

            foreach (var finger in touches)
            {
                if (!_lastFingerPositions.ContainsKey(finger))
                {
                    _uniqueTouchCount++;
                } 
            
                if (_lastFingerPositions.TryGetValue(finger, out var lastPosition))
                {
                    _fingerMovement += (finger.screenPosition - lastPosition).magnitude;
                } 
            }
        
            _lastFingerPositions.Clear();
            foreach (var finger in touches)
            {
                _lastFingerPositions.Add(finger, finger.screenPosition);
            } 
        }

        private void LateUpdate()
        {
            if (_currentTaskId == 0) return;
        
            var playerTransform = _camera.transform;
            var playerTransformData = new PlayerTransformData
            {
                Time = Time.time - _taskStartTime,
                Position = playerTransform.position,
                Rotation = playerTransform.rotation
            };
            _playerTransforms.Add(playerTransformData);
        
            if (_lastPlayerTransform.Time != 0)
            {
                _headRotation += Quaternion.Angle(_lastPlayerTransform.Rotation, playerTransform.localRotation);
                _headMovement += (_lastPlayerTransform.Position - playerTransform.localPosition).magnitude;
            }

            _lastPlayerTransform = new PlayerTransformData
            {
                Time = Time.time - _taskStartTime,
                Position = playerTransform.localPosition,
                Rotation = playerTransform.localRotation
            };
        }

        public bool startTask;
        public bool endTask;
        public bool endTaskFail;

        private void OnValidate()
        {
            if (startTask)
            {
                StartTask();
                startTask = false;
            }
        
            if (endTask)
            {
                EndTask(true);
                endTask = false;
            }
        
            if (endTaskFail)
            {
                EndTask(false);
                endTaskFail = false;
            }
        }
    }
}