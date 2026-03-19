using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private float moveSpeedDampTime = 0.05f;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");

    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        // 현재 이동 속도를 계산하여 애니메이터 변수에 전달
        float normalizedSpeed = GetNormalizedMovementSpeed();
        animator.SetFloat(MoveSpeedHash, normalizedSpeed, moveSpeedDampTime, Time.deltaTime);
    }

    /// <summary>
    /// 공격 애니메이션 재생
    /// </summary>
    public void PlayAttack()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(AttackTriggerHash);
        animator.SetTrigger(AttackTriggerHash);
    }

    /// <summary>
    /// 현재 속도를 0.0 ~ 1.0 사이의 비율로 계산
    /// </summary>
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
