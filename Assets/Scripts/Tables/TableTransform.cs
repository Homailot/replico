using System;
using Player;
using Unity.Netcode;
using UnityEngine;

namespace Tables
{
    public class TableTransform : NetworkBehaviour
    {
        private struct TablePositionData : INetworkSerializable
        {
            private float _x, _y, _z;
            private float _yRotation;

            internal Vector3 position
            {
                get => new Vector3(_x, _y, _z);
                set => (_x, _y, _z) = (value.x, value.y, value.z);
            }
            
            internal Vector3 rotation
            {
                get => new Vector3(0, _yRotation, 0);
                set => _yRotation = value.y;
            }
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _x);
                serializer.SerializeValue(ref _y);
                serializer.SerializeValue(ref _z);
                serializer.SerializeValue(ref _yRotation);
            }
        }
        
        
        private Table _table;
        private readonly NetworkVariable<TablePositionData> _positionData =
            new NetworkVariable<TablePositionData>(writePerm: NetworkVariableWritePermission.Server);
        
        public Vector3 position => _positionData.Value.position;
        public Vector3 rotation => _positionData.Value.rotation;
        
        public void SetPositionAndRotation(Vector3 newPosition, Vector3 newRotation)
        {
            _positionData.Value = new TablePositionData
            {
                position = newPosition,
                rotation = newRotation
            };
        }

        private void Start()
        {
            _table = GetComponent<Table>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // TODO: on changed, move players as well
            _positionData.OnValueChanged += OnPositionDataChanged;
        }
        
        private void OnPositionDataChanged(TablePositionData oldData, TablePositionData newData)
        {
            transform.position = newData.position;
            transform.rotation = Quaternion.Euler(newData.rotation);
            foreach (var playerObject in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
            {
                if (playerObject.playerId == _table.firstSeat.Value)
                {
                    playerObject.MovePlayerToTable(_table, 0);
                }
                else if (playerObject.playerId == _table.secondSeat.Value)
                {
                    playerObject.MovePlayerToTable(_table, 1);
                }
            }

            if (!IsClient) return;
            
            var playerNetwork = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            if (playerNetwork != null && playerNetwork.gestureDetector != null)
            {
                Debug.Log(newData.rotation);
                playerNetwork.gestureDetector.UpdateTablePosition(NetworkObjectId, newData.position, Quaternion.Euler(newData.rotation));
            }
        }
    }
}