using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class TouchController : MonoBehaviour
{
    Dictionary<int, Vector2> lastTouchPositions;

    Vector2 rightHandLastTouchCenter;
    Vector2 leftHandLastTouchCenter;
    int rightHandLastTouchCount;
    int leftHandLastTouchCount;
    float rightHandLastDistance;
    float leftHandLastDistance;
    Dictionary<int, Touch> rightHandTouches;
    Dictionary<int, Touch> leftHandTouches;

    public GameObject rightHandObj;
    public GameObject leftHandObj;

    private bool TAP = false;
    private bool tapWait = false;
    private int tapCounter = 0;
    private DateTime tapTimeStamp;
    public float tapTimeSeconds = 0.3f;

    // Use this for initialization
    void Start()
    {
        lastTouchPositions = new Dictionary<int, Vector2>();

        rightHandLastTouchCount = 0;
        leftHandLastTouchCount = 0;
        rightHandTouches = new Dictionary<int, Touch>();
        leftHandTouches = new Dictionary<int, Touch>();
    }

    // Update is called once per frame
    void Update()
    {
        TAP = false;

        Dictionary<int, Touch> newRightHandTouches = new Dictionary<int, Touch>();
        Dictionary<int, Touch> newLeftHandTouches = new Dictionary<int, Touch>();

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (rightHandTouches.ContainsKey(t.fingerId))
            {
                newRightHandTouches[t.fingerId] = t;
            }
            else if (leftHandTouches.ContainsKey(t.fingerId))
            {
                newLeftHandTouches[t.fingerId] = t;
            }
            else if (t.position.x > (Screen.width / 2))
            {
                newRightHandTouches[t.fingerId] = t;
            }
            else
            {
                newLeftHandTouches[t.fingerId] = t;
            }
        }

        rightHandTouches = new Dictionary<int, Touch>(newRightHandTouches);
        leftHandTouches = new Dictionary<int, Touch>(newLeftHandTouches);

        List<Touch> rightHandTouchesList = rightHandTouches.Values.ToList();
        List<Touch> leftHandTouchesList = leftHandTouches.Values.ToList();

        // Right Hand

        if (rightHandTouchesList.Count > 0)
        {
            if (rightHandTouchesList.Count == 5)
            {
                if (!tapWait)
                {
                    tapWait = true;
                    tapTimeStamp = DateTime.Now;
                }
            }

            // Calc new center
            Vector2 touchCenter = calcCenter(rightHandTouchesList);

            // Calc avg touch distance to touch center
            float avgDistance = calcAvgDistance(rightHandTouchesList, touchCenter);

            // Calc avg touch rotation around touch center
            float avgRotation = calcAvgRotation(rightHandTouchesList, touchCenter, rightHandLastTouchCenter, rightHandLastTouchCount);

            // Calc scale and rotation
            applyTransformations(rightHandObj, rightHandTouchesList.Count, rightHandLastTouchCount, touchCenter, rightHandLastTouchCenter, avgRotation, avgDistance, rightHandLastDistance);

            rightHandLastTouchCenter = touchCenter;
            rightHandLastDistance = avgDistance;
            rightHandObj.SetActive(true);
        }
        else
        {
            rightHandObj.SetActive(false);


            if (tapWait)
            {
                if (DateTime.Now < tapTimeStamp.AddMilliseconds(0.3f * 1000))
                {
                    TAP = true;
                }
                else
                {
                    tapWait = false;
                    tapCounter = 0;
                }
            }
        }

        

        rightHandLastTouchCount = rightHandTouchesList.Count;

        // Left Hand

        if (leftHandTouchesList.Count > 0)
        {
            // Calc new center
            Vector2 touchCenter = calcCenter(leftHandTouchesList);

            // Calc avg touch distance to touch center
            float avgDistance = calcAvgDistance(leftHandTouchesList, touchCenter);

            // Calc avg touch rotation around touch center
            float avgRotation = calcAvgRotation(leftHandTouchesList, touchCenter, leftHandLastTouchCenter, leftHandLastTouchCount);

            // Calc scale and rotation
            applyTransformations(leftHandObj, leftHandTouchesList.Count, leftHandLastTouchCount, touchCenter, leftHandLastTouchCenter, avgRotation, avgDistance, leftHandLastDistance);

            leftHandLastTouchCenter = touchCenter;
            leftHandLastDistance = avgDistance;
            leftHandObj.SetActive(true);
        }
        else
        {
            leftHandObj.SetActive(false);
        }

        leftHandLastTouchCount = leftHandTouchesList.Count;

        // Update last touch positions

        lastTouchPositions.Clear();
        for (int i = 0; i < Input.touchCount; i++)
        {
            lastTouchPositions[Input.GetTouch(i).fingerId] = Input.GetTouch(i).position;
        }

    }

    private Vector2 calcCenter(List<Touch> touches)
    {
        Vector2 tMax = touches[0].position;
        Vector2 tMin = tMax;

        for (int i = 1; i < touches.Count; i++)
        {
            Vector2 tPos = touches[i].position;
            tMax = new Vector2(Mathf.Max(tMax.x, tPos.x), Mathf.Max(tMax.y, tPos.y));
            tMin = new Vector2(Mathf.Min(tMin.x, tPos.x), Mathf.Min(tMin.y, tPos.y));
        }

        return (tMin + tMax) / 2.0f;
    }

    private float calcAvgDistance(List<Touch> touches, Vector2 center)
    {
        float avgDistance = 0;
        for (int i = 0; i < touches.Count; i++)
        {
            avgDistance += (center - touches[i].position).magnitude;
        }
        avgDistance /= (float)touches.Count;

        return avgDistance;
    }

    private float calcAvgRotation(List<Touch> touches, Vector2 center, Vector2 lastCenter, int lastTouchCount)
    {
        float avgRotation = 0;
        if (lastTouchCount == touches.Count && touches.Count > 1)
        {
            for (int i = 0; i < touches.Count; i++)
            {
                Vector2 oldDir = lastTouchPositions[touches[i].fingerId] - lastCenter;
                Vector2 newDir = touches[i].position - center;
                float angle = Vector2.Angle(oldDir, newDir);
                if (Vector3.Cross(oldDir, newDir).z < 0) angle = -angle;
                avgRotation += angle;
            }
            avgRotation /= (float)touches.Count;
        }

        return avgRotation;
    }

    private void applyTransformations(GameObject gameObject, int touchCount, int lastTouchCount, Vector2 touchCenter, Vector2 lastTouchCenter, float avgRotation, float avgDistance, float lastAvgDistance)
    {
        if (lastTouchCount == touchCount)
        {
            // scale

            if (touchCount > 1)
            {
                float scale = avgDistance / lastAvgDistance;
                gameObject.transform.localScale *= scale;
            }

            // rotate

            gameObject.transform.Rotate(0, 0, avgRotation);
        }

        if (lastTouchCount == 0)
        {
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localPosition = Vector3.zero;// new Vector3((touchCenter.x / (float)Screen.height - ((Screen.width / (float)Screen.height) / 2.0f)), touchCenter.y / (float)Screen.height - 0.5f, 0) * 2.0f;
        }
        else if (lastTouchCount == touchCount)
        {
            Vector2 touchCenterDelta = touchCenter - lastTouchCenter;
            gameObject.transform.localPosition += new Vector3(touchCenterDelta.x / (float)Screen.height, touchCenterDelta.y / (float)Screen.height, 0) * 2.0f;
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 500;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), (TAP ? "TAP" : ""), style);
    }
}
