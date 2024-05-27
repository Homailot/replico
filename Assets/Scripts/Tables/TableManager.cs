using System.Collections.Generic;
using System.Linq;
using Player;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Tables
{
    public class TableManager : NetworkBehaviour
    {
        // magic variables for now
        [SerializeField] private Transform firstTableSpawnPosition;
        [SerializeField] private GameObject tablePrefab;
        [SerializeField] private PlayerManager playerManager;
    
        private readonly List<Table> _tables = new List<Table>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log("TableManager spawned");
            Debug.Log(_tables.Count);
            
            if (!NetworkManager.Singleton.IsServer) return;
            NetworkManager.Singleton.OnServerStopped += OnShutdown;
        }

        private void OnShutdown(bool a)
        {
            Debug.Log("TableManager shutdown");
            _tables.Clear();
        }

        public void AddToAvailableTable(ulong playerId)
        {
            Debug.Log($"Adding player {playerId} to available table");
            if (!NetworkManager.Singleton.IsServer) return;

            var clientId = playerManager.GetClientId(playerId);
            foreach (var table in _tables)
            {
                Debug.Log($"Checking table {table.networkObject.NetworkObjectId}");
                Debug.Log($"Table first {table.firstSeat.Value} second {table.secondSeat.Value}");
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

            var tableTransform = newTable.GetComponent<TableTransform>();
            newTable.AddToTable(playerId, 0);
            
            newTable.networkObject.Spawn(true);
            tableTransform.SetPositionAndRotation(firstTableSpawnPosition.position, firstTableSpawnPosition.rotation.eulerAngles);
            
            SendToClient(newTable, 0, clientId);
        }
    
        public void RemoveFromTable(ulong playerId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            var table = GetTableWithPlayer(playerId);
            if (table == null) return;
            
            RemoveFromTable(playerId, table);
        }

        private void RemoveFromTable(ulong playerId, Table table)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (table.firstSeat.Value == playerId)
            {
                Debug.Log($"Removing player {playerId} from table {table.networkObject.NetworkObjectId} in seat 0");
                table.firstSeat.Value = ulong.MaxValue;
            }
            
            if (table.secondSeat.Value == playerId)
            {
                Debug.Log($"Removing player {playerId} from table {table.networkObject.NetworkObjectId} in seat 1");
                table.secondSeat.Value = ulong.MaxValue;
            }
            
            // if table is empty, destroy it
            if (table.isFirstSeatAvailable && table.isSecondSeatAvailable)
            {
                Debug.Log($"Destroying table {table.networkObject.NetworkObjectId}");
                _tables.Remove(table);
                table.networkObject.Despawn();
                Destroy(table.gameObject);
            } 
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
            Debug.Log(_tables.Count);
            Debug.Log(table.NetworkObjectId);
            MovePlayerToTableClientRpc(table.networkObject, seat, clientRpcParams); 
        }

        private Table GetTableWithPlayer(ulong playerId)
        {
            return _tables.FirstOrDefault(table => table.HasPlayer(playerId));
        }

        public void MovePlayerToTable(ulong playerId, ulong clientId, ulong tableId)
        {
            var initialTable = GetTableWithPlayer(playerId);
            if (initialTable == null) return;
            
            var table = _tables.FirstOrDefault(t => t.networkObject.NetworkObjectId == tableId);
            if (table == null) return;
            
            RemoveFromTable(playerId, initialTable);
            initialTable.RemoveFromTable(playerId);
            
            if (table.firstSeat.Value == ulong.MaxValue)
            {
                table.AddToTable(playerId, 0);
                SendToClient(table, 0, clientId);
            }
            else if (table.secondSeat.Value == ulong.MaxValue)
            {
                table.AddToTable(playerId, 1);
                SendToClient(table, 1, clientId);
            }
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
                    MoveTableAndPlayer(table, playerId, clientId, 0, position, rotation);
                }
                else
                {
                    CreateNewTableAndMovePlayer(table, playerId, clientId, 0, position, rotation);
                }
            }
        }

        private void MoveTableAndPlayer(Table table, ulong playerId, ulong clientId, int seat, Vector3 position, Quaternion rotation)
        {
            var tableTransform = table.GetComponent<TableTransform>();
            tableTransform.SetPositionAndRotation(position, rotation.eulerAngles);
        }
    
        private void CreateNewTableAndMovePlayer(Table table, ulong playerId, ulong clientId, int seat, Vector3 position, Quaternion rotation)
        {
            table.RemoveFromTable(playerId);
        
            var newTableGameObject = Instantiate(tablePrefab, position, rotation);
            var newTable = newTableGameObject.GetComponent<Table>();
            _tables.Add(newTable);
            newTable.AddToTable(playerId, seat);
        
            newTable.networkObject.Spawn(true);
        
            SendToClient(newTable, seat, clientId);
        } 
        
        public void CreateNewTableWithPlayer(ulong playerId, Vector3 position, Quaternion rotation)
        {
            var newTableGameObject = Instantiate(tablePrefab, position, rotation);
            var newTable = newTableGameObject.GetComponent<Table>();
            _tables.Add(newTable);
            newTable.AddToTable(playerId, 0);
        
            newTable.networkObject.Spawn(true);
        }
        
        public void MoveBothPlayersToNewTable(ulong playerId1, ulong clientId1, ulong playerId2, ulong clientId2)
        {
            var table1 = GetTableWithPlayer(playerId1);
            if (table1 == null) return;
            
            MovePlayerTableToStartPosition(playerId1, clientId1);
            
            var table2 = GetTableWithPlayer(playerId2);
            if (table2 == null) return;
            
            MovePlayerToTable(playerId2, clientId2, table1.networkObject.NetworkObjectId);
        }

        [ClientRpc]
        private void MovePlayerToTableClientRpc(NetworkObjectReference tableReference, int seat, ClientRpcParams clientRpcParams = default)
        {
            var player = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();

            if (!tableReference.TryGet(out var tableObject)) return;
        
            var table = tableObject.GetComponent<Table>();
            player.MovePlayerToTable(table, seat);
        }

        public void MovePlayerTableToStartPosition(ulong playerId, ulong clientId)
        {
            var table = GetTableWithPlayer(playerId);
            if (table == null) return;
            
            if (table.firstSeat.Value == playerId)
            {
                MoveTableAndPlayer(table, playerId, clientId, 0, firstTableSpawnPosition.position, firstTableSpawnPosition.rotation);
            }
            else if (table.secondSeat.Value == playerId)
            {
                MoveTableAndPlayer(table, playerId, clientId, 1, firstTableSpawnPosition.position, firstTableSpawnPosition.rotation);
            }
        }
    }
}