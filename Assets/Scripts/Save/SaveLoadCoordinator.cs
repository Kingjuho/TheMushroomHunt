using UnityEngine;

public class SaveLoadCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerClickMove clickMove;
    [SerializeField] private PlayerGoldWallet goldWallet;
    [SerializeField] private PlayerHarvestController harvestController;

    [Header("Save")]
    [SerializeField] private string saveFileName = "save-slot.json";
    [SerializeField] private float loadPositionNavMeshSampleDistance = 2.0f;

    private LocalJsonSaveService _saveService;
    private bool _hasTriedInitialLoad;

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

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        _saveService = new LocalJsonSaveService(saveFileName);
    }

    private void Start()
    {
        TryApplyInitialLoad();
    }

    public void SaveNow()
    {
        // 자동 로드보다 먼저 저장이 실행되면 기존 세이브를 덮어쓸 수 있으므로 차단
        if (!enabled || !_hasTriedInitialLoad)
        {
            return;
        }

        SaveData saveData = new SaveData
        {
            gold = goldWallet.CurrentGold,
            playerPosition = playerTransform.position,
            attackPower = harvestController.AttackPower,
            attacksPerSecond = harvestController.AttacksPerSecond
        };

        _saveService.Save(saveData);

        Debug.Log(
            $"{nameof(SaveLoadCoordinator)}: save completed. Path: {_saveService.SaveFilePath}",
            this);
    }

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

    private void ApplyLoadedData(SaveData loadedData)
    {
        goldWallet.TrySetGold(loadedData.gold);
        harvestController.TrySetCombatStats(loadedData.attackPower, loadedData.attacksPerSecond);

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

    private bool ValidateReferences()
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

    private bool IsLoadedDataValid(SaveData loadedData)
    {
        return loadedData != null
            && loadedData.gold >= 0
            && loadedData.attackPower > 0
            && loadedData.attacksPerSecond > 0f;
    }
}
