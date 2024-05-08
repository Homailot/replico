using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private GameObject _targetIndicatorPrefab;
    
        private Canvas _canvas;
        private Camera _camera;
        private List<TargetIndicator> _targetIndicators = new List<TargetIndicator>();
    
        // Start is called before the first frame update
        private void Start()
        {
            _canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
            _camera = Camera.main;
        }

        // Update is called once per frame
        private void Update()
        {
            foreach (var targetIndicator in _targetIndicators)
            {
                targetIndicator.UpdatePosition();
            } 
        }

        private void AddTargetIndicator(GameObject target)
        {
            var targetIndicator = Instantiate(_targetIndicatorPrefab, _canvas.transform).GetComponent<TargetIndicator>();
            targetIndicator.Initialize(target, _camera, _canvas);
            _targetIndicators.Add(targetIndicator);
        }
    }
}
