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

    [Header("Debug")]
    [SerializeField] private HarvestState currentState = HarvestState.Idle;

    private Mushroom _currentHarvestTarget;
    private float _attackTimer;

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
        _attackTimer = 0f;
        currentState = HarvestState.Attacking;

        clickMove.StopImmediately();
        clickMove.ClearTargetMushroom();
        clickMove.InputLocked = true;

        Debug.Log($"Attack started: {mushroom.name}");
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

        float attackInterval = 1f / Mathf.Max(0.01f, attacksPerSecond);
        _attackTimer += Time.deltaTime;

        if (_attackTimer < attackInterval)
        {
            return;
        }

        _attackTimer -= attackInterval;

        // 실제 공격 판정이 나는 틱에 맞춰 공격 애니메이션도 같이 재생합니다.
        if (animationController != null)
        {
            animationController.PlayAttack();
        }

        bool wasHarvested = _currentHarvestTarget.TryTakeDamage(attackPower);

        Debug.Log(
            $"Hit mushroom: {_currentHarvestTarget.name}, " +
            $"damage: {attackPower}, " +
            $"hp: {_currentHarvestTarget.CurrentHp}/{_currentHarvestTarget.MaxHp}");

        if (!wasHarvested)
        {
            return;
        }

        Debug.Log($"Harvest finished: {_currentHarvestTarget.name}");
        EndAttack();
    }

    private void EndAttack()
    {
        _currentHarvestTarget = null;
        currentState = HarvestState.Idle;
        clickMove.InputLocked = false;
    }
}
