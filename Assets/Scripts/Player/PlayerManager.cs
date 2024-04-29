using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class PlayerManager : NetworkBehaviour
    {
        [SerializeField] private TableManager tableManager;
        
        private readonly Queue<ulong> _availablePlayerIds = new Queue<ulong>();
        private readonly Dictionary<ulong, ulong> _playerIdToClientId = new Dictionary<ulong, ulong>();
        
        private bool _hostConnected;
        
        public void Start()
        {
            _availablePlayerIds.Enqueue(0);
            _availablePlayerIds.Enqueue(1);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        public ulong GetClientId(ulong playerId)
        {
            return _playerIdToClientId.GetValueOrDefault(playerId, ulong.MaxValue);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (_availablePlayerIds.Count == 0) return;
            if (_hostConnected && clientId == NetworkManager.Singleton.LocalClientId) return;
            Debug.Log($"Client {clientId} connected");

            var playerId = _availablePlayerIds.Dequeue();
            var playerNetworkObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            playerNetworkObject.GetComponent<PlayerNetwork>().PlayerId = playerId;
            _playerIdToClientId[playerId] = clientId;
            
            tableManager.AddToAvailableTable(playerId);
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            var playerNetworkObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerId = playerNetworkObject.GetComponent<PlayerNetwork>().PlayerId;
            _availablePlayerIds.Enqueue(playerId);
            _playerIdToClientId.Remove(playerId);
            
            tableManager.RemoveFromTable(playerId);
        }
    }
}