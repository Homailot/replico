using System;
using Unity.Netcode;
using UnityEngine;

public class Table : NetworkBehaviour
{
    public Transform firstAttach;
    public Transform secondAttach;

    public NetworkVariable<ulong> firstSeat { get; } = new NetworkVariable<ulong>(writePerm: NetworkVariableWritePermission.Server, value: ulong.MaxValue);
    public NetworkVariable<ulong> secondSeat { get; } = new NetworkVariable<ulong>(writePerm: NetworkVariableWritePermission.Server, value: ulong.MaxValue);

    public bool isFirstSeatAvailable => firstSeat.Value == ulong.MaxValue;
    public bool isSecondSeatAvailable => secondSeat.Value == ulong.MaxValue;
    
    public NetworkObject networkObject { get; private set; }

    public void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        firstSeat.OnValueChanged += OnFirstSeatChanged;
        secondSeat.OnValueChanged += OnSecondSeatChanged;
    }
    
    public void AddToTable(ulong playerId, int seat)
    {
        switch (seat)
        {
            case 0:
                firstSeat.Value = playerId;
                break;
            case 1:
                secondSeat.Value = playerId;
                break;
        }
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