using System;
using UnityEngine;

public class OutlinePulse : MonoBehaviour
{
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseOffset = 0f;
    [SerializeField] private float outlineMin = 1f;
    [SerializeField] private float outlineMax = 5f;
    
    private Outline _outline;

    private void Start()
    {
        _outline = GetComponent<Outline>();
        pulseCurve.postWrapMode = WrapMode.PingPong;
    }
    
    private void Update()
    {
        var pulse = pulseCurve.Evaluate(Time.time * pulseSpeed + pulseOffset);
        _outline.OutlineWidth = Mathf.Lerp(outlineMin, outlineMax, pulse);
    }
}