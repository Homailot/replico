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
    [SerializeField] private float startAlpha;
    [SerializeField] private float endAlpha;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Vector3 rotationAxis;
    [SerializeField] private float rotationStart;
    [SerializeField] private float rotationEnd;

    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private Vector3 _startScale;
    private Vector3 _endScale;
    
    private Replica _replica;

    private float _t;
    private Coroutine _currentCoroutine;
    
    private void Start()
    {
        _replica = GetComponent<Replica>();
        _t = 0.0f;

        _startPosition = startTransform.position;
        _endPosition = endTransform.position;
        _startScale = startTransform.localScale;
        _endScale = endTransform.localScale;
        AnimateTo(0.0f);
    }
    
    public void SetStartTransform(Transform start)
    {
        _startPosition = start.position;
        _startScale = start.localScale;
    }
    
    public void SetEndTransform(Transform end)
    {
        _endPosition = end.position;
        _endScale = end.localScale;
    }
    
    public void ResetTransforms()
    {
        _startPosition = startTransform.position;
        _endPosition = endTransform.position;
        _startScale = startTransform.localScale;
        _endScale = endTransform.localScale;
    }

    public void AnimateTo(float t)
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }
        
        var startTransformPosition = _startPosition;
        var endTransformPosition = _endPosition;
        var startTransformScale = _startScale;
        var endTransformScale = _endScale;
        var startAngle = rotationStart;
        var endAngle = rotationEnd;
        
        var curveValue = animationCurve.Evaluate(t);
        _replica.GetReplica().transform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, curveValue);
        _replica.GetReplica().transform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, curveValue);
        _replica.GetReplica().transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(startAngle, endAngle, curveValue), rotationAxis);
        
        _t = t;
    }

    public void RevertAnimation(Action onComplete = null)
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }
        
        _currentCoroutine = StartCoroutine(AnimateToCoroutine(0.0f, onComplete));
    }
    
    public void CompleteAnimation(Action onComplete = null)
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }
        
        _currentCoroutine = StartCoroutine(AnimateToCoroutine(1.0f, onComplete));
    }

    private IEnumerator AnimateToCoroutine(float t, Action onComplete = null)
    {
        var currentTime = 0.0f;
        var startTransformPosition = _startPosition;
        var endTransformPosition = _endPosition;
        var startTransformScale = _startScale;
        var endTransformScale = _endScale;
        var startAngle = rotationStart;
        var endAngle = rotationEnd;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            var curveValue = animationCurve.Evaluate(_t + (currentTime / duration) * (t - _t));
            
            _replica.GetReplica().transform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, curveValue);
            _replica.GetReplica().transform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, curveValue);
            
            var angle = Mathf.Lerp(startAngle, endAngle, curveValue);
            _replica.GetReplica().transform.rotation = Quaternion.AngleAxis(angle, rotationAxis);
            
            yield return null;
        }
        
        _replica.GetReplica().transform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, t);
        _replica.GetReplica().transform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, t);
        _replica.GetReplica().transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(startAngle, endAngle, t), rotationAxis);

        _currentCoroutine = null;
        _t = t;
        onComplete?.Invoke();
    }
}