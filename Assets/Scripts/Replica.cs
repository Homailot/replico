using System;
using UnityEngine;

public class Replica : MonoBehaviour
{
    [SerializeField] private GameObject objectToReplicate;
    private GameObject _replica;
    private ReplicaAnimation _replicaAnimation;
    
    private void Awake()
    {
        var transform1 = transform;
        _replica = Instantiate(objectToReplicate, transform1.position, transform1.rotation);
        _replica.transform.parent = transform1;
    }

    private void Start()
    {
        _replicaAnimation = GetComponent<ReplicaAnimation>(); 
    }

    public void EnableReplica()
    {
        _replica.SetActive(true);
    }
    
    public void DisableReplica()
    {
        _replica.SetActive(false);
    }
    
    public void SetStartTransform(Transform start)
    {
        _replicaAnimation.SetStartTransform(start);
    }
    
    public void SetEndTransform(Transform end)
    {
        _replicaAnimation.SetEndTransform(end);
    }
    
    public void ResetTransforms()
    {
        _replicaAnimation.ResetTransforms();
    }
    
    public void RevertAnimation(Action onComplete = null)
    {
        _replicaAnimation.RevertAnimation(onComplete);
    }
    
    public void CompleteAnimation(Action onComplete = null)
    {
        _replicaAnimation.CompleteAnimation(onComplete);
    }
    
    public void AnimateTo(float t)
    {
        _replicaAnimation.AnimateTo(t);
    }
    
    public GameObject GetReplica()
    {
        return _replica;
    }
}