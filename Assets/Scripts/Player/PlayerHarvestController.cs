using UnityEngine;

[RequireComponent(typeof(PlayerClickMove))]
public class PlayerHarvestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerClickMove clickMove;

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

        // 공격 시작 시 경로를 끊고 입력을 잠가,
        // 우클릭 연타로 대상이 흔들리지 않게 합니다.
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
