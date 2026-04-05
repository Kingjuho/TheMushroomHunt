using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Guard Animator의 MoveSpeed 파라미터를 NavMeshAgent 속도에 맞춰 갱신
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GuardAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController runtimeAnimatorController;

    [Header("Animation")]
    [SerializeField] private string moveSpeedParameterName = "MoveSpeed";
    [SerializeField] private float moveSpeedDampTime = 0.05f;

    private NavMeshAgent _agent;
    private int _moveSpeedHash;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        _moveSpeedHash = Animator.StringToHash(
            string.IsNullOrWhiteSpace(moveSpeedParameterName)
                ? "MoveSpeed"
                : moveSpeedParameterName);

        if (animator == null)
        {
            Debug.LogWarning($"{nameof(GuardAnimationController)}: animator reference is missing.", this);
            enabled = false;
            return;
        }

        if (animator.runtimeAnimatorController == null && runtimeAnimatorController != null)
        {
            animator.runtimeAnimatorController = runtimeAnimatorController;
        }

        animator.applyRootMotion = false;
        animator.SetFloat(_moveSpeedHash, 0f);
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.SetFloat(_moveSpeedHash, 0f);
        }
    }

    private void Update()
    {
        if (animator == null || _agent == null)
        {
            return;
        }

        float normalizedSpeed = GetNormalizedMovementSpeed();
        animator.SetFloat(_moveSpeedHash, normalizedSpeed, moveSpeedDampTime, Time.deltaTime);
    }

    private float GetNormalizedMovementSpeed()
    {
        if (HasReachedDestination())
        {
            return 0f;
        }

        if (!_agent.enabled || !_agent.isOnNavMesh || _agent.speed <= 0.001f)
        {
            return 0f;
        }

        Vector3 planarVelocity = _agent.velocity;
        planarVelocity.y = 0f;

        return Mathf.Clamp01(planarVelocity.magnitude / _agent.speed);
    }

    private bool HasReachedDestination()
    {
        return
            !_agent.pathPending &&
            _agent.remainingDistance <= _agent.stoppingDistance + 0.02f &&
            (!_agent.hasPath || _agent.desiredVelocity.sqrMagnitude < 0.0001f);
    }
}
