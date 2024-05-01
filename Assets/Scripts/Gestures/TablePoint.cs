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

                    _firstPlayer = Instantiate(playerPrefab, firstAttach, true);
                    _firstPlayer.transform.localPosition = Vector3.zero;
                    _firstPlayer.transform.localRotation = Quaternion.identity;
                    _firstPlayer.transform.localScale = Vector3.one;
                    
                    firstPlayerId = playerId;
                    break;
                case 1:
                    if (_secondPlayer != null)
                    {
                        Destroy(_secondPlayer);
                    }
                    _secondPlayer = Instantiate(playerPrefab, secondAttach, true);
                    _secondPlayer.transform.localPosition = Vector3.zero;
                    _secondPlayer.transform.localRotation = Quaternion.identity;
                    _secondPlayer.transform.localScale = Vector3.one;
                    
                    secondPlayerId = playerId;
                    break;
            }
        } 
        
        public void DetachPlayer(ulong playerId)
        {
            if (firstPlayerId == playerId)
            {
                _firstPlayer = null;
                firstPlayerId = ulong.MaxValue;
                Destroy(_firstPlayer);
            }
            else if (secondPlayerId == playerId)
            {
                _secondPlayer = null;
                secondPlayerId = ulong.MaxValue;
                Destroy(_secondPlayer);
            }
        }
    }
}