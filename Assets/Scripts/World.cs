using System.Collections.Generic;
using Gestures;
using Gestures.Balloon;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

public class World : MonoBehaviour
{
    [SerializeField] private GameObject worldBalloonSelection;
    [SerializeField] private Transform worldBalloonTransformTarget;
    [SerializeField] private TransformSmoothFollow worldBalloonLineFollow;
    [SerializeField] private GameObject worldBalloonLine;
    [SerializeField] private WorldBalloonHeightToCoordinates balloonHeightToCoordinates;
    [SerializeField] private GameObject balloonPrefab;
    [SerializeField] private BalloonMaterialUpdate balloonMaterialUpdate;
    [SerializeField] private Transform balloonParent;

    [SerializeField] private float balloonScaleMultiplier = 0.045f; 
    
    private readonly IDictionary<BalloonPointId, BalloonPoint> _pointsOfInterest =
        new Dictionary<BalloonPointId, BalloonPoint>(new BalloonEqualityComparer());
    private readonly IDictionary<BalloonPointTempId, BalloonPoint> _tempPointsOfInterest =
        new Dictionary<BalloonPointTempId, BalloonPoint>(new BalloonTempEqualityComparer()); 
    
    public void AddPointOfInterest(BalloonPointTempId balloonPointId)
    {
        if (_tempPointsOfInterest.ContainsKey(balloonPointId))
        {
            return;
        }
        
        var balloon = Instantiate(balloonPrefab, balloonParent);
        var balloonPoint = balloon.GetComponent<BalloonPoint>();
        balloonPoint.playerId = balloonPointId.playerId;
        balloonPoint.localPosition = balloonPointId.position;
        balloonPoint.transform.SetParent(balloonParent);
        balloonPoint.transform.localPosition = balloonPointId.position;

        var indicatorLine = balloonPoint.GetIndicatorLine();
        indicatorLine.DisableLine();
        indicatorLine.DisablePinIndicator();

        var balloonScale = balloonPoint.GetBalloonScale();
        balloonScale.scaleMultiplier = balloonScaleMultiplier;
        
        _tempPointsOfInterest.Add(balloonPointId, balloonPoint);
        balloonMaterialUpdate.UpdateBalloonWorld(balloonPoint, balloonPointId.playerId);
    }

    public void AddPointOfInterest(BalloonPointId id, Vector3 position)
    { 
        if (_pointsOfInterest.ContainsKey(id))
        { 
            return;
        }

        var balloon = Instantiate(balloonPrefab, balloonParent);
        var balloonPoint = balloon.GetComponent<BalloonPoint>();
        balloonPoint.playerId = id.playerId;
        balloonPoint.localPosition = position;
        balloonPoint.id = id.id;
        balloonPoint.transform.SetParent(balloonParent);
        balloonPoint.transform.localPosition = position;

        var indicatorLine = balloonPoint.GetIndicatorLine();
        indicatorLine.DisableLine();
        indicatorLine.DisablePinIndicator();
        indicatorLine.SetBalloonId(id.id.ToString());

        var balloonScale = balloonPoint.GetBalloonScale();
        balloonScale.scaleMultiplier = balloonScaleMultiplier;


        _pointsOfInterest.Add(id, balloonPoint);
        balloonMaterialUpdate.UpdateBalloonWorld(balloonPoint, id.playerId);       
    }

    public void EnableWorldBalloonSelection(ulong playerId)
    {
        var balloonPoint = worldBalloonSelection.GetComponent<BalloonPoint>();
        balloonMaterialUpdate.UpdateBalloonWorld(balloonPoint, playerId);
        worldBalloonLine.SetActive(true);
        worldBalloonSelection.SetActive(true);
    }
    
    public void DisableWorldBalloonSelection()
    {
        worldBalloonLine.SetActive(false);
        worldBalloonSelection.SetActive(false);
        
        balloonHeightToCoordinates.ResetBalloonHeight();
        worldBalloonTransformTarget.localPosition = new Vector3(-1000, -1000, -1000);
        worldBalloonSelection.transform.localPosition = new Vector3(-1000, -1000, -1000);
        worldBalloonLineFollow.UpdateTarget(new Vector3(-1000, -1000, -1000));
        worldBalloonLine.transform.localPosition = new Vector3(-1000, -1000, -1000);
    }
    
    public void UpdateWorldBalloonSelection(float originHeight, Vector3 position)
    {
        worldBalloonTransformTarget.localPosition = position;
        worldBalloonLineFollow.UpdateTarget(new Vector3(worldBalloonTransformTarget.position.x, originHeight, worldBalloonTransformTarget.position.z));

        balloonHeightToCoordinates.SetBalloonHeight(worldBalloonSelection.transform.position.y);
    }
    
    public void UpdateBalloonId(ulong playerIdValue, Vector3 point, ulong id)
    {
        if (!_tempPointsOfInterest.TryGetValue(new BalloonPointTempId(playerIdValue, point), out var balloonPoint)) return;
        balloonPoint.id = id;
        balloonPoint.selectable = false;
        
        var indicatorLine = balloonPoint.GetIndicatorLine();
        indicatorLine.SetBalloonId(id.ToString());
        
        _pointsOfInterest.Add(new BalloonPointId(playerIdValue, id), balloonPoint);
        _tempPointsOfInterest.Remove(new BalloonPointTempId(playerIdValue, point));
    }
    
    public void RemovePointOfInterest(BalloonPointId balloonPointId)
    {
        if (!_pointsOfInterest.ContainsKey(balloonPointId))
        {
            return;
        }
        
        var balloonPoint = _pointsOfInterest[balloonPointId];
        _pointsOfInterest.Remove(balloonPointId);
        Destroy(balloonPoint.gameObject);
    }
}