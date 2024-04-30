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
        [SerializeField] private PointSelected pointSelected;
        [SerializeField] private TeleportSelected teleportSelected;
        [SerializeField] private GameObject balloonPointPrefab;
        [SerializeField] private BalloonMaterialUpdate balloonMaterialUpdate;

        private readonly List<BalloonPoint> _pointsOfInterest = new List<BalloonPoint>();
        private World _world;
        private ulong _playerId = 0;
        
        private static readonly int ActivationTime = Shader.PropertyToID("_ActivationTime");
        private static readonly int FirstHand = Shader.PropertyToID("_First_Hand");
        private static readonly int SecondHand = Shader.PropertyToID("_Second_Hand");
        private static readonly int Disabled = Shader.PropertyToID("_Disabled");
        private static readonly int Activated = Shader.PropertyToID("_Activated");
        private static readonly int ActivatedMin = Shader.PropertyToID("_ActivatedMin");

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
            foreach (var balloonPoint in _pointsOfInterest)
            {
                balloonPoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
            }
        }

        public void SetWorld(World world)
        {
            _world = world;
            gestureConfiguration.replicaController.SetObjectToReplicate(world.gameObject);
        }
        
        public void OnTeleportSelected()
        {
            var localPoint = gestureConfiguration.replicaController.GetReplica().transform.InverseTransformPoint(balloon.position);
            teleportSelected.Invoke(localPoint);
        }

        public void OnPointSelected()
        {
            var localPoint = gestureConfiguration.replicaController.GetReplica().transform.InverseTransformPoint(balloon.position);
            AddPointOfInterest(localPoint, _playerId);
            pointSelected.Invoke(localPoint);
        }
        
        public void AddPointOfInterest(Vector3 position, ulong playerId)
        {
            var balloonPointObject = Instantiate(balloonPointPrefab, balloon.position, Quaternion.identity);
            var balloonPoint = balloonPointObject.GetComponent<BalloonPoint>();
            balloonPointObject.GetComponent<BalloonScale>().enabled = false;
            balloonMaterialUpdate.UpdateBalloonLayer(balloonPointObject, playerId);
            balloonPoint.playerId = playerId;
            balloonPoint.localPosition = position;
            balloonPoint.UpdatePosition(gestureConfiguration.replicaController.GetReplica().transform);
            _pointsOfInterest.Add(balloonPoint);
            _world.AddPointOfInterest(new BalloonPointId(playerId, position));
        }
        
        public void RemovePointOfInterest(Vector3 position)
        {
            var balloonPoint = _pointsOfInterest.FirstOrDefault(point => point.localPosition == position);
            if (balloonPoint == null) return;
            _pointsOfInterest.Remove(balloonPoint);
            Destroy(balloonPoint.gameObject);
        }
        
        public void AddPointSelectedListener(UnityAction<Vector3> action)
        {
            pointSelected.AddListener(action);
        }

        public void AddTeleportSelectedListener(UnityAction<Vector3> action)
        {
            teleportSelected.AddListener(action);
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

        public void EnableBalloon()
        {
            if (balloon == null) return;
            balloon.rotation = Quaternion.identity;
            balloon.gameObject.SetActive(true);
            balloonBillboard.gameObject.SetActive(true);
        }
        
        public void DisableBalloon()
        {
            if (balloon == null) return;
            balloon.gameObject.SetActive(false);
            balloonBillboard.gameObject.SetActive(false);
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
        public class TeleportSelected : UnityEvent<Vector3>
        {
        }
    }
}
