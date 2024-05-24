using UnityEngine;
using UnityEngine.InputSystem;

public class CursorVisible : MonoBehaviour
{
    [SerializeField] private InputActionReference cursorAction;
    
    private void Start()
    {
        Cursor.visible = false;
        
        cursorAction.action.performed += (_) => Cursor.visible = false;
    } 
}