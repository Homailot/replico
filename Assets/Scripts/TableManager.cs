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
    
    private Table _table;
    private NetworkObject _tableNetworkObject;
    
    public void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }
    
    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (_table == null)
        {
            var tableGameObject = Instantiate(tablePrefab);
            _table = tableGameObject.GetComponent<Table>();
            _tableNetworkObject = tableGameObject.GetComponent<NetworkObject>();
            
            var tableTransform = _table.transform;
            tableTransform.position = firstTableSpawnPosition.position;
            tableTransform.rotation = firstTableSpawnPosition.rotation;
            
            _tableNetworkObject.Spawn();
        }
        
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] {clientId}
            }
        };
        
        if (_table.isSecondSeatAvailable)
        {
            _table.secondSeat.Value = clientId;
            MovePlayerToTableClientRpc(_tableNetworkObject, 1, clientRpcParams);
        }    
        else if (_table.isFirstSeatAvailable)
        {
            _table.firstSeat.Value = clientId;
            MovePlayerToTableClientRpc(_tableNetworkObject, 0, clientRpcParams);
        }
    
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