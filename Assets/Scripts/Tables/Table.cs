using System;
using Player;
using Unity.Netcode;
using UnityEngine;

namespace Tables
{
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

            if (IsClient)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer == null) return;
                
                var playerNetwork = localPlayer.GetComponent<PlayerNetwork>();
                if (playerNetwork != null && playerNetwork.gestureDetector != null)
                {
                    playerNetwork.gestureDetector.CreateTable(NetworkObjectId, firstSeat.Value, secondSeat.Value,
                        transform.position, transform.rotation);
                }
            }
        }

        private void Start()
        {
            firstSeat.OnValueChanged += OnFirstSeatChanged;
            secondSeat.OnValueChanged += OnSecondSeatChanged;
        }


        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (!IsClient) return;
            
            var playerNetwork = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            if (playerNetwork != null && playerNetwork.gestureDetector != null)
            {
                playerNetwork.gestureDetector.RemoveTable(NetworkObjectId);
            }
        }

        public bool HasPlayer(ulong playerId)
        {
            return firstSeat.Value == playerId || secondSeat.Value == playerId;
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
    
        public void RemoveFromTable(ulong playerId)
        {
            if (firstSeat.Value == playerId)
            {
                firstSeat.Value = ulong.MaxValue;
            }
            else if (secondSeat.Value == playerId)
            {
                secondSeat.Value = ulong.MaxValue;
            }
        }
    
        private void OnFirstSeatChanged(ulong oldSeat, ulong newSeat)
        {
            Debug.Log($"First seat changed from {oldSeat} to {newSeat}");

            if (!IsClient) return;
            
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            
            if (localPlayer == null) return;
            var playerNetwork = localPlayer.GetComponent<PlayerNetwork>();
            if (playerNetwork == null || playerNetwork.gestureDetector == null) return;
            
            if (newSeat == ulong.MaxValue)
            {
                playerNetwork.gestureDetector.DetachPlayerFromTable(NetworkObjectId, oldSeat);
            }
            else
            {
                playerNetwork.gestureDetector.AttachPlayerToTable(NetworkObjectId, newSeat, 0);
            }
        }
    
        private void OnSecondSeatChanged(ulong oldSeat, ulong newSeat)
        {
            Debug.Log($"Second seat changed from {oldSeat} to {newSeat}");
            
            if (!IsClient) return;
            
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            
            if (localPlayer == null) return;
            var playerNetwork = localPlayer.GetComponent<PlayerNetwork>();
            if (playerNetwork == null || playerNetwork.gestureDetector == null) return;
            
            if (newSeat == ulong.MaxValue)
            {
                Debug.Log($"Detaching player {oldSeat} from table");
                playerNetwork.gestureDetector.DetachPlayerFromTable(NetworkObjectId, oldSeat);
            }
            else
            {
                playerNetwork.gestureDetector.AttachPlayerToTable(NetworkObjectId, newSeat, 1);
            }
        }
    }
}