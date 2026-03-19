using UnityEngine;

[RequireComponent(typeof(PlayerClickMove))]
public class PlayerHarvestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerClickMove clickMove;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Combat")]
    [SerializeField] private int attackPower = 10;
    [SerializeField] private float attacksPerSecond = 1.0f;
    [SerializeField] private float attackRotationSpeed = 720f;

    [Header("Debug")]
    [SerializeField] private HarvestState currentState = HarvestState.Idle;

    private Mushroom _currentHarvestTarget;
    private float _attackCooldownTimer;
    private bool _isAttackAnimationPlaying;
    private bool _hasPendingImpact;

    private enum HarvestState
    {
        Idle,
        MovingToTarget,
        Attacking
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
    }

    private void Update()
    {
        if (clickMove == null)
        {
            return;
        }

        if (currentState == HarvestState.Attacking)
        {
            UpdateAttacking();
            return;
        }

        Mushroom targetMushroom = clickMove.TargetMushroom;

        if (targetMushroom == null)
        {
            currentState = HarvestState.Idle;
            return;
        }

        if (!targetMushroom.IsHarvestable)
        {
            clickMove.ClearTargetMushroom();
            currentState = HarvestState.Idle;
            return;
        }

        currentState = HarvestState.MovingToTarget;

        if (!clickMove.HasReachedMushroom(targetMushroom))
        {
            return;
        }

        BeginAttack(targetMushroom);
    }

    private void BeginAttack(Mushroom mushroom)
    {
        _currentHarvestTarget = mushroom;
        _attackCooldownTimer = 0f;
        _isAttackAnimationPlaying = false;
        _hasPendingImpact = false;
        currentState = HarvestState.Attacking;

        clickMove.StopImmediately();
        clickMove.ClearTargetMushroom();
        clickMove.InputLocked = true;
    }

    private void UpdateAttacking()
    {
        if (_currentHarvestTarget == null)
        {
            EndAttack();
            return;
        }

        if (!_currentHarvestTarget.IsHarvestable)
        {
            EndAttack();
            return;
        }

        FaceCurrentTarget();

        _attackCooldownTimer -= Time.deltaTime;

        // 쿨다운이 끝났고 현재 공격 모션이 비어 있을 때만 새 공격을 시작합니다.
        if (_attackCooldownTimer > 0f || _isAttackAnimationPlaying)
        {
            return;
        }

        StartAttackSwing();
    }

    private void StartAttackSwing()
    {
        _isAttackAnimationPlaying = true;
        _hasPendingImpact = true;
        _attackCooldownTimer = 1f / Mathf.Max(0.01f, attacksPerSecond);

        if (animationController != null)
        {
            animationController.PlayAttack();
        }
    }

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

    // Animation Event에서 호출됩니다.
    // 실제 피해 적용은 여기서만 수행합니다.
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
            EndAttack();
            return;
        }

        bool wasHarvested = _currentHarvestTarget.TryTakeDamage(attackPower);

        Debug.Log(
            $"Hit mushroom: {_currentHarvestTarget.name}, " +
            $"damage: {attackPower}, " +
            $"hp: {_currentHarvestTarget.CurrentHp}/{_currentHarvestTarget.MaxHp}");

        if (wasHarvested)
        {
            Debug.Log($"Harvest finished: {_currentHarvestTarget.name}");
            EndAttack();
        }
    }

    // Attack 클립 끝에서 호출됩니다.
    public void OnAttackAnimationFinishedEvent()
    {
        _isAttackAnimationPlaying = false;
    }

    private void EndAttack()
    {
        _currentHarvestTarget = null;
        _isAttackAnimationPlaying = false;
        _hasPendingImpact = false;
        currentState = HarvestState.Idle;
        clickMove.InputLocked = false;
    }
}
