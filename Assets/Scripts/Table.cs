using Unity.Netcode;
using UnityEngine;

public class Table : NetworkBehaviour
{
    // TODO: TEMPORARY! USE TRACKER FOR TABLE POSITION, USING MAGIC VARIABLES FOR NOW
    public Transform firstSeatAttach;
    public Transform secondSeatAttach; 
    
    public Transform firstAttach;
    public Transform secondAttach;

    public NetworkVariable<ulong> firstSeat { get; } = new NetworkVariable<ulong>(writePerm: NetworkVariableWritePermission.Server, value: ulong.MaxValue);
    public NetworkVariable<ulong> secondSeat { get; } = new NetworkVariable<ulong>(writePerm: NetworkVariableWritePermission.Server, value: ulong.MaxValue);

    public bool isFirstSeatAvailable => firstSeat.Value == ulong.MaxValue;
    public bool isSecondSeatAvailable => secondSeat.Value == ulong.MaxValue;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        firstSeat.OnValueChanged += OnFirstSeatChanged;
        secondSeat.OnValueChanged += OnSecondSeatChanged;
    }
    
    private void OnFirstSeatChanged(ulong oldSeat, ulong newSeat)
    {
        Debug.Log($"First seat changed from {oldSeat} to {newSeat}");
    }
    
    private void OnSecondSeatChanged(ulong oldSeat, ulong newSeat)
    {
        Debug.Log($"Second seat changed from {oldSeat} to {newSeat}");
    }
}