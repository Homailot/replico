using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TeleportZoneHighlight : MonoBehaviour
{
    [SerializeField] private Material unhighlightedMaterial;
    [SerializeField] private Material highlightedMaterial;
    
    private Material _material;
    private bool _intersects;
    
    private void Start()
    {
        _material = GetComponent<Renderer>().material;
        _material.CopyPropertiesFromMaterial(unhighlightedMaterial);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        _material.CopyPropertiesFromMaterial(highlightedMaterial);
        _intersects = true;
    }
    
    private void OnTriggerExit(Collider other)
    {
        _material.CopyPropertiesFromMaterial(unhighlightedMaterial);
        _intersects = false;
    }
    
    public bool Intersects()
    {
        return _intersects;
    }
}