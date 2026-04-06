using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// MainScene의 플레이어에 붙여서
/// 1회 자동 로드와 단일 슬롯 저장 요청을 처리하는 코디네이터.
/// 실제 저장 시점은 Trigger가 결정하고, 이 클래스는 데이터 수집/적용과 저장 완료 피드백을 담당한다.
/// </summary>
public class SaveLoadCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerClickMove clickMove;
    [SerializeField] private PlayerGoldWallet goldWallet;
    [SerializeField] private PlayerHarvestController harvestController;
    [SerializeField] private TMP_Text saveStatusText;
    [SerializeField] private GuardEncounterController guardEncounterController;
    [SerializeField] private InformationPanelPresenter informationPanelPresenter;

    [Header("Save")]
    [SerializeField] private string saveFileName = "save-slot.json";
    [SerializeField] private float loadPositionNavMeshSampleDistance = 2.0f;

    [Header("Feedback")]
    [SerializeField] private string saveCompletedMessage = "저장되었습니다.";
    [SerializeField] private float saveStatusMessageDuration = 1.5f;
    // 즉시 재저장/재로드 방지. 저장 직후 또는 로드 직후 일정 시간 동안은 추가 저장을 막음
    private const float AutoSaveBlockAfterLoadDuration = 0.25f;
    private float _lastSuccessfulLoadRealtime = float.NegativeInfinity;
    public bool IsAutoSaveBlockedAfterLoad
    {
        get => Time.realtimeSinceStartup - _lastSuccessfulLoadRealtime < AutoSaveBlockAfterLoadDuration;
    }

    private LocalJsonSaveService _saveService;
    private bool _hasTriedInitialLoad;
    private Coroutine _saveStatusCoroutine;

    public string SaveFilePath => _saveService != null ? _saveService.SaveFilePath : string.Empty;
    public bool IsReadyToSave => _hasTriedInitialLoad;

    private void Awake()
    {
        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        if (clickMove == null)
        {
            clickMove = GetComponent<PlayerClickMove>();
        }

        if (goldWallet == null)
        {
            goldWallet = GetComponent<PlayerGoldWallet>();
        }

        if (harvestController == null)
        {
            harvestController = GetComponent<PlayerHarvestController>();
        }

        loadPositionNavMeshSampleDistance = Mathf.Max(0.1f, loadPositionNavMeshSampleDistance);
        saveStatusMessageDuration = Mathf.Max(0.1f, saveStatusMessageDuration);

        if (!ValidateRequiredReferences())
        {
            enabled = false;
            return;
        }

        if (saveStatusText == null)
        {
            Debug.LogWarning($"{nameof(SaveLoadCoordinator)}: saveStatusText reference is missing. Save feedback text will not be shown.", this);
        }
        else
        {
            ClearSaveStatusText();
        }

        _saveService = new LocalJsonSaveService(saveFileName);
    }

    private void Start()
    {
        TryApplyInitialLoad();
        StartCoroutine(ResetGuardRuntimeAfterInitialLoad());
    }

    private void OnDisable()
    {
        if (_saveStatusCoroutine != null)
        {
            StopCoroutine(_saveStatusCoroutine);
            _saveStatusCoroutine = null;
        }

        ClearSaveStatusText();
    }

    /// <summary>
    /// 현재 플레이어 상태를 단일 JSON 슬롯에 저장
    /// 초기 자동 로드 시도보다 먼저 저장이 일어나면 기존 세이브를 덮어쓸 수 있으므로, 로드 시도 완료 전에는 저장하지 않음
    /// </summary>
    public bool SaveNow()
    {
        if (!enabled || !_hasTriedInitialLoad)
        {
            return false;
        }

        SaveData saveData = new SaveData
        {
            gold = goldWallet.CurrentGold,
            playerPosition = playerTransform.position,
            attackPower = harvestController.AttackPower,
            attacksPerSecond = harvestController.AttacksPerSecond,
            moveSpeed = clickMove.CurrentMoveSpeed,
            attackPowerUpgradeCount = harvestController.AttackPowerUpgradeCount,
            attackSpeedUpgradeCount = harvestController.AttackSpeedUpgradeCount
        };

        if (!_saveService.TrySave(saveData))
        {
            return false;
        }

        informationPanelPresenter?.ClearSelection();

        ShowSaveCompletedMessage();

        Debug.Log(
            $"{nameof(SaveLoadCoordinator)}: save completed. Path: {_saveService.SaveFilePath}",
            this);

        return true;
    }

    /// <summary>
    /// MainScene 진입 직후 한 번만 저장 파일을 읽어 적용
    /// 저장 파일이 없거나 손상된 경우에는 scene/Inspector 기본값을 그대로 유지
    /// </summary>
    private void TryApplyInitialLoad()
    {
        if (!enabled || _hasTriedInitialLoad)
        {
            return;
        }

        _hasTriedInitialLoad = true;

        if (!_saveService.TryLoad(out SaveData loadedData))
        {
            return;
        }

        if (!IsLoadedDataValid(loadedData))
        {
            Debug.LogWarning($"{nameof(SaveLoadCoordinator)}: loaded save data is invalid. Keeping scene defaults.", this);
            return;
        }

        ApplyLoadedData(loadedData);
    }

    /// <summary>
    /// GuardEncounterController의 Start 초기화보다 나중에 실행되도록 한 프레임 미뤄서
    /// load 성공/실패와 무관하게 경비원 상태가 최종적으로 기본값으로 고정되게 함
    /// 저장 스키마를 늘리지 않고도 "경비원 상태는 저장하지 않는다" 정책을 보장하기 위한 훅
    /// </summary>
    private IEnumerator ResetGuardRuntimeAfterInitialLoad()
    {
        yield return null;

        if (guardEncounterController == null)
        {
            yield break;
        }

        guardEncounterController.ResetRuntimeStateAfterLoad();
    }

    /// <summary>
    /// 저장된 골드, 전투 수치, 이동속도, 위치를 순서대로 적용
    /// 앞단 JSON 검증이 fail-closed로 동작하므로 여기서는 저장된 최종값을 그대로 복원
    /// </summary>
    private void ApplyLoadedData(SaveData loadedData)
    {
        goldWallet.TrySetGold(loadedData.gold);

        bool appliedCombatStats = harvestController.TrySetCombatStats(
            loadedData.attackPower,
            loadedData.attacksPerSecond,
            loadedData.attackPowerUpgradeCount,
            loadedData.attackSpeedUpgradeCount);

        if (!appliedCombatStats)
        {
            Debug.LogWarning(
                $"{nameof(SaveLoadCoordinator)}: failed to apply saved combat progression. Keeping current scene defaults.",
                this);
        }

        if (!clickMove.TrySetMoveSpeed(loadedData.moveSpeed))
        {
            Debug.LogWarning(
                $"{nameof(SaveLoadCoordinator)}: failed to apply saved move speed. Keeping current NavMeshAgent speed.",
                this);
        }

        bool moved = clickMove.TryWarpToPosition(
            loadedData.playerPosition,
            loadPositionNavMeshSampleDistance);

        if (!moved)
        {
            Debug.LogWarning(
                $"{nameof(SaveLoadCoordinator)}: saved position is not on NavMesh. Keeping current scene start position.",
                this);
        }
    }

    /// <summary>
    /// 세이브 코어가 동작하기 위한 필수 참조 검사
    /// 저장 알림 텍스트는 UX용 보조 참조이므로 누락돼도 저장/로드 자체는 막지 않음
    /// </summary>
    private bool ValidateRequiredReferences()
    {
        bool isValid = true;

        if (playerTransform == null)
        {
            Debug.LogWarning($"{nameof(SaveLoadCoordinator)}: playerTransform reference is missing.", this);
            isValid = false;
        }

        if (clickMove == null)
        {
            Debug.LogWarning($"{nameof(SaveLoadCoordinator)}: clickMove reference is missing.", this);
            isValid = false;
        }

        if (goldWallet == null)
        {
            Debug.LogWarning($"{nameof(SaveLoadCoordinator)}: goldWallet reference is missing.", this);
            isValid = false;
        }

        if (harvestController == null)
        {
            Debug.LogWarning($"{nameof(SaveLoadCoordinator)}: harvestController reference is missing.", this);
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// 저장 파일 내 수치의 유효성 검사
    /// </summary>
    private bool IsLoadedDataValid(SaveData loadedData)
    {
        return loadedData != null
            && loadedData.gold >= 0
            && loadedData.attackPower > 0
            && loadedData.attacksPerSecond > 0f
            && loadedData.moveSpeed > 0f
            && loadedData.attackPowerUpgradeCount >= 0
            && loadedData.attackSpeedUpgradeCount >= 0;
    }

    /// <summary>
    /// 저장 성공 문구를 일정 시간 표시
    /// </summary>
    private void ShowSaveCompletedMessage()
    {
        if (saveStatusText == null)
        {
            return;
        }

        if (_saveStatusCoroutine != null)
        {
            StopCoroutine(_saveStatusCoroutine);
        }

        saveStatusText.text = string.IsNullOrWhiteSpace(saveCompletedMessage)
            ? "저장되었습니다."
            : saveCompletedMessage;

        _saveStatusCoroutine = StartCoroutine(ClearSaveStatusAfterDelay());
    }

    private IEnumerator ClearSaveStatusAfterDelay()
    {
        yield return new WaitForSecondsRealtime(saveStatusMessageDuration);

        ClearSaveStatusText();
        _saveStatusCoroutine = null;
    }

    private void ClearSaveStatusText()
    {
        if (saveStatusText != null)
        {
            saveStatusText.text = string.Empty;
        }
    }
}
