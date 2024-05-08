using System;
using System.Collections.Generic;
using Tables;
using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class PlayerManager : NetworkBehaviour
    {
        [SerializeField] private TableManager tableManager;
        
        private readonly Queue<ulong> _availablePlayerIds = new Queue<ulong>();
        private readonly Dictionary<ulong, ulong> _playerIdToClientId = new Dictionary<ulong, ulong>();
        private readonly Dictionary<ulong, ulong> _clientIdToPlayerId = new Dictionary<ulong, ulong>();
        
        public void Start()
        {
            _availablePlayerIds.Enqueue(0);
            _availablePlayerIds.Enqueue(1);

            Quaternion rotation = new Quaternion(1, 2, 3, 4);
            Debug.Log(rotation);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Debug.Log(inverse);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        public ulong GetClientId(ulong playerId)
        {
            return _playerIdToClientId.GetValueOrDefault(playerId, ulong.MaxValue);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void MovePlayerFromTableToPositionServerRpc(ulong playerId, Vector3 position, Quaternion rotation)
        {
            var clientId = GetClientId(playerId);
            tableManager.MovePlayerTableToPosition(playerId, clientId, position, rotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MovePlayerToTableServerRpc(ulong playerId, ulong tableId)
        {
            var clientId = GetClientId(playerId);
            tableManager.MovePlayerToTable(playerId, clientId, tableId);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (_availablePlayerIds.Count == 0) return;
            Debug.Log($"Client {clientId} connected");

            var playerId = _availablePlayerIds.Dequeue();
            var playerNetworkObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerNetwork = playerNetworkObject.GetComponent<PlayerNetwork>();
            playerNetwork.playerId = playerId;
            _playerIdToClientId[playerId] = clientId;
            _clientIdToPlayerId[clientId] = playerId;
            
            tableManager.AddToAvailableTable(playerId);
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            var playerId = _clientIdToPlayerId.GetValueOrDefault(clientId, ulong.MaxValue);
             
            if (playerId == ulong.MaxValue) return;
            
            _availablePlayerIds.Enqueue(playerId);
            _playerIdToClientId.Remove(playerId);
            _clientIdToPlayerId.Remove(clientId);
            
            tableManager.RemoveFromTable(playerId);
        }
    }
}