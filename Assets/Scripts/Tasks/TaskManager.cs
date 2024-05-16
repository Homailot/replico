using System;
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
        [SerializeField] private InputActionReference player1City;
        [SerializeField] private InputActionReference player2City;
        [SerializeField] private InputActionReference player1Dungeon;
        [SerializeField] private InputActionReference player2Dungeon;
        [SerializeField] private InputActionReference nextTask;
        [SerializeField] private NetworkManager networkManager;
       // [SerializeField] private PlayerManager playerManager;
        [SerializeField] private Logger logger;
        
        [SerializeField] private SceneAsset cityScene;
        [SerializeField] private SceneAsset dungeonScene;

        private Tasks _tasks;
        private bool _loaded;
        
        private void Awake()
        {
            DontDestroyOnLoad(transform.gameObject); 
            
            player1City.action.performed += ctx => LoadCity(0);
            player2City.action.performed += ctx => LoadCity(1);
            
            player1Dungeon.action.performed += ctx => LoadDungeon(0);
            player2Dungeon.action.performed += ctx => LoadDungeon(1);
            
            nextTask.action.performed += ctx =>
            {
                if (_loaded)
                {
                    _tasks.StartOrSkipTask();
                }
            };
            
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                _tasks = GameObject.FindWithTag("TaskHolder").GetComponent<Tasks>();
                _tasks.SetLogger(logger); 
                _loaded = true;
            }
        }

        private void LoadCity(ulong playerId)
        {
            if (_loaded)
            {
                _tasks.SkipTask();
                networkManager.Shutdown();
                networkManager.OnClientStopped += _ => LoadCity(playerId);
                _loaded = false;
                return;
            }
            
            _loaded = false;
            logger.EnableLogger("city");
            networkManager.StartHost();
            networkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            
            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            player.playerId = playerId;
            
            networkManager.SceneManager.LoadScene(cityScene.name, LoadSceneMode.Single);
        }
        
        private void LoadDungeon(ulong playerId)
        {
            _loaded = false;
            logger.EnableLogger("dungeon");
            networkManager.StartHost();
            
            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            player.playerId = playerId;
            
            networkManager.SceneManager.LoadScene(dungeonScene.name, LoadSceneMode.Single);
        }
    }
}