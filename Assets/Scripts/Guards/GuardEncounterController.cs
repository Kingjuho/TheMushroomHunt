using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 광대버섯 섬 진입, 30초 경비원 스폰 타이머, 거점 복귀 리셋, 접촉 제재 흐름을 총괄
/// </summary>
public class GuardEncounterController : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerClickMove playerClickMove;
    [SerializeField] private PlayerHarvestController playerHarvestController;
    [SerializeField] private PlayerGoldWallet playerGoldWallet;
    [SerializeField] private SaveLoadCoordinator saveLoadCoordinator;

    [Header("Guard References")]
    [SerializeField] private GuardChaseController guardChaseController;
    [SerializeField] private Transform guardSpawnPoint;
    [SerializeField] private Transform baseReturnPoint;

    [Header("Feedback")]
    [SerializeField] private TMP_Text guardStatusText;
    [SerializeField] private string guardSpawnedMessage = "경비원이 나타났습니다!";
    [SerializeField] private string guardCaughtMessage = "경비원에게 붙잡혔습니다.\n뇌물로 소지금의 절반을 빼앗겼습니다.";
    [SerializeField] private float guardStatusMessageDuration = 1.5f;

    [Header("Timing")]
    [SerializeField] private float guardSpawnDelaySeconds = 30f;
    [SerializeField] private float baseReturnWarpSampleDistance = 2f;

    private bool _isPlayerInsideIslandZone;
    private bool _isGuardActive;
    private float _remainingSpawnDelay;
    private Coroutine _guardStatusCoroutine;

    private void Awake()
    {
        guardSpawnDelaySeconds = Mathf.Max(0.1f, guardSpawnDelaySeconds);
        baseReturnWarpSampleDistance = Mathf.Max(0.1f, baseReturnWarpSampleDistance);
        guardStatusMessageDuration = Mathf.Max(0.1f, guardStatusMessageDuration);

        if (guardStatusText != null)
        {
            ClearGuardStatusText();
        }

        _remainingSpawnDelay = guardSpawnDelaySeconds;

        if (saveLoadCoordinator == null && playerClickMove != null)
        {
            saveLoadCoordinator = playerClickMove.GetComponent<SaveLoadCoordinator>();
        }

        if (!ValidateReferences())
        {
            enabled = false;
        }
    }

    private void OnDisable()
    {
        if (_guardStatusCoroutine != null)
        {
            StopCoroutine(_guardStatusCoroutine);
            _guardStatusCoroutine = null;
        }

        ClearGuardStatusText();
    }

    private void Start()
    {
        ResetEncounterState(resetIslandPresence: true);
    }

    private void Update()
    {
        if (!_isPlayerInsideIslandZone || _isGuardActive)
        {
            return;
        }

        _remainingSpawnDelay = Mathf.Max(0f, _remainingSpawnDelay - Time.deltaTime);

        if (_remainingSpawnDelay > 0f)
        {
            return;
        }

        SpawnGuard();
    }

    /// <summary>
    /// 광대버섯 섬 진입 전용 Trigger에서 플레이어 입장을 통지
    /// 영역 안에 연속으로 머무르는 동안에는 최초 진입만 타이머 시작점으로 취급
    /// </summary>
    public void NotifyIslandZoneEntered(PlayerClickMove player)
    {
        if (!IsTrackedPlayer(player))
        {
            return;
        }

        if (_isPlayerInsideIslandZone)
        {
            return;
        }

        _isPlayerInsideIslandZone = true;

        if (_isGuardActive)
        {
            return;
        }

        _remainingSpawnDelay = guardSpawnDelaySeconds;
    }

    /// <summary>
    /// 경비원 스폰 전 섬 밖으로 나가면 타이머를 초기화
    /// 경비원이 이미 활성화된 뒤에는 이탈만으로는 리셋하지 않고, 거점 복귀만 리셋 조건으로 봄
    /// </summary>
    public void NotifyIslandZoneExited(PlayerClickMove player)
    {
        if (!IsTrackedPlayer(player))
        {
            return;
        }

        if (!_isPlayerInsideIslandZone)
        {
            return;
        }

        _isPlayerInsideIslandZone = false;

        if (_isGuardActive)
        {
            return;
        }

        _remainingSpawnDelay = guardSpawnDelaySeconds;
    }

    /// <summary>
    /// 거점 복귀 Trigger에 들어오면 타이머/경비원 상태를 모두 기본값으로 되돌림
    /// </summary>
    public void NotifyBaseReturnZoneEntered(PlayerClickMove player)
    {
        if (!IsTrackedPlayer(player))
        {
            return;
        }

        ResetEncounterState(resetIslandPresence: true);
    }

    /// <summary>
    /// 경비원 접촉 시 한 번만 호출되는 제재 진입점
    /// 홀수 골드의 절반은 정수 나눗셈 기준으로 내림 처리
    /// </summary>
    public void NotifyGuardContact(PlayerClickMove player)
    {
        if (!IsTrackedPlayer(player))
        {
            return;
        }

        if (!_isGuardActive)
        {
            return;
        }

        _isGuardActive = false;

        bool returnedToBase = ApplyGuardPenaltyAndReturnToBase();

        guardChaseController.ResetToSpawnAndDisable(
            guardSpawnPoint.position,
            guardSpawnPoint.rotation);

        ShowGuardStatus(guardCaughtMessage);

        _isPlayerInsideIslandZone = false;
        _remainingSpawnDelay = guardSpawnDelaySeconds;

        ResumeMainSceneBgm();
        TryAutoSaveAfterGuardPenalty(returnedToBase);
    }

    /// <summary>
    /// 저장/로드 직후 경비원 런타임 상태를 기본값으로 초기화
    /// 경비원 추적 상태는 저장하지 않으므로, load 결과와 무관하게 항상 inactive + 카운트다운 미가동 상태를 강제함
    /// </summary>
    public void ResetRuntimeStateAfterLoad()
    {
        _isPlayerInsideIslandZone = false;
        _isGuardActive = false;
        _remainingSpawnDelay = 0f;

        if (guardChaseController == null || guardSpawnPoint == null)
        {
            return;
        }

        guardChaseController.ResetToSpawnAndDisable(
            guardSpawnPoint.position,
            guardSpawnPoint.rotation);

        ResumeMainSceneBgm();
    }

    private void SpawnGuard()
    {
        if (_isGuardActive)
        {
            return;
        }

        bool spawned = guardChaseController.BeginChase(
            playerClickMove,
            this,
            guardSpawnPoint.position,
            guardSpawnPoint.rotation);

        if (!spawned)
        {
            Debug.LogWarning(
                $"{nameof(GuardEncounterController)}: failed to activate guard. Spawn point may be outside NavMesh.",
                this);

            _remainingSpawnDelay = guardSpawnDelaySeconds;
            return;
        }

        _isGuardActive = true;

        SoundManager.Instance?.PlaySfx(SfxId.GuardSiren);
        SoundManager.Instance?.PlayBgm(BgmId.Guard);

        ShowGuardStatus(guardSpawnedMessage);
    }

    private bool ApplyGuardPenaltyAndReturnToBase()
    {
        int penaltyGoldAmount = playerGoldWallet.CurrentGold / 2;

        if (penaltyGoldAmount > 0)
        {
            playerGoldWallet.TrySpendGold(penaltyGoldAmount);
        }

        playerHarvestController.ForceCancelHarvest(true);
        playerClickMove.ResetMovementState();

        bool returnedToBase = playerClickMove.TryWarpToPosition(
            baseReturnPoint.position,
            baseReturnWarpSampleDistance);

        if (!returnedToBase)
        {
            Debug.LogWarning(
                $"{nameof(GuardEncounterController)}: failed to warp player to base return point. Check NavMesh placement.",
                this);
        }

        return returnedToBase;
    }

    private void TryAutoSaveAfterGuardPenalty(bool returnedToBase)
    {
        // 가정: Guard 패널티 상태가 "확정"된 시점을 거점 Warp 성공 직후로 본다.
        if (!returnedToBase)
        {
            Debug.LogWarning(
                $"{nameof(GuardEncounterController)}: guard penalty auto save skipped because return warp did not complete.",
                this);
            return;
        }

        if (saveLoadCoordinator == null)
        {
            Debug.LogWarning(
                $"{nameof(GuardEncounterController)}: saveLoadCoordinator reference is missing. Guard penalty auto save skipped.",
                this);
            return;
        }

        if (!saveLoadCoordinator.SaveNow())
        {
            Debug.LogWarning(
                $"{nameof(GuardEncounterController)}: guard penalty auto save request failed.",
                this);
        }
    }

    private void ResetEncounterState(bool resetIslandPresence)
    {
        _isGuardActive = false;
        _remainingSpawnDelay = guardSpawnDelaySeconds;

        if (resetIslandPresence)
        {
            _isPlayerInsideIslandZone = false;
        }

        guardChaseController.ResetToSpawnAndDisable(
            guardSpawnPoint.position,
            guardSpawnPoint.rotation);

        ResumeMainSceneBgm();
    }

    private void ResumeMainSceneBgm()
    {
        SoundManager.Instance?.PlayBgm(BgmId.MainScene);
    }

    private bool IsTrackedPlayer(PlayerClickMove player)
    {
        return player != null && player == playerClickMove;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (playerClickMove == null)
        {
            Debug.LogWarning($"{nameof(GuardEncounterController)}: playerClickMove reference is missing.", this);
            isValid = false;
        }

        if (playerHarvestController == null)
        {
            Debug.LogWarning($"{nameof(GuardEncounterController)}: playerHarvestController reference is missing.", this);
            isValid = false;
        }

        if (playerGoldWallet == null)
        {
            Debug.LogWarning($"{nameof(GuardEncounterController)}: playerGoldWallet reference is missing.", this);
            isValid = false;
        }

        if (guardChaseController == null)
        {
            Debug.LogWarning($"{nameof(GuardEncounterController)}: guardChaseController reference is missing.", this);
            isValid = false;
        }

        if (guardSpawnPoint == null)
        {
            Debug.LogWarning($"{nameof(GuardEncounterController)}: guardSpawnPoint reference is missing.", this);
            isValid = false;
        }

        if (baseReturnPoint == null)
        {
            Debug.LogWarning($"{nameof(GuardEncounterController)}: baseReturnPoint reference is missing.", this);
            isValid = false;
        }

        return isValid;
    }

    private void ShowGuardStatus(string message)
    {
        if (guardStatusText == null)
        {
            return;
        }

        if (_guardStatusCoroutine != null)
        {
            StopCoroutine(_guardStatusCoroutine);
        }

        guardStatusText.text = message;
        _guardStatusCoroutine = StartCoroutine(ClearGuardStatusAfterDelay());
    }

    private IEnumerator ClearGuardStatusAfterDelay()
    {
        yield return new WaitForSecondsRealtime(guardStatusMessageDuration);

        ClearGuardStatusText();
        _guardStatusCoroutine = null;
    }

    private void ClearGuardStatusText()
    {
        if (guardStatusText != null)
        {
            guardStatusText.text = string.Empty;
        }
    }
}
