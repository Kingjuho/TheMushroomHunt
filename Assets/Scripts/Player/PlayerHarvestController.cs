using UnityEngine;

[RequireComponent(typeof(PlayerClickMove))]
public class PlayerHarvestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerClickMove clickMove;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerGoldWallet goldWallet;

    [Header("Combat")]
    [SerializeField] private int attackPower = 10;              // 공격력
    [SerializeField] private float attacksPerSecond = 1.0f;     // 공격 속도
    [SerializeField] private float attackRotationSpeed = 720f;  // 회전 속도

    [Header("Debug")]
    // 현재 상태
    [SerializeField] private HarvestState currentState = HarvestState.Idle;

    private Mushroom _currentHarvestTarget;     // 현재 타겟
    private float _attackCooldownTimer;         // 다음 공격 쿨다운
    private bool _isAttackAnimationPlaying;     // 애니메이션 재생 중인지 여부
    private bool _hasPendingImpact;             // 공격 발생 여부

    public Mushroom CurrentHarvestTarget => _currentHarvestTarget;
    public Mushroom DisplayTarget
    {
        get
        {
            if (_currentHarvestTarget != null)
            {
                return _currentHarvestTarget;
            }

            return clickMove != null ? clickMove.TargetMushroom : null;
        }
    }

    private enum HarvestState
    {
        Idle,               // 대기
        MovingToTarget,     // 타겟을 향해 이동
        Attacking           // 공격(채집)
    }

    private void Awake()
    {
        if (clickMove == null)
        {
            clickMove = GetComponent<PlayerClickMove>();
        }

        if (animationController == null)
        {
            animationController = GetComponent<PlayerAnimationController>();
        }

        if (goldWallet == null)
        {
            goldWallet = GetComponent<PlayerGoldWallet>();
        }

        ApplyCurrentAttackAnimationSpeed();
    }

    private void OnValidate()
    {
        attacksPerSecond = Mathf.Max(0.01f, attacksPerSecond);
    }

    private void Update()
    {
        if (clickMove == null) return;

        // 쿨다운 체크
        UpdateAttackCooldownTimer();

        // 현재 공격 중이면 계속 공격
        if (currentState == HarvestState.Attacking)
        {
            UpdateAttacking();
            return;
        }

        TryEnterAttackState();
    }

    /// <summary>
    /// 공격 쿨다운 타이머
    /// </summary>
    private void UpdateAttackCooldownTimer()
    {
        if (_attackCooldownTimer <= 0f)
        {
            return;
        }

        _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
    }

    /// <summary>
    /// 현재 공격속도 수치를 애니메이션 컨트롤러에 반영
    /// 공격속도 강화가 나중에 추가되더라도 이 메서드만 다시 호출하면 됨
    /// </summary>
    private void ApplyCurrentAttackAnimationSpeed()
    {
        if (animationController == null)
        {
            return;
        }

        animationController.SetAttackAnimationSpeedMultiplier(attacksPerSecond);
    }

    /// <summary>
    /// 버섯에게 다가가는 중이거나, 다가갔다면 공격 상태로 돌입할 수 있는지 판별
    /// </summary>
    private void TryEnterAttackState()
    {
        Mushroom targetMushroom = clickMove.TargetMushroom;

        // 버섯이 없을 시 리턴
        if (targetMushroom == null)
        {
            currentState = HarvestState.Idle;
            return;
        }

        // 버섯이 아직 리스폰 전이라면 타겟을 유지한 채 대기
        if (!targetMushroom.IsHarvestable)
        {
            currentState = HarvestState.Idle;
            return;
        }

        currentState = HarvestState.MovingToTarget;

        // 아직 거리가 멀다면 공격 X
        if (!clickMove.HasReachedMushroom(targetMushroom))
        {
            return;
        }

        // 쿨다운 체크
        if (_isAttackAnimationPlaying || _attackCooldownTimer > 0f)
        {
            currentState = HarvestState.Idle;
            return;
        }

        // 도착 시 공격
        BeginAttack(targetMushroom);
    }

    /// <summary>
    /// 공격 전 초기화 함수
    /// </summary>
    private void BeginAttack(Mushroom mushroom)
    {
        _currentHarvestTarget = mushroom;
        _attackCooldownTimer = 0f;
        _isAttackAnimationPlaying = false;
        _hasPendingImpact = false;
        currentState = HarvestState.Attacking;

        // 공격 상태에 들어오는 시점에 현재 공격속도를 애니메이션에도 반영
        ApplyCurrentAttackAnimationSpeed();
        clickMove.StopImmediately();
    }

    /// <summary>
    /// 공격 상태일 때 매 프레임 실행, 쿨타임이 찰 때마다 무기를 휘두름
    /// </summary>
    private void UpdateAttacking()
    {
        // 종료 이벤트가 누락되었어도 Animator 상태를 확인하여 복구
        SyncAttackAnimationState();

        // 공격을 할 수 없는 상태일 경우 중단
        if (!CanContinueAttacking())
        {
            return;
        }

        // 버섯 방향으로 회전
        FaceCurrentTarget();

        // 쿨타임 계산
        if (_attackCooldownTimer > 0f || _isAttackAnimationPlaying)
        {
            return;
        }

        // 공격 시작
        StartAttackSwing();
    }

    /// <summary>
    /// 공격을 해도 되는지 유효성 검사
    /// </summary>
    private bool CanContinueAttacking()
    {
        if (_currentHarvestTarget == null)
        {
            EndAttack();
            return false;
        }

        if (!_currentHarvestTarget.IsHarvestable)
        {
            ExitAttackStatePreserveSwing(clearClickTarget: false);
            return false;
        }

        if (clickMove.TargetMushroom != _currentHarvestTarget)
        {
            EndAttack(clearClickTarget: false);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 애니메이터에게 공격 명령 전달
    /// </summary>
    private void StartAttackSwing()
    {
        _isAttackAnimationPlaying = true;
        _hasPendingImpact = true;

        // 공격 속도에 맞춰 쿨타임을 다시 세팅
        _attackCooldownTimer = 1f / Mathf.Max(0.01f, attacksPerSecond);

        // 공격 중엔 인풋락
        clickMove.InputLocked = true;

        // 공격속도 값이 런타임 중 바뀔 수 있으므로,
        // 매 스윙 시작 시점에도 애니메이션 속도를 다시 맞춰줌
        ApplyCurrentAttackAnimationSpeed();

        if (animationController != null)
        {
            animationController.PlayAttack();
        }
    }

    /// <summary>
    /// 플레이어 모델이 버섯을 바라보도록 회전
    /// </summary>
    private void FaceCurrentTarget()
    {
        if (_currentHarvestTarget == null)
        {
            return;
        }

        Vector3 direction = _currentHarvestTarget.InteractionPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            attackRotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 애니메이션 이벤트 함수, 실제 데미지 발생
    /// </summary>
    public void OnAttackImpactAnimationEvent()
    {
        if (currentState != HarvestState.Attacking)
        {
            return;
        }

        if (!_hasPendingImpact)
        {
            return;
        }

        _hasPendingImpact = false;

        if (_currentHarvestTarget == null || !_currentHarvestTarget.IsHarvestable)
        {
            ExitAttackStatePreserveSwing(clearClickTarget: false);
            return;
        }

        int rewardGold = _currentHarvestTarget.RewardGold;
        bool wasHarvested = _currentHarvestTarget.TryTakeDamage(attackPower);

        Debug.Log(
            $"Hit mushroom: {_currentHarvestTarget.name}, " +
            $"damage: {attackPower}, " +
            $"hp: {_currentHarvestTarget.CurrentHp}/{_currentHarvestTarget.MaxHp}");

        if (!wasHarvested)
        {
            return;
        }

        if (goldWallet != null)
        {
            goldWallet.AddGold(rewardGold);
        }

        Debug.Log(
            $"Harvest finished: {_currentHarvestTarget.name}, " +
            $"reward gold: {rewardGold}");

        // 별도 입력이 없다면 리스폰 후 같은 버섯을 다시 공격
        ExitAttackStatePreserveSwing(clearClickTarget: false);
    }

    /// <summary>
    /// 애니메이션 이벤트 함수, 상태값 변경 및 인풋락 해제
    /// </summary>
    public void OnAttackAnimationFinishedEvent()
    {
        ReleaseAttackLock();
    }

    /// <summary>
    /// 공격을 강제로, 혹은 자연스럽게 끝낼 때 모든 변수를 깔끔하게 초기화
    /// </summary>
    private void EndAttack()
    {
        EndAttack(true);
    }

    private void EndAttack(bool clearClickTarget)
    {
        _currentHarvestTarget = null;
        _isAttackAnimationPlaying = false;
        _hasPendingImpact = false;
        currentState = HarvestState.Idle;
        clickMove.InputLocked = false;

        if (clearClickTarget && clickMove.TargetMushroom != null)
        {
            clickMove.ClearTargetMushroom();
        }
    }

    /// <summary>
    /// Attack 종료 이벤트가 누락되더라도
    /// Animator가 이미 Attack 상태를 벗어났다면 입력 잠금과 상태값을 풀어줌
    /// </summary>
    private void SyncAttackAnimationState()
    {
        if (!_isAttackAnimationPlaying)
        {
            return;
        }

        if (animationController == null)
        {
            return;
        }

        if (animationController.IsPlayingAttackState())
        {
            return;
        }

        ReleaseAttackLock();
    }

    /// <summary>
    /// 현재 타겟에 대한 공격 상태만 종료,
    /// 이미 시작된 스윙의 애니메이션 / 쿨다운은 유지
    /// 버섯 리스폰 직후 공격 모션이 끊기며 공속이 빨라지는 문제를 막기 위한 처리
    /// </summary>
    private void ExitAttackStatePreserveSwing(bool clearClickTarget)
    {
        _currentHarvestTarget = null;
        _hasPendingImpact = false;
        currentState = HarvestState.Idle;

        if (clearClickTarget && clickMove.TargetMushroom != null)
        {
            clickMove.ClearTargetMushroom();
        }
    }

    /// <summary>
    /// 한 번의 공격 모션이 끝났을 때 공통으로 호출하는 정리 함수
    /// </summary>
    private void ReleaseAttackLock()
    {
        _isAttackAnimationPlaying = false;
        clickMove.InputLocked = false;
    }
}
