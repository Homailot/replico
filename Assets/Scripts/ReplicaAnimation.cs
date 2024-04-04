using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ReplicaAnimation : MonoBehaviour
{
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    // Duration used for single events, such as a swipe being cancelled. In seconds
    [SerializeField] private float duration;
    // Duration used in smaller steps, such as when in the middle of a swipe. In seconds
    [SerializeField] private float steppedDuration;
    [SerializeField] private float startAlpha;
    [SerializeField] private float endAlpha;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Vector3 rotationAxis;
    [SerializeField] private float rotationStart;
    [SerializeField] private float rotationEnd;
    
    private Replica _replica;

    private float _t;
    private readonly Queue<Action> _animationQueue = new Queue<Action>();
    private Action _currentAnimation;
    
    private void Start()
    {
        _replica = GetComponent<Replica>();
        _t = 0.0f;
    }

    private void Update()
    {
        if (_currentAnimation != null || _animationQueue.Count <= 0) return;
        
        Debug.Log("Dequeueing animation");
        _currentAnimation = _animationQueue.Dequeue();
        _currentAnimation();
    }
    
    public void AnimateTo(float t)
    {
        var startTransformPosition = startTransform.position;
        var endTransformPosition = endTransform.position;
        var startTransformScale = startTransform.localScale;
        var endTransformScale = endTransform.localScale;
        var startAngle = rotationStart;
        var endAngle = rotationEnd;
        
        var curveValue = animationCurve.Evaluate(t);
        _replica.GetReplica().transform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, curveValue);
        _replica.GetReplica().transform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, curveValue);
        _replica.GetReplica().transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(startAngle, endAngle, curveValue), rotationAxis);
        
        _t = t;
    }

    private IEnumerator AnimateToCoroutine(float t)
    {
        Debug.Log("Animating from " + _t + " to " + t);
        if (Mathf.Abs(t - _t) < 0.00001f)
        {
            _currentAnimation = null;
            _t = t;
            yield break;
        }
        var currentTime = 0.0f;
        var startTransformPosition = startTransform.position;
        var endTransformPosition = endTransform.position;
        var startTransformScale = startTransform.localScale;
        var endTransformScale = endTransform.localScale;
        var startAngle = rotationStart;
        var endAngle = rotationEnd;

        while (currentTime < steppedDuration)
        {
            currentTime += Time.deltaTime;
            var curveValue = animationCurve.Evaluate(currentTime / steppedDuration);
            curveValue = _t + curveValue * (t - _t);
            Debug.Log("Curve value: " + curveValue);
            
            _replica.GetReplica().transform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, curveValue);
            _replica.GetReplica().transform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, curveValue);
            
            var angle = Mathf.Lerp(startAngle, endAngle, curveValue);
            _replica.GetReplica().transform.rotation = Quaternion.AngleAxis(angle, rotationAxis);
            
            yield return null;
        }
        
        _replica.GetReplica().transform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, t);
        _replica.GetReplica().transform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, t);
        _replica.GetReplica().transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(startAngle, endAngle, t), rotationAxis);
        
        _currentAnimation = null;
        _t = t;
    }
}