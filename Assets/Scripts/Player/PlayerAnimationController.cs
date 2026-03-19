using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private float moveSpeedDampTime = 0.05f;

    [Header("Attack Animation")]
    [SerializeField] private float baseAttackAnimationSpeed = 1.0f;
    [SerializeField] private float minAttackAnimationSpeed = 0.25f;
    [SerializeField] private float maxAttackAnimationSpeed = 2.5f;

    [Header("State Detection")]
    [SerializeField] private string attackStateName = "Attack";

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackSpeedMultiplierHash = Animator.StringToHash("AttackSpeedMultiplier");

    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // 시작 시에도 Attack 상태 속도 파라미터가 비어 있지 않도록 기본값 삽입
        SetAttackAnimationSpeedMultiplier(1.0f);
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
    /// 실제 데미지는 Animation Event를 통해 별도로 적용
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
    /// 공격속도 수치에 맞춰 Attack 상태의 재생 속도를 조정
    /// Animator의 Attack 상태는 Speed Multiplier를 이 파라미터에 연결해야 함
    /// </summary>
    public void SetAttackAnimationSpeedMultiplier(float attacksPerSecond)
    {
        if (animator == null)
        {
            return;
        }

        float speedMultiplier = baseAttackAnimationSpeed * attacksPerSecond;
        speedMultiplier = Mathf.Clamp(
            speedMultiplier,
            minAttackAnimationSpeed,
            maxAttackAnimationSpeed);

        animator.SetFloat(AttackSpeedMultiplierHash, speedMultiplier);
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

    /// <summary>
    /// NavMeshAgent가 실질적으로 목적지에 도착했는지 확인
    /// 도착 직후 velocity가 잠깐 남는 문제를 줄이기 위해 path 상태도 함께 확인
    /// </summary>
    private bool HasReachedDestination()
    {
        return
            !_agent.pathPending &&
            _agent.remainingDistance <= _agent.stoppingDistance + 0.02f &&
            (!_agent.hasPath || _agent.desiredVelocity.sqrMagnitude < 0.0001f);
    }

    /// <summary>
    /// Animator가 현재 Attack 상태를 재생 중인지 확인
    /// Attack 종료 이벤트가 누락되더라도 코드를 통해 상태를 복구하기 위한 안전장치
    /// </summary>
    public bool IsPlayingAttackState()
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(attackStateName);
    }
}
