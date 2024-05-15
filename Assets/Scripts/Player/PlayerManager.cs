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
        
        private ulong _currentBalloonId = 0;
        private readonly Queue<ulong> _availablePlayerIds = new Queue<ulong>();
        private readonly Dictionary<ulong, ulong> _playerIdToClientId = new Dictionary<ulong, ulong>();
        private readonly Dictionary<ulong, ulong> _clientIdToPlayerId = new Dictionary<ulong, ulong>();
        
        public void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject.GetComponent<PlayerNetwork>();
                OnClientConnected(client.ClientId, playerObject.playerId );
            }
        }
        
        public void ResetAvailablePlayerIds()
        {
            _availablePlayerIds.Clear();
            _playerIdToClientId.Clear();
            _clientIdToPlayerId.Clear();
        }
        
        public void AddAvailablePlayerId(ulong playerId)
        {
            _availablePlayerIds.Enqueue(playerId);
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

        public ulong IncrementAndGetBalloonId()
        {
            if (!NetworkManager.Singleton.IsServer) return 0;
            _currentBalloonId++;
            return _currentBalloonId;
        }
        
        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (_availablePlayerIds.Count == 0) return;
            Debug.Log($"Client {clientId} connected");

            var playerId = _availablePlayerIds.Dequeue();
            OnClientConnected(clientId, playerId);
        }

        private void OnClientConnected(ulong clientId, ulong playerId)
        {
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