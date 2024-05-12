using System;
using System.Collections.Generic;
using System.Linq;
using Gestures.Balloon;
using Gestures.ReplicaTransform;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Serialization;
using Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class GestureDetector : MonoBehaviour
    {
        private IGestureState _currentState;
        [SerializeField] private GestureConfiguration gestureConfiguration;
        [SerializeField] private Renderer effectRenderer;
        [SerializeField] private Renderer balloonPlaneRenderer;
        [SerializeField] private Material renderBehindPlaneMaterial;
        [SerializeField] private Transform balloon;
        [SerializeField] private Transform balloonBillboard;
        [SerializeField] private BalloonHeightToCoordinates balloonHeightToCoordinates;
        [SerializeField] private GameObject balloonPointPrefab;
        [SerializeField] private BalloonMaterialUpdate balloonMaterialUpdate;
        [SerializeField] private List<GameObject> playerReplicaPrefabs;
        [SerializeField] private GameObject tableReplicaPrefab;
        [SerializeField] private Transform balloonArrow;
        [SerializeField] private Transform forward;

        private readonly IDictionary<BalloonPointId, BalloonPoint> _pointsOfInterest = new Dictionary<BalloonPointId, BalloonPoint>(new BalloonEqualityComparer());
        private readonly IDictionary<BalloonPointTempId, BalloonPoint> _tempPoints = new Dictionary<BalloonPointTempId, BalloonPoint>(new BalloonTempEqualityComparer());
        private readonly IDictionary<ulong, TablePoint> _tablePoints = new Dictionary<ulong, TablePoint>();
        private World _world;
        private ulong _playerId = 0;
        
        private static readonly int ActivationTime = Shader.PropertyToID("_ActivationTime");
        private static readonly int FirstHand = Shader.PropertyToID("_First_Hand");
        private static readonly int SecondHand = Shader.PropertyToID("_Second_Hand");
        private static readonly int Disabled = Shader.PropertyToID("_Disabled");
        private static readonly int Activated = Shader.PropertyToID("_Activated");
        private static readonly int ActivatedMin = Shader.PropertyToID("_ActivatedMin");
        
        [SerializeField] private PointSelected pointSelected;
        [SerializeField] private TeleportSelected teleportSelected;
        [SerializeField] private PointRemoved pointRemoved;
        [SerializeField] private TableSelected tableSelected;
        
        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            _currentState = new InitialGesture(this, gestureConfiguration);
            _currentState.OnEnter();
        }

        private void Start()
        {
            DisableBalloon();
            ResetBalloonPlanePositionsAndHeight();
        }

        public void LateUpdate()
        {
            foreach (var balloonPoint in _pointsOfInterest.Values)
            {
                balloonPoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
            }

            foreach (var tempPoint in _tempPoints.Values)
            {
                tempPoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
            }
            
            foreach (var tablePoint in _tablePoints.Values)
            {
                tablePoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
            }
        }

        public IReplicaPoint GetReplicaPointFromBalloon()
        {
            foreach (var balloonPoint in _pointsOfInterest.Values)
            {
                if (balloonPoint.Intersects() && balloonPoint.selectable)
                {
                    return balloonPoint;
                }
            } 
            
            foreach (var tablePoint in _tablePoints.Values)
            {
                if (tablePoint.Intersects() && tablePoint.selectable)
                {
                    return tablePoint;
                }
            }
            
            return null;
        }

        public void SetWorld(World world)
        {
            _world = world;
            gestureConfiguration.replicaController.SetObjectToReplicate(world.gameObject);
        }

        public void SetLogger(Logger logger)
        {
            gestureConfiguration.logger = logger;
        }

        public void OnTableSelected(ulong tableId)
        {
            gestureConfiguration.logger.TableJoin();
            tableSelected.Invoke(tableId); 
        }
        
        public void OnTeleportSelected()
        {
            var localPoint = gestureConfiguration.replicaController.GetReplica().transform.InverseTransformPoint(balloon.position);
            var localRotation = gestureConfiguration.replicaController.GetReplica().transform.InverseTransformDirection(balloonArrow.forward);
            var rotation = Quaternion.LookRotation(localRotation, Vector3.up);
            teleportSelected.Invoke(localPoint, rotation);
        }

        public void OnPointSelected()
        {
            var localPoint = gestureConfiguration.replicaController.GetReplica().transform.InverseTransformPoint(balloon.position);
            AddPointOfInterest(localPoint, _playerId);
            pointSelected.Invoke(localPoint);
        }
        
        public void AddPointOfInterest(Vector3 position, ulong playerId)
        {
            var balloonPoint = CreateBalloonPoint(position, playerId);
            balloonPoint.id = ulong.MaxValue;
            balloonPoint.selectable = false;
            
            var indicatorLine = balloonPoint.GetIndicatorLine();
            // TODO: uncomment
            indicatorLine.DisableLine();
            indicatorLine.DisablePinIndicator();
            
            _tempPoints.Add(new BalloonPointTempId(balloonPoint.playerId, balloonPoint.localPosition), balloonPoint);
            _world.AddPointOfInterest(new BalloonPointTempId(playerId, position));
        }

        public void AddPointOfInterest(Vector3 position, ulong playerId, ulong id)
        {
            var balloonPoint = CreateBalloonPoint(position, playerId);
            balloonPoint.id = id;
            balloonPoint.selectable = true; 
            
            var indicatorLine = balloonPoint.GetIndicatorLine();
            indicatorLine.SetBalloonId(id.ToString());
            
            _pointsOfInterest.Add(new BalloonPointId(playerId, id), balloonPoint);
            _world.AddPointOfInterest(new BalloonPointId(playerId, id), position);
        }
        
        private BalloonPoint CreateBalloonPoint(Vector3 position, ulong playerId)
        {
            var balloonPointObject = Instantiate(balloonPointPrefab, balloon.position, Quaternion.identity);
            var balloonPoint = balloonPointObject.GetComponent<BalloonPoint>();
            balloonPointObject.GetComponent<BalloonScale>().enabled = false;
            balloonMaterialUpdate.UpdateBalloonLayer(balloonPointObject, playerId);
            balloonPoint.playerId = playerId;
            balloonPoint.localPosition = position;
            balloonPoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
            
            var indicatorLine = balloonPoint.GetIndicatorLine();
            indicatorLine.SetPlayerId(playerId);
            
            return balloonPoint;
        }
        
        public void UpdateBalloonId(ulong playerIdValue, Vector3 point, ulong id)
        {
            if (!_tempPoints.TryGetValue(new BalloonPointTempId(playerIdValue, point), out var balloonPoint)) return;
            balloonPoint.id = id;
            balloonPoint.selectable = true; 
            
            var indicatorLine = balloonPoint.GetIndicatorLine();
            indicatorLine.SetBalloonId(id.ToString());
            
            _pointsOfInterest.Add(new BalloonPointId(playerIdValue, id), balloonPoint);
            _tempPoints.Remove(new BalloonPointTempId(playerIdValue, point));
            _world.UpdateBalloonId(playerIdValue, point, id);
        }
        
        public void OnPointRemoved(BalloonPoint balloonPoint)
        {
            if (balloonPoint.playerId != _playerId)
            {
                balloonPoint.selectable = false;
                
                var line = balloonPoint.GetIndicatorLine();
                line.DisableLine();
                line.DisablePinIndicator();
                gestureConfiguration.logger.PointAcknowledgement();
                return;
            }
            gestureConfiguration.logger.PointDeletion();
            RemovePointOfInterest(balloonPoint.id, _playerId);
            pointRemoved.Invoke(new BalloonPointId(balloonPoint.playerId, balloonPoint.id));
        }
        
        public void RemovePointOfInterest(ulong balloonId, ulong playerId)
        {
            BalloonPoint balloonPoint = null;
            foreach (var point in _pointsOfInterest)
            {
                if (point.Key.id == balloonId && point.Key.playerId == playerId)
                {
                    balloonPoint = point.Value;
                    break;
                }
            }

            if (balloonPoint == null) return;
            RemovePointOfInterest(new BalloonPointId(balloonPoint.playerId, balloonId), balloonPoint);
        }

        public void RemovePointOfInterest(BalloonPointId balloonPointId, BalloonPoint balloonPoint)
        {
            _pointsOfInterest.Remove(balloonPointId);
            Destroy(balloonPoint.gameObject);
            _world.RemovePointOfInterest(balloonPointId);
        }

        public void CreateTable(ulong tableId, ulong firstPlayerId, ulong secondPlayerId, Vector3 position,
            Quaternion rotation)
        {
            if (!_tablePoints.TryGetValue(tableId, out var tablePoint))
            {
                var tablePointObject = Instantiate(tableReplicaPrefab);
                tablePoint = tablePointObject.GetComponent<TablePoint>();
                _tablePoints.Add(tableId, tablePoint);
            }
            
            tablePoint.localPosition = position;
            tablePoint.localRotation = rotation;
            tablePoint.tableId = tableId;
            tablePoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
            if (tablePoint.firstPlayerId != firstPlayerId)
            {
                tablePoint.AttachPlayer(playerReplicaPrefabs[(int) firstPlayerId % playerReplicaPrefabs.Count], firstPlayerId, 0);
            }
            
            if (tablePoint.secondPlayerId != secondPlayerId)
            {
                tablePoint.AttachPlayer(playerReplicaPrefabs[(int) secondPlayerId % playerReplicaPrefabs.Count], secondPlayerId, 1);
            }
            
            if (tablePoint.firstPlayerId == _playerId || tablePoint.secondPlayerId == _playerId)
            {
                tablePoint.selectable = false;
            }
        }
        
        public void RemoveTable(ulong tableId)
        {
            if (!_tablePoints.Remove(tableId, out var tablePoint)) return;
            Destroy(tablePoint.gameObject);
        }
        
        public void UpdateTablePosition(ulong tableId, Vector3 position, Quaternion rotation)
        {
            if (!_tablePoints.TryGetValue(tableId, out var tablePoint)) return;
            tablePoint.localPosition = position;
            tablePoint.localRotation = rotation;
            tablePoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
        }
        
        public void AttachPlayerToTable(ulong tableId, ulong playerId, int seat)
        {
            if (!_tablePoints.TryGetValue(tableId, out var tablePoint)) return;
            tablePoint.AttachPlayer(playerReplicaPrefabs[(int) playerId % playerReplicaPrefabs.Count], playerId, seat);
            
            if (tablePoint.firstPlayerId == _playerId || tablePoint.secondPlayerId == _playerId)
            {
                tablePoint.selectable = false;
            }
        }
        
        public void DetachPlayerFromTable(ulong tableId, ulong playerId)
        {
            if (!_tablePoints.TryGetValue(tableId, out var tablePoint)) return;
            tablePoint.DetachPlayer(playerId);
            
            if (tablePoint.firstPlayerId != _playerId && tablePoint.secondPlayerId != _playerId)
            {
                tablePoint.selectable = true;
            }
        }
        
        public void AddPointSelectedListener(UnityAction<Vector3> action)
        {
            pointSelected.AddListener(action);
        }
        
        public void AddPointRemovedListener(UnityAction<BalloonPointId> action)
        {
            pointRemoved.AddListener(action);
        }

        public void AddTeleportSelectedListener(UnityAction<Vector3, Quaternion> action)
        {
            teleportSelected.AddListener(action);
        }
        
        public void AddTableSelectedListener(UnityAction<ulong> action)
        {
            tableSelected.AddListener(action);
        }

        public void SetBalloonProgress(float progress)
        {
            balloonMaterialUpdate.SetBalloonProgress(progress);
        }

        public void SetPlayerId(ulong playerId)
        {
            _playerId = playerId;
            balloonMaterialUpdate.UpdateBalloonNone(balloon.gameObject, playerId);
        }

        public void Init()
        {
            gestureConfiguration.replicaController.CompleteAnimation(() =>
                           {
                               gestureConfiguration.replicaController.ResetTransforms();
                               SwitchState(new TransformReplicaInitialState(this, gestureConfiguration));
                           }
                       ); 
        }

        private void Update()
        {
            _currentState.OnUpdate();
        }

        public void OnGestureDetected()
        {
            if (effectRenderer == null) return;
            effectRenderer.material.SetFloat(ActivationTime, Time.time);
            effectRenderer.material.SetInt(Activated, 1);
        }
        
        public void OnGestureExit()
        {
            if (effectRenderer == null) return;
            effectRenderer.material.SetInt(Activated, 0);
            // some sneaky stuff here
            var activatedMin = effectRenderer.material.GetFloat(ActivatedMin);
            effectRenderer.material.SetFloat(ActivationTime, Time.time - activatedMin);
        }

        public void EnableBalloonArrow()
        {
            balloonArrow.gameObject.SetActive(true);
            balloonArrow.rotation = forward.rotation;
        } 
        
        public void RotateBalloonArrow(float yRotation)
        {
            balloonArrow.Rotate(Vector3.up, yRotation);
        }
        
        public void EnableBalloon()
        {
            if (balloon == null) return;
            balloon.rotation = Quaternion.identity;
            balloonBillboard.gameObject.SetActive(true);
        }
        
        public void DisableBalloon()
        {
            if (balloon == null) return;
            balloonBillboard.gameObject.SetActive(false);
            balloon.position = new Vector3(0, 1000, 0);
        }
        
        public void UpdateBalloonPosition(Vector3 position)
        {
            if (balloon == null) return;
            var screenPosition = new Vector2(position.x, position.z);
            balloon.position = gestureConfiguration.touchToPosition.GetTouchPosition(screenPosition);
            balloon.position = new Vector3(balloon.position.x, balloon.position.y + position.y, balloon.position.z);
            
            if (balloonHeightToCoordinates == null) return;
            balloonHeightToCoordinates.SetBalloonHeight(balloon.position.y);
            balloonBillboard.position = new Vector3(balloon.position.x, balloonBillboard.position.y, balloon.position.z);
        }
        
        public void ToggleBalloonPlaneLine(bool active)
        {
            if (balloonPlaneRenderer == null) return;
            balloonPlaneRenderer.material.SetInt(Disabled, active ? 0 : 1);
            renderBehindPlaneMaterial.SetInt(Disabled, active ? 0 : 1);
        }
        
        public void UpdateBalloonPlanePositions(Vector2 firstHand, Vector2 secondHand)
        {
            if (balloonPlaneRenderer == null) return;
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            balloonPlaneRenderer.material.SetVector(FirstHand, firstHand / screenMax);
            balloonPlaneRenderer.material.SetVector(SecondHand, secondHand / screenMax);
            
            renderBehindPlaneMaterial.SetVector(FirstHand, firstHand / screenMax);
            renderBehindPlaneMaterial.SetVector(SecondHand, secondHand / screenMax);
        }
        
        public void ResetBalloonPlanePositionsAndHeight()
        {
            if (balloonPlaneRenderer == null) return;
            balloonPlaneRenderer.material.SetVector(FirstHand, new Vector2(-1f, -1f));
            balloonPlaneRenderer.material.SetVector(SecondHand, new Vector2(-1f, -1f)); 
            renderBehindPlaneMaterial.SetVector(FirstHand, new Vector2(-1f, -1f));
            renderBehindPlaneMaterial.SetVector(SecondHand, new Vector2(-1f, -1f));
            balloonHeightToCoordinates.ResetBalloonHeight();
            balloonMaterialUpdate.SetBalloonProgress(0);
            balloonArrow.gameObject.SetActive(false);
        }

        public void ResetBalloonPlanePositions()
        {
            if (balloonPlaneRenderer == null) return;
            balloonPlaneRenderer.material.SetVector(FirstHand, new Vector2(-1f, -1f));
            balloonPlaneRenderer.material.SetVector(SecondHand, new Vector2(-1f, -1f));
            renderBehindPlaneMaterial.SetVector(FirstHand, new Vector2(-1f, -1f));
            renderBehindPlaneMaterial.SetVector(SecondHand, new Vector2(-1f, -1f));
        }

        public void SwitchState(IGestureState newState)
        {
            _currentState.OnExit();
            _currentState = newState;
            _currentState.OnEnter();
        }
        
        [Serializable]
        public class PointSelected : UnityEvent<Vector3> { }
        
        [Serializable]
        public class PointRemoved : UnityEvent<BalloonPointId> { }

        [Serializable]
        public class TeleportSelected : UnityEvent<Vector3, Quaternion>
        {
        }
        
        [Serializable]
        public class TableSelected : UnityEvent<ulong> {}


    }
}
