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
        private readonly IList<ulong> _availablePlayerIds = new List<ulong>() { 0, 1 };
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
        public void MovePlayerFromTableToStartPositionServerRpc(ulong playerId)
        {
            var clientId = GetClientId(playerId);
            tableManager.MovePlayerTableToStartPosition(playerId, clientId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void MoveBothPlayersToNewTableServerRpc()
        {
            var clientId1 = GetClientId(0);
            var clientId2 = GetClientId(1);
            
            tableManager.MoveBothPlayersToNewTable(0, clientId1, 1, clientId2);
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

            var playerId = _availablePlayerIds[0];
            OnClientConnected(clientId, playerId);
        }

        private void OnClientConnected(ulong clientId, ulong playerId)
        {
            _availablePlayerIds.Remove(playerId);
            
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
            
            _availablePlayerIds.Add(playerId);
            _playerIdToClientId.Remove(playerId);
            _clientIdToPlayerId.Remove(clientId);
            
            tableManager.RemoveFromTable(playerId);
        }

        public void ResetBalloonId()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            _currentBalloonId = 0;
        }
        
        public void SetBalloonId(ulong balloonId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            _currentBalloonId = balloonId;
        }
    }
}