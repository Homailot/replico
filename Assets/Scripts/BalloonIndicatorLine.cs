using System;
using System.Numerics;
using TMPro;
using UnityEngine;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class BalloonIndicatorLine : MonoBehaviour
{
    [Header("Height Scaling")] [SerializeField]
    private float minHeight;

    [SerializeField] private float maxHeight;
    [SerializeField] private float minDistance;
    [SerializeField] private float maxDistance;

    [Header("Screen Space Restrictions")] [SerializeField]
    private float edgeScreenSpaceMargin;

    [Header("Pin Indicator Settings")] [SerializeField]
    private float pinIndicatorMargin;
    [SerializeField] private float minIndicatorScale;
    [SerializeField] private float maxIndicatorScale;
    [SerializeField] private RectTransform pinIndicator;
    [SerializeField] private TextMeshProUGUI text;

    private LineRenderer _lineRenderer;
    private Camera _camera;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _camera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            return;
        }

        var distance = Vector3.Distance(_camera.transform.position, transform.position);
        var height = Mathf.Lerp(minHeight, maxHeight, Mathf.InverseLerp(minDistance, maxDistance, distance));
        var scale = Mathf.Lerp(minIndicatorScale, maxIndicatorScale, Mathf.InverseLerp(minDistance, maxDistance, distance));
        var margin = Mathf.Lerp(pinIndicatorMargin, pinIndicatorMargin + (maxIndicatorScale - minIndicatorScale) / 2,
            Mathf.InverseLerp(minDistance, maxDistance, distance));
        var endPosition = new Vector3(transform.position.x, transform.position.y + height, transform.position.z);
        var pinIndicatorPosition = new Vector3(0, height + margin, 0);
        var pinScale = new Vector3(scale, scale, scale);
        var textScale = new Vector3(1, 1, 1);
        
        var endScreenPosition = _camera.WorldToScreenPoint(endPosition);

        if (endScreenPosition.y > Screen.height - edgeScreenSpaceMargin * Screen.height)
        {
            var downEndPosition = new Vector3(transform.position.x, transform.position.y - height, transform.position.z);
            var downScreenPosition = _camera.WorldToScreenPoint(downEndPosition);
            
            if (downScreenPosition.y >= edgeScreenSpaceMargin * Screen.height)
            {
                endPosition = downEndPosition;
                pinIndicatorPosition = new Vector3(0, -height - margin, 0);
                pinScale = new Vector3(scale, -scale, scale);
                textScale = new Vector3(1, -1, 1);
            }
        }

        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, endPosition);
        pinIndicator.localPosition = pinIndicatorPosition;
        pinIndicator.localScale = pinScale;
        text.transform.localScale = textScale;
    }

    private float Cotangent(float angle)
    {
        return 1 / Mathf.Tan(angle);
    }

    private void FirstIdea()
    {
             // var truncY = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x,
             //     Screen.height - edgeScreenSpaceMargin * Screen.height,
             //     screenPosition.z));
             // var cameraToLine = transform.position - _camera.transform.position;
             // endPosition = truncY;
 
             // if (truncY - transform.position.y < minHeight)
             // {
             //     var inversePosition = transform.position.y - height;
             //     var screenPositionInverse = _camera.WorldToScreenPoint(new Vector3(transform.position.x, inversePosition, transform.position.z));
             //     
             //     if (screenPositionInverse.y < edgeScreenSpaceMargin * Screen.height)
             //     {
             //         var inverseTruncY = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, edgeScreenSpaceMargin * Screen.height, screenPosition.z)).y;
             //         
             //         var pointScreenPosition = _camera.WorldToScreenPoint(transform.position);
             //         var truncYHeight = Mathf.Abs(pointScreenPosition.y - (Screen.height - edgeScreenSpaceMargin * Screen.height));
             //         var inverseTruncYHeight = Mathf.Abs(pointScreenPosition.y - (edgeScreenSpaceMargin * Screen.height));
             //         
             //         Debug.Log(truncYHeight < inverseTruncYHeight);
             //         endPosition.y = truncYHeight < inverseTruncYHeight ? inverseTruncY : truncY;
             //     }
             //     else
             //     {
             //         endPosition.y = inversePosition;
             //     }
             // }
             // else
             // {
             //     endPosition.y = truncY;
             // }       
    }

    private void OtherIdeas()
    {
        // var ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height - edgeScreenSpaceMargin * Screen.height,0));
        // var forward = new Vector3(_camera.transform.forward.x, 0, _camera.transform.forward.z).normalized;
        // var plane = new Plane(-forward, _camera.transform.position + forward * Vector3.Distance(_camera.transform.position, transform.position));
        // Debug.DrawLine(_camera.transform.position, _camera.transform.position + forward * Vector3.Distance(_camera.transform.position, transform.position), Color.green); 
        // if (plane.Raycast(ray, out var enter))
        // {
        //     var hitPoint = ray.GetPoint(enter);
        //     Debug.DrawLine(_camera.transform.position + forward * Vector3.Distance(_camera.transform.position, transform.position), hitPoint, Color.blue);
        //     endPosition = new Vector3(transform.position.x, hitPoint.y, transform.position.z);
        //     //_lineRenderer.SetPosition(2, endPosition);
        // }
        // Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);
        // //var truncY = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x,
        //                // Screen.height - edgeScreenSpaceMargin * Screen.height,
        //                 //screenEndPosition.z));
        // //endPosition = Vector3.Project(truncY - transform.position, endPosition - transform.position) + transform.position;
        //
        // var ray2 = _camera.ScreenPointToRay(new Vector3(screenEndPosition.x, Screen.height - edgeScreenSpaceMargin * Screen.height, 0));
        // var planeToCamera = transform.position - _camera.transform.position;
        // var plan2Normal = new Vector3(planeToCamera.x, 0, planeToCamera.z).normalized;
        // var plane2 = new Plane(plan2Normal, transform.position);
        // Debug.DrawRay(ray2.origin, ray2.direction * 100, Color.red);
        // if (plane2.Raycast(ray2, out var enter2))
        // {
        //     var hitPoint2 = ray2.GetPoint(enter2);
        //     Debug.DrawLine(transform.position, hitPoint2, Color.red);
        //     var p = new Vector3(transform.position.x, hitPoint2.y, transform.position.z);
        //     _lineRenderer.SetPosition(1, p);
        // }
    }

    private float OnGetY()
    {
        var s = Screen.height - edgeScreenSpaceMargin * Screen.height;
        var f = _camera.fieldOfView;
        var q = Quaternion.Inverse(_camera.transform.rotation);
        var q1 = q.x;
        var q2 = q.y;
        var q3 = q.z;
        var q4 = q.w;
        var x = transform.position.x;
        var y = transform.position.y;
        var z = transform.position.z;
        var c2 = _camera.transform.position.y;
        var h = Screen.height;
        //var ny = _camera.transform.position.y + (2 * s * Mathf.Tan(f * Mathf.Deg2Rad / 2)) / Screen.height -
        //    Mathf.Tan(f * Mathf.Deg2Rad / 2) + 2 * q1 * (q2 * z - q4 * x) - 2 * q3 * q4 * x + 2 * q2 * q3 * z;
        var cot = Cotangent(f * Mathf.Deg2Rad / 2);
        var ny = (c2 * h * cot - 2 * h * q1 * q4 * x * cot - 2 * h * q3 * q4 * x * cot + 2 * h * q1 * q2 * z * cot +
                     2 * h * q2 * q3 * z * cot + 2 * h * q1 * q3 * x + 2 * h * q3 * q4 * x - h * z -
                     4 * q1 * q3 * s * x - 4 * q3 * q4 * s * x + 2 * s * z) /
                 (h * cot + 2 * h * q1 * q2 + 2 * h * q2 * q4 - 4 * q1 * q2 * s - 4 * q2 * q4 * s);

        return ny;
    }
}