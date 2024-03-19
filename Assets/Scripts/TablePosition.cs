using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class TablePosition : MonoBehaviour
{
    [SerializeField] private Transform tableTracker;
    [SerializeField] private Transform tableAttach;
    [SerializeField] private Transform table;
    [SerializeField] private XROrigin xrOrigin;
   
    [SerializeField] private InputAction alignToTable;

    
    private void Start()
    { 
        StartCoroutine(InitialAlignment());
        alignToTable.performed += AlignToTable;
    }
    
    private IEnumerator InitialAlignment()
    {
         yield return new WaitForSeconds(1);
         
         AlignCameraToTable();
    }   
     
    private void AlignToTable(InputAction.CallbackContext obj)
    {
        Debug.Log("hello");
        AlignCameraToTable();
    }

    private void AlignCameraToTable()
    {
//        xrOrigin.MatchOriginUpOriginForward(table.up, table.forward);
        
        var trackerToOrigin = xrOrigin.CameraInOriginSpacePos - tableTracker.position; 
        
        table.rotation = Quaternion.LookRotation(-tableTracker.forward, -tableTracker.up); 
        xrOrigin.MoveCameraToWorldLocation(tableAttach.position + trackerToOrigin);
    }
}
