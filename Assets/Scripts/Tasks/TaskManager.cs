using System;
using Player;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tasks
{
    public class TaskManager : MonoBehaviour
    {
        [SerializeField] private InputActionReference player1City;
        [SerializeField] private InputActionReference player2City;
        
        [SerializeField] private InputActionReference player1Rover;
        [SerializeField] private InputActionReference player2Rover;
        
        [SerializeField] private InputActionReference player1Dungeon;
        [SerializeField] private InputActionReference player2Dungeon;
        
        [SerializeField] private InputActionReference nextTask;
        [SerializeField] private NetworkManager networkManager;
       // [SerializeField] private PlayerManager playerManager;
        [SerializeField] private Logger logger;
        
        [SerializeField] private Camera initialCamera;
        [SerializeField] private UIDocument uiDocument;
        
        private TextField _serverIpInput;
        private TextField _serverPortInput;
        private TextField _clientPortInput;
        
        private string _serverIp;
        private ushort _serverPort;
        private ushort _clientPort;
        
        private bool _ipSet;
       
        private Tasks _tasks;
        private bool _loaded;
        
        private void Awake()
        {
            DontDestroyOnLoad(transform.gameObject); 
            
            player1City.action.performed += ctx => LoadCity(0);
            player2City.action.performed += ctx => LoadCity(1);
            
            player1Rover.action.performed += ctx => LoadRover(0);
            player2Rover.action.performed += ctx => LoadRover(1);
            
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
        private void Start()
        {
            var root = uiDocument.rootVisualElement;
            _serverIpInput = root.Q<TextField>("server-ip-input");
            _serverPortInput = root.Q<TextField>("server-port-input");
            _clientPortInput = root.Q<TextField>("local-port-input");
            var setIpButton = root.Q<UnityEngine.UIElements.Button>("set-ip-button");

            setIpButton.RegisterCallback<ClickEvent>(ev =>
            {
                _serverIp = _serverIpInput.value;
                _serverPort = ushort.Parse(_serverPortInput.value);
                _clientPort = ushort.Parse(_clientPortInput.value);
                
                _ipSet = true;
                
                Debug.Log("Server IP: " + _serverIp);
                Debug.Log("Server port: " + _serverPort);
                Debug.Log("Client port: " + _clientPort);

                networkManager.GetComponent<UnityTransport>().SetConnectionData(
                    "0.0.0.0", _clientPort, "0.0.0.0" 
                );
            });
        }
        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            Debug.Log("Scene event: " + sceneEvent.SceneEventType);
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                _tasks = GameObject.FindWithTag("TaskHolder").GetComponent<Tasks>();
                _tasks.SetLogger(logger); 
                _tasks.serverIp = _serverIp;
                _tasks.serverPort = _serverPort;
                _tasks.clientPort = _clientPort;
                _loaded = true;
            }
        }

        private void LoadCity(ulong playerId)
        {
            if (!_ipSet)
            {
                return;
            }
            
            if (_loaded)
            {
                _tasks.SkipTask();
                networkManager.Shutdown();
                //networkManager.OnClientStopped += _ => LoadCity(playerId);
                _loaded = false;
                return;
            } 
            
            if (initialCamera != null)
            {
                initialCamera.gameObject.SetActive(false);
            }
            
            _loaded = false;
            logger.EnableLogger("city");
            networkManager.GetComponent<UnityTransport>().SetConnectionData(
                "0.0.0.0", _clientPort, "0.0.0.0" 
            );
            networkManager.StartHost();
            networkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            
            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            player.playerId = playerId;
            
            networkManager.SceneManager.LoadScene("CityScene", LoadSceneMode.Single);
        }
        
        private void LoadRover(ulong playerId)
        {
            if (!_ipSet)
            {
                return;
            }
            
            if (_loaded)
            {
                _tasks.SkipTask();
                networkManager.Shutdown();
                //networkManager.OnClientStopped += _ => LoadCity(playerId);
                _loaded = false;
                return;
            }
            
            if (initialCamera != null)
            {
                initialCamera.gameObject.SetActive(false);
            }
            
            _loaded = false;
            logger.EnableLogger("rover");
            networkManager.GetComponent<UnityTransport>().SetConnectionData(
                "0.0.0.0", _clientPort, "0.0.0.0" 
            );
            networkManager.StartHost();
            networkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            
            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            player.playerId = playerId;
            
            networkManager.SceneManager.LoadScene("Rover Scene", LoadSceneMode.Single);
        }
        
        private void LoadDungeon(ulong playerId)
        {
            if (!_ipSet)
            {
                return;
            }
            
            if (_loaded)
            {
                _tasks.SkipTask();
                networkManager.Shutdown();
                //networkManager.OnClientStopped += _ => LoadCity(playerId);
                _loaded = false;
                return;
            }
            
            if (initialCamera != null)
            {
                initialCamera.gameObject.SetActive(false);
            }
            
            _loaded = false;
            networkManager.GetComponent<UnityTransport>().SetConnectionData(
                "0.0.0.0", _clientPort, "0.0.0.0" 
            );
            networkManager.StartHost();
            networkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            
            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            player.playerId = playerId;
            
            networkManager.SceneManager.LoadScene("Dungeon Scene", LoadSceneMode.Single);
        }
    }
}