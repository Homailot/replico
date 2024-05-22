using System;
using Unity.Netcode;
using UnityEngine;

namespace Tasks
{
    public class CollaborativeTaskNetwork : NetworkBehaviour
    {
        [SerializeField] private FourthTask fourthTask;
        [SerializeField] private FifthTask fifthTask;
        [SerializeField] private Tasks tasks;
        
        [Rpc(SendTo.Everyone)] 
        public void EndFourthTaskRpc(bool success)
        {
            fourthTask.EndTask(success);
        }
        
        [Rpc(SendTo.Everyone)]
        public void EndFifthTaskRpc(bool success)
        {
            fifthTask.EndTask(success);
        }

        public void StartNextTask()
        {
            StartNextTaskRpc(RpcTarget.Not(NetworkManager.Singleton.LocalClientId, RpcTargetUse.Temp));
        }
        
        [Rpc(SendTo.SpecifiedInParams)]
        public void StartNextTaskRpc(RpcParams rpcParams)
        {
            tasks.StartOrSkipTask();
        }
    }
}