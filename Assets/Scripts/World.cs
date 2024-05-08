using System.Collections.Generic;
using Gestures;
using Gestures.Balloon;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

public class World : MonoBehaviour
{
    [SerializeField] private GameObject balloonPrefab;
    [SerializeField] private BalloonMaterialUpdate balloonMaterialUpdate;
    [SerializeField] private Transform balloonParent;
    
    private readonly IDictionary<BalloonPointId, BalloonPoint> _pointsOfInterest =
        new Dictionary<BalloonPointId, BalloonPoint>(new BalloonEqualityComparer());
    
    public void AddPointOfInterest(BalloonPointId balloonPointId)
    {
        if (_pointsOfInterest.ContainsKey(balloonPointId))
        {
            return;
        }
        
        var balloon = Instantiate(balloonPrefab, balloonParent);
        var balloonPoint = balloon.GetComponent<BalloonPoint>();
        balloonPoint.playerId = balloonPointId.playerId;
        balloonPoint.localPosition = balloonPointId.position;
        balloonPoint.transform.SetParent(balloonParent);
        balloonPoint.transform.localPosition = balloonPointId.position;
        var lineRenderer = balloon.transform.GetChild(0);
        lineRenderer.gameObject.SetActive(false);
        _pointsOfInterest.Add(balloonPointId, balloonPoint);
        balloonMaterialUpdate.UpdateBalloonLayer(balloon, balloonPointId.playerId);
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