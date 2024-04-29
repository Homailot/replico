using System;
using System.Collections;
using System.Collections.Generic;
using Player;
using Unity.Netcode;
using UnityEngine;

public class TableManager : NetworkBehaviour
{
    // magic variables for now
    [SerializeField] private Transform firstTableSpawnPosition;
    [SerializeField] private GameObject tablePrefab;
    [SerializeField] private PlayerManager playerManager;
    
    private readonly List<Table> _tables = new List<Table>();
    
    public void AddToAvailableTable(ulong playerId)
    {
        Debug.Log($"Adding player {playerId} to available table");
        if (!NetworkManager.Singleton.IsServer) return;

        var clientId = playerManager.GetClientId(playerId);
        foreach (var table in _tables)
        {
            if (table.isFirstSeatAvailable)
            {
                AddToTable(table, 0, clientId, playerId);
                return;
            }
            
            if (table.isSecondSeatAvailable)
            {
                AddToTable(table, 1, clientId, playerId);
                return;
            }
        }
        
        Debug.Log("Creating new table");
        var tableGameObject = Instantiate(tablePrefab);
        var newTable = tableGameObject.GetComponent<Table>();
        _tables.Add(newTable);
        
        var tableTransform = newTable.transform;
        tableTransform.position = firstTableSpawnPosition.position;
        tableTransform.rotation = firstTableSpawnPosition.rotation;
        
        newTable.networkObject.Spawn();
        
        AddToTable(newTable, 0, clientId, playerId);
    }
    
    public void RemoveFromTable(ulong playerId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        foreach (var table in _tables)
        {
            if (table.firstSeat.Value == playerId)
            {
                Debug.Log($"Removing player {playerId} from table {table.networkObject.NetworkObjectId} in seat 0");
                table.firstSeat.Value = ulong.MaxValue;
                return;
            }
            
            if (table.secondSeat.Value == playerId)
            {
                Debug.Log($"Removing player {playerId} from table {table.networkObject.NetworkObjectId} in seat 1");
                table.secondSeat.Value = ulong.MaxValue;
                return;
            }
        }
        
        // if table is empty, destroy it
    }

    private void AddToTable(Table table, int seat, ulong clientId, ulong playerId)
    {
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] {clientId}
            }
        };
        
        table.AddToTable(playerId, seat);
        MovePlayerToTableClientRpc(table.networkObject, seat, clientRpcParams);
    }

    [ClientRpc]
    private void MovePlayerToTableClientRpc(NetworkObjectReference tableReference, int seat, ClientRpcParams clientRpcParams = default)
    {
        var player = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();

        if (!tableReference.TryGet(out var tableObject)) return;
        
        var table = tableObject.GetComponent<Table>();
        player.MovePlayerToTable(table, seat);
    }
}