using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        table.AddToTable(playerId, seat); 
        SendToClient(table, seat, clientId);
    }

    private void SendToClient(Table table, int seat, ulong clientId)
    { 
        var clientRpcParams = new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams 
            {
                TargetClientIds = new[] {clientId} 
            } 
        };
        MovePlayerToTableClientRpc(table.networkObject, seat, clientRpcParams); 
    }
    
    public Table GetTableWithPlayer(ulong playerId)
    {
        return _tables.FirstOrDefault(table => table.HasPlayer(playerId));
    }
    
    public void MovePlayerTableToPosition(ulong playerId, ulong clientId, Vector3 position, Quaternion rotation)
    {
        var table = GetTableWithPlayer(playerId);
        if (table == null) return;

        if (table.firstSeat.Value == playerId)
        {
            if (table.isSecondSeatAvailable)
            {
                MoveTableAndPlayer(table, playerId, clientId, 0, position, rotation);
            }
            else
            {
                CreateNewTableAndMovePlayer(table, playerId, clientId, 0, position, rotation);
            }
        } else if (table.secondSeat.Value == playerId)
        {
            if (table.isFirstSeatAvailable)
            {
                MoveTableAndPlayer(table, playerId, clientId, 1, position, rotation);
            }
            else
            {
                CreateNewTableAndMovePlayer(table, playerId, clientId, 1, position, rotation);
            }
        }
    }

    private void MoveTableAndPlayer(Table table, ulong playerId, ulong clientId, int seat, Vector3 position, Quaternion rotation)
    {
        table.transform.position = position;
        table.transform.rotation = rotation;
        
        SendToClient(table, seat, clientId);
    }
    
    private void CreateNewTableAndMovePlayer(Table table, ulong playerId, ulong clientId, int seat, Vector3 position, Quaternion rotation)
    {
         
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