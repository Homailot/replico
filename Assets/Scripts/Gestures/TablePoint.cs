using System;
using UnityEngine;

namespace Gestures
{
    public class TablePoint : MonoBehaviour
    {
        [SerializeField] private Transform firstAttach;
        [SerializeField] private Transform secondAttach;
        
        public Vector3 localPosition { get; set; }
        public ulong firstPlayerId { get; set; }
        public ulong secondPlayerId { get; set; }
        
        private GameObject _firstPlayer;
        private GameObject _secondPlayer;

        public void Awake()
        {
            firstPlayerId = ulong.MaxValue;
            secondPlayerId = ulong.MaxValue;
        }
        
        public void UpdatePosition(Transform parent)
        {
            transform.position = parent.TransformPoint(localPosition);
            transform.rotation = parent.rotation;
        }

        public void AttachPlayer(GameObject playerPrefab, ulong playerId, int seat)
        {
            switch (seat)
            {
                case 0:
                    if (_firstPlayer != null)
                    {
                        Destroy(_firstPlayer);
                    }

                    _firstPlayer = Instantiate(playerPrefab, firstAttach);
                    _firstPlayer.transform.localRotation = Quaternion.identity;
                    
                    firstPlayerId = playerId;
                    break;
                case 1:
                    if (_secondPlayer != null)
                    {
                        Destroy(_secondPlayer);
                    }
                    _secondPlayer = Instantiate(playerPrefab, secondAttach);
                    _secondPlayer.transform.localRotation = Quaternion.identity;
                    
                    secondPlayerId = playerId;
                    break;
            }
        } 
        
        public void DetachPlayer(ulong playerId)
        {
            Debug.Log($"Detaching player {playerId}");
            Debug.Log($"First player: {firstPlayerId}");
            Debug.Log($"Second player: {secondPlayerId}");
            if (firstPlayerId == playerId)
            {
                Destroy(_firstPlayer);
                _firstPlayer = null;
                firstPlayerId = ulong.MaxValue;
            }
            else if (secondPlayerId == playerId)
            {
                Destroy(_secondPlayer);
                _secondPlayer = null;
                secondPlayerId = ulong.MaxValue;
            }
        }
    }
}