using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class Logger : MonoBehaviour
{
    private struct ReplicaTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    private struct PlayerTransformData
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private enum GestureType
    {
        Transform,
        VerticalTransform,
        BalloonSelection
    }

    [SerializeField] private string outputFilePath = "Assets/Resources/Logs/";
    private Camera _camera;
    
    private ulong _taskId = 0;
    private ulong _playerId = 0;
    private float _taskStartTime = 0; 
    
    private float _timeSpentInTransforms = 0;
    private float _transformStartTime = 0;
    private ulong _transformCount = 0;
    
    private float _timeSpentInVerticalTransforms = 0;
    private float _verticalTransformStartTime = 0;
    private ulong _verticalTransformCount = 0;
    
    private float _replicaTranslationDistance = 0;
    private float _replicaRotationAngle = 0;
    private float _replicaScaleFactor = 0;
    
    private readonly IDictionary<float, ReplicaTransform> _replicaTransforms = new Dictionary<float, ReplicaTransform>();
    
    private float _timeSpentInBalloonSelection = 0;
    private float _balloonSelectionStartTime = 0;
    private ulong _balloonSelectionCount = 0;
    
    private ulong _pointCreationCount = 0;
    private ulong _teleportationCount = 0;
    
    private ulong _uniqueTouchCount = 0;
    private float _fingerMovement = 0;
    
    private readonly IDictionary<float, GestureType> _gestures = new Dictionary<float, GestureType>();
    
    private readonly IDictionary<Finger, Vector2> _lastFingerPositions = new Dictionary<Finger, Vector2>(new FingerEqualityComparer());
    private readonly IDictionary<float, IDictionary<int, Vector2>> _fingerPositions = new Dictionary<float, IDictionary<int, Vector2>>();
    
    private float _headRotation = 0;
    private float _headMovement = 0;
    private readonly IDictionary<float, PlayerTransformData> _playerTransforms = new Dictionary<float, PlayerTransformData>();
    
    private string _finalDirectoryPath;

    public void Start()
    {
        _finalDirectoryPath = $"{outputFilePath}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}/";
        System.IO.Directory.CreateDirectory(_finalDirectoryPath); 
        
        var outputAveragePath = $"{_finalDirectoryPath}all.csv";
        var writer = new StreamWriter(outputAveragePath, true);
        writer.WriteLine("TaskId;TaskDuration;TaskSuccess;TimeSpentInTransforms;TransformCount;TimeSpentInVerticalTransforms;VerticalTransformCount;ReplicaTranslationDistance;ReplicaRotationAngle;ReplicaScaleFactor;TimeSpentInBalloonSelection;BalloonSelectionCount;PointCreationCount;TeleportationCount;UniqueTouchCount;FingerMovement;HeadRotation;HeadMovement;PlayerId");
        writer.Close();
    }
    
    public void StartTask()
    {
        _taskId++;
        _taskStartTime = Time.time;
        
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
        
        _uniqueTouchCount = 0;
        _fingerMovement = 0;
        
        _lastFingerPositions.Clear();
        _camera = Camera.main;
        
        var playerNetwork = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
        _playerId = playerNetwork.playerId;
        Debug.Log($"Task {_taskId} started.");
    }
    
    public void EndTask(bool success)
    {
        if (_taskId == 0)
        {
            Debug.LogWarning("No task to end.");
            return;
        }
        
        var taskEndTime = Time.time;
        Debug.Log($"Task {_taskId} {(success ? "completed" : "failed")} in {taskEndTime - _taskStartTime} seconds.");
        
        var outputAveragePath = $"{_finalDirectoryPath}all.csv";
        var outputTaskPath = $"{_finalDirectoryPath}Task_{_taskId}/";
        System.IO.Directory.CreateDirectory(outputTaskPath);
        
        var writer = new StreamWriter(outputAveragePath, true);
        writer.WriteLine($"{_taskId};{taskEndTime - _taskStartTime};{success};{_timeSpentInTransforms};{_transformCount};{_timeSpentInVerticalTransforms};{_verticalTransformCount};{_replicaTranslationDistance};{_replicaRotationAngle};{_replicaScaleFactor};{_timeSpentInBalloonSelection};{_balloonSelectionCount};{_pointCreationCount};{_teleportationCount};{_uniqueTouchCount};{_fingerMovement};{_headRotation};{_headMovement};{_playerId}");
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
            playerTransformWriter.WriteLine($"{playerTransform.Key};{playerTransform.Value.Position.x};{playerTransform.Value.Position.y};{playerTransform.Value.Position.z};{playerTransform.Value.Rotation.x};{playerTransform.Value.Rotation.y};{playerTransform.Value.Rotation.z};{playerTransform.Value.Rotation.w}");
        }
        playerTransformWriter.Close();
        
        var replicaTransformWriter = new StreamWriter($"{outputTaskPath}replica_transforms.csv", true);
        replicaTransformWriter.WriteLine("Time;PositionX;PositionY;PositionZ;RotationX;RotationY;RotationZ;RotationW;ScaleX;ScaleY;ScaleZ");
        foreach (var replicaTransform in _replicaTransforms)
        {
            replicaTransformWriter.WriteLine($"{replicaTransform.Key};{replicaTransform.Value.Position.x};{replicaTransform.Value.Position.y};{replicaTransform.Value.Position.z};{replicaTransform.Value.Rotation.x};{replicaTransform.Value.Rotation.y};{replicaTransform.Value.Rotation.z};{replicaTransform.Value.Rotation.w};{replicaTransform.Value.Scale.x};{replicaTransform.Value.Scale.y};{replicaTransform.Value.Scale.z}");
        }
        replicaTransformWriter.Close();
        
        var gestureDetectionWriter = new StreamWriter($"{outputTaskPath}gesture_detection.csv", true);
        gestureDetectionWriter.WriteLine("Time;DetectedGesture");
        foreach (var gesture in _gestures)
        {
            gestureDetectionWriter.WriteLine($"{gesture.Key};{gesture.Value}");
        }
        gestureDetectionWriter.Close();
        
        _taskId = 0;
    }
    
    public void UpdateReplicaTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (_taskId == 0) return;
        
        var replicaTransform = new ReplicaTransform
        {
            Position = position,
            Rotation = rotation,
            Scale = scale
        };
        
        if (_replicaTransforms.Count >= 1)
        {
            var previousReplicaTransform = _replicaTransforms.ElementAt(_replicaTransforms.Count - 1).Value;
            _replicaTranslationDistance += (previousReplicaTransform.Position - position).magnitude;
            _replicaRotationAngle += Quaternion.Angle(previousReplicaTransform.Rotation, rotation);
            _replicaScaleFactor += (previousReplicaTransform.Scale - scale).magnitude;
            Debug.Log($"Distance: {_replicaTranslationDistance}, Angle: {_replicaRotationAngle}, Scale: {_replicaScaleFactor}");
            
            if (previousReplicaTransform.Position != position || previousReplicaTransform.Rotation != rotation || previousReplicaTransform.Scale != scale)
            {
                _replicaTransforms.Add(Time.time - _taskStartTime, replicaTransform);
            }
        }
        else
        {
            _replicaTransforms.Add(Time.time - _taskStartTime, replicaTransform);
        }
    }

    private void Update()
    {
        if (_taskId == 0) return;
        
        var touches = Touch.activeFingers;
        var touchesDictionary = touches.ToDictionary(finger => finger.index, finger => finger.screenPosition);

        if (touches.Count == 0)
        {
            _lastFingerPositions.Clear();
            return;
        }
        else
        {
            _fingerPositions.Add(Time.time - _taskStartTime, touchesDictionary);
        }
        
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
        if (_taskId == 0) return;
        
        var playerTransform = _camera.transform;
        var playerTransformData = new PlayerTransformData
        {
            Position = playerTransform.position,
            Rotation = playerTransform.rotation
        };
        _playerTransforms.Add(Time.time - _taskStartTime, playerTransformData);
        
        if (_playerTransforms.Count > 1)
        {
            var previousPlayerTransform = _playerTransforms.ElementAt(_playerTransforms.Count - 2).Value;
            _headRotation += Quaternion.Angle(previousPlayerTransform.Rotation, playerTransform.rotation);
            _headMovement += (previousPlayerTransform.Position - playerTransform.position).magnitude;
        }
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