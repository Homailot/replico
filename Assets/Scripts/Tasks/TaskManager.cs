using Player;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tasks
{
    public class TaskManager : MonoBehaviour
    {
        [SerializeField] private InputActionReference player1NextTask;
        [SerializeField] private InputActionReference player2NextTask;
        [SerializeField] private NetworkManager networkManager;
       // [SerializeField] private PlayerManager playerManager;
        [SerializeField] private Logger logger;
        
        [SerializeField] private SceneAsset nextScene;
        
        private void Awake()
        {
            DontDestroyOnLoad(transform.gameObject); 
            player1NextTask.action.performed += _ => NextTask(0);
            player2NextTask.action.performed += _ => NextTask(1);
        }
        
        private void NextTask(ulong playerId)
        {
         //   playerManager.ResetAvailablePlayerIds();
          //  playerManager.AddAvailablePlayerId(playerId);
            logger.EnableLogger("city");

            networkManager.StartHost();
            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            player.playerId = playerId;
            networkManager.SceneManager.LoadScene(nextScene.name, LoadSceneMode.Single);
        }
    }
}