using System.Linq;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class InactiveTransparency : MonoBehaviour
{
    [Header("Transparency Settings")]
    [SerializeField] private AnimationCurve transparencyCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.15f);
    [SerializeField] private float inactivityThreshold = 5.0f;
    [SerializeField] private float transparencyDuration = 1.0f;
    
    [Header("Opaque Settings")]
    [SerializeField] private AnimationCurve opaqueCurve = AnimationCurve.EaseInOut(0.0f, 0.15f, 1.0f, 1.0f);
    [SerializeField] private float opaqueDuration = 0.5f;
    
    private Material _material;
    private Color _color;
    private float _inactivityTimer;
    private float _activeTimer;
    private float _lastFingerTouch;
    private bool _inactive;
    
    private bool _disappearing = false;

    private void Start()
    {
        _material = GetComponent<Renderer>().material;
        _color = _material.color;
        _inactivityTimer = Time.time;
        _activeTimer = Time.time;
    }

    private void Update()
    {
        if (Touch.activeFingers.Count == 0 && _disappearing == false && Time.time - _lastFingerTouch > inactivityThreshold)
        {
            _disappearing = true;
            _inactivityTimer = Time.time;
        } 
        else if (Touch.activeFingers.Count != 0)
        {
            if (_disappearing)
            {
                _disappearing = false;
                _activeTimer = Time.time;
            }
            _lastFingerTouch = Time.time;
        }
        
        if (_disappearing == false)
        {
            var inactiveT = 1 - Mathf.Clamp01((_activeTimer - _inactivityTimer) /
                                 transparencyDuration);
            var time = (Time.time - _activeTimer) / opaqueDuration + inactiveT;
            if (time < 0) return;
            
            var transparency = opaqueCurve.Evaluate(time);
            _material.color = new Color(_color.r, _color.g, _color.b, transparency);
        }
        else
        {
            var activeT = 1 - Mathf.Clamp01((_inactivityTimer - _activeTimer) /
                               opaqueDuration);
            var time = (Time.time - _inactivityTimer) / transparencyDuration + activeT;
            if (time < 0) return;
            
            var transparency = transparencyCurve.Evaluate(time);
            _material.color = new Color(_color.r, _color.g, _color.b, transparency);
        }
    } 
}