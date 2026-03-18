using UnityEngine;

[RequireComponent(typeof(PlayerClickMove))]
public class PlayerHarvestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerClickMove clickMove;

    [Header("Harvest")]
    [SerializeField] private float harvestStateDuration = 0.8f;

    // 아직 정식 상태머신은 아니지만,
    // 현재 상태를 Inspector에서 바로 볼 수 있게 단순 enum으로 둡니다.
    [SerializeField] private HarvestState currentState = HarvestState.Idle;

    private Mushroom _currentHarvestTarget;
    private float _harvestTimer;

    private enum HarvestState
    {
        Idle,
        MovingToTarget,
        Harvesting
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

        if (currentState == HarvestState.Harvesting)
        {
            UpdateHarvesting();
            return;
        }

        Mushroom targetMushroom = clickMove.TargetMushroom;

        if (targetMushroom == null)
        {
            currentState = HarvestState.Idle;
            return;
        }

        currentState = HarvestState.MovingToTarget;

        if (!clickMove.HasReachedMushroom(targetMushroom))
        {
            return;
        }

        BeginHarvest(targetMushroom);
    }

    private void BeginHarvest(Mushroom mushroom)
    {
        _currentHarvestTarget = mushroom;
        _harvestTimer = harvestStateDuration;
        currentState = HarvestState.Harvesting;

        // 채집이 시작되면 이동을 끊고, 우클릭 입력도 잠급니다.
        clickMove.StopImmediately();
        clickMove.ClearTargetMushroom();
        clickMove.InputLocked = true;

        Debug.Log($"Harvest started: {mushroom.name}");
    }

    private void UpdateHarvesting()
    {
        if (_currentHarvestTarget == null)
        {
            EndHarvest();
            return;
        }

        _harvestTimer -= Time.deltaTime;

        if (_harvestTimer > 0f)
        {
            return;
        }

        Debug.Log($"Harvest finished: {_currentHarvestTarget.name}");

        // 아직 진짜 채집 결과 처리 전 단계이므로,
        // 상태 전환까지만 하고 대상은 그대로 둡니다.
        EndHarvest();
    }

    private void EndHarvest()
    {
        _currentHarvestTarget = null;
        currentState = HarvestState.Idle;
        clickMove.InputLocked = false;
    }
}
