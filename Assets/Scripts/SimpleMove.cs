using UnityEngine;
using UnityEngine.AI;

public class SimpleMove : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rayDistance = 3f;
    [SerializeField] private float stoppingDis = .1f;
    [SerializeField] private LayerMask groundLayer;

    private void Start()
    {

    }

    private void Update()
    {
        if (target == null)
            return;

        agent.SetDestination(target.position);
    }

    public bool IsOffMeshLink() => agent.isOnOffMeshLink;
}