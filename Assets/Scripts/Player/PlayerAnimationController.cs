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

        bool hasArrived =
            !_agent.pathPending &&
            _agent.remainingDistance <= _agent.stoppingDistance + 0.02f &&
            (!_agent.hasPath || _agent.desiredVelocity.sqrMagnitude < 0.0001f);

        float normalizedSpeed = 0f;

        if (!hasArrived && _agent.enabled && _agent.isOnNavMesh && _agent.speed > 0.001f)
        {
            Vector3 planarVelocity = _agent.velocity;
            planarVelocity.y = 0f;
            normalizedSpeed = Mathf.Clamp01(planarVelocity.magnitude / _agent.speed);
        }

        animator.SetFloat(MoveSpeedHash, normalizedSpeed, moveSpeedDampTime, Time.deltaTime);
    }

    // 공격 시작 시점에만 호출합니다.
    // 실제 피해 적용은 Animation Event에서 처리합니다.
    public void PlayAttack()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(AttackTriggerHash);
        animator.SetTrigger(AttackTriggerHash);
    }
}
