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

        // NavMeshAgent는 도착 후에도 velocity가 잠깐 남는 경우가 있습니다.
        // 그래서 "속도"보다 먼저 "도착했는가"를 판정하고,
        // 도착 상태면 MoveSpeed를 강제로 0으로 내려 애니메이션 꼬임을 막습니다.
        bool hasArrived =
            !_agent.pathPending &&
            _agent.remainingDistance <= _agent.stoppingDistance + 0.02f &&
            (!_agent.hasPath || _agent.desiredVelocity.sqrMagnitude < 0.0001f);

        float normalizedSpeed = 0f;

        if (!hasArrived && _agent.enabled && _agent.isOnNavMesh && _agent.speed > 0.001f)
        {
            // y값 제거
            Vector3 planarVelocity = _agent.velocity;
            planarVelocity.y = 0f;

            // Animator에는 실제 속도를 그대로 주기보다
            // 0~1 범위로 정규화한 값을 넘겨 상태 전환을 안정화합니다.
            normalizedSpeed = Mathf.Clamp01(planarVelocity.magnitude / _agent.speed);
        }

        // damping을 사용하면 출발/정지 순간 값 변화가 덜 튀고,
        // 위의 도착 판정과 함께 쓰면 정지 전환도 충분히 빠르게 유지됩니다.
        animator.SetFloat(MoveSpeedHash, normalizedSpeed, moveSpeedDampTime, Time.deltaTime);
    }
}