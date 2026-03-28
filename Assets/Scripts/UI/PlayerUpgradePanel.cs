using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUpgradePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGoldWallet goldWallet;                  // 골드 보유량
    [SerializeField] private PlayerHarvestController harvestController;    // 전투 수치 변경 대상

    [Header("UI - Common")]
    [SerializeField] private Button closeButton;                           // 패널 닫기 버튼

    [Header("UI - Attack Power")]
    [SerializeField] private TMP_Text attackPowerLabelText;                // "공격력"
    [SerializeField] private TMP_Text attackPowerValueText;                // "현재 10 -> 11"
    [SerializeField] private TMP_Text attackPowerCostText;                 // "100G"
    [SerializeField] private Button attackPowerUpgradeButton;              // 공격력 강화 버튼

    [Header("UI - Attack Speed")]
    [SerializeField] private TMP_Text attackSpeedLabelText;                // "공격 속도"
    [SerializeField] private TMP_Text attackSpeedValueText;                // "현재 1.00 -> 1.05"
    [SerializeField] private TMP_Text attackSpeedCostText;                 // "100G"
    [SerializeField] private Button attackSpeedUpgradeButton;              // 공격속도 강화 버튼

    [Header("Balance")]
    [SerializeField] private int attackPowerUpgradeAmount = 1;             // 공격력 1회 강화량
    [SerializeField] private float attackSpeedUpgradeAmount = 0.05f;       // 공격속도 1회 강화량
    [SerializeField] private int attackPowerUpgradeCost = 50;              // 공격력 강화 비용
    [SerializeField] private int attackSpeedUpgradeCost = 50;              // 공격속도 강화 비용

    private void Awake()
    {
        // 패널은 자동 탐색보다 Inspector 수동 연결이 안전합니다.
        // 연결 누락이 있으면 바로 비활성화해 조용히 잘못 동작하는 일을 막습니다.
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // 골드와 전투 수치가 바뀌면 패널 내용도 즉시 갱신되도록 이벤트에 구독합니다.
        goldWallet.GoldChanged += HandleGoldChanged;
        harvestController.CombatStatsChanged += HandleCombatStatsChanged;

        closeButton.onClick.AddListener(HandleCloseClicked);
        attackPowerUpgradeButton.onClick.AddListener(HandleAttackPowerUpgradeClicked);
        attackSpeedUpgradeButton.onClick.AddListener(HandleAttackSpeedUpgradeClicked);

        RefreshAll();
    }

    private void OnDisable()
    {
        if (goldWallet != null)
        {
            goldWallet.GoldChanged -= HandleGoldChanged;
        }

        if (harvestController != null)
        {
            harvestController.CombatStatsChanged -= HandleCombatStatsChanged;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        if (attackPowerUpgradeButton != null)
        {
            attackPowerUpgradeButton.onClick.RemoveListener(HandleAttackPowerUpgradeClicked);
        }

        if (attackSpeedUpgradeButton != null)
        {
            attackSpeedUpgradeButton.onClick.RemoveListener(HandleAttackSpeedUpgradeClicked);
        }
    }

    /// <summary>
    /// 패널을 외부에서 열 때 사용할 함수
    /// 현재 수치를 먼저 갱신한 뒤 표시
    /// </summary>
    public void OpenPanel()
    {
        RefreshAll();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 패널을 닫을 때 사용할 함수
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 필수 참조가 비어 있으면 경고를 출력하고 패널을 비활성화
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (goldWallet == null)
        {
            Debug.LogWarning($"{nameof(PlayerUpgradePanel)}: goldWallet reference is missing.", this);
            isValid = false;
        }

        if (harvestController == null)
        {
            Debug.LogWarning($"{nameof(PlayerUpgradePanel)}: harvestController reference is missing.", this);
            isValid = false;
        }

        if (closeButton == null)
        {
            Debug.LogWarning($"{nameof(PlayerUpgradePanel)}: closeButton reference is missing.", this);
            isValid = false;
        }

        if (attackPowerLabelText == null || attackPowerValueText == null || attackPowerCostText == null || attackPowerUpgradeButton == null)
        {
            Debug.LogWarning($"{nameof(PlayerUpgradePanel)}: attack power row reference is missing.", this);
            isValid = false;
        }

        if (attackSpeedLabelText == null || attackSpeedValueText == null || attackSpeedCostText == null || attackSpeedUpgradeButton == null)
        {
            Debug.LogWarning($"{nameof(PlayerUpgradePanel)}: attack speed row reference is missing.", this);
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// 골드가 변했을 때 버튼 활성화 여부와 비용 표시를 다시 갱신
    /// </summary>
    private void HandleGoldChanged(int currentGold, int delta)
    {
        RefreshButtons();
        RefreshCosts();
    }

    /// <summary>
    /// 전투 수치가 변했을 때 현재 수치 표시를 다시 갱신
    /// </summary>
    private void HandleCombatStatsChanged()
    {
        RefreshStats();
        RefreshButtons();
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 패널을 닫음
    /// </summary>
    private void HandleCloseClicked()
    {
        ClosePanel();
    }

    /// <summary>
    /// 공격력 강화 버튼 클릭 처리
    /// 골드가 충분할 때만 차감 후 수치를 올림
    /// </summary>
    private void HandleAttackPowerUpgradeClicked()
    {
        if (!goldWallet.TrySpendGold(attackPowerUpgradeCost))
        {
            return;
        }

        harvestController.AddAttackPower(attackPowerUpgradeAmount);
    }

    /// <summary>
    /// 공격속도 강화 버튼 클릭 처리
    /// 공격속도는 PlayerHarvestController 내부에서 애니메이션 속도와 함께 갱신
    /// </summary>
    private void HandleAttackSpeedUpgradeClicked()
    {
        if (!goldWallet.TrySpendGold(attackSpeedUpgradeCost))
        {
            return;
        }

        harvestController.AddAttackSpeed(attackSpeedUpgradeAmount);
    }

    /// <summary>
    /// 패널 전체를 한 번에 갱신
    /// </summary>
    private void RefreshAll()
    {
        RefreshLabels();
        RefreshStats();
        RefreshCosts();
        RefreshButtons();
    }

    /// <summary>
    /// 각 행의 라벨 텍스트를 고정 문구로 갱신
    /// 씬에서 직접 적어도 되지만, 스크립트에서 관리하면 프리팹 복제 시 누락이 줄어듦
    /// </summary>
    private void RefreshLabels()
    {
        attackPowerLabelText.text = "공격력";
        attackSpeedLabelText.text = "공격 속도";
    }

    /// <summary>
    /// 현재 수치와 강화 후 수치를 함께 표시
    /// 현재 패널 디자인에 맞춰 "현재 X -> Y" 형식으로 출력
    /// </summary>
    private void RefreshStats()
    {
        int currentAttackPower = harvestController.AttackPower;
        int nextAttackPower = currentAttackPower + attackPowerUpgradeAmount;

        float currentAttackSpeed = harvestController.AttacksPerSecond;
        float nextAttackSpeed = currentAttackSpeed + attackSpeedUpgradeAmount;

        attackPowerValueText.text = $"현재 {currentAttackPower} -> {nextAttackPower}";
        attackSpeedValueText.text = $"현재 {currentAttackSpeed:0.00} -> {nextAttackSpeed:0.00}";
    }

    /// <summary>
    /// 현재 강화 비용 표시
    /// 패널 디자인에 맞춰 "100G" 형식으로 출력
    /// </summary>
    private void RefreshCosts()
    {
        attackPowerCostText.text = $"{attackPowerUpgradeCost}G";
        attackSpeedCostText.text = $"{attackSpeedUpgradeCost}G";
    }

    /// <summary>
    /// 현재 보유 골드에 따라 버튼 활성화 여부를 갱신
    /// </summary>
    private void RefreshButtons()
    {
        attackPowerUpgradeButton.interactable = goldWallet.CurrentGold >= attackPowerUpgradeCost;
        attackSpeedUpgradeButton.interactable = goldWallet.CurrentGold >= attackSpeedUpgradeCost;
    }
}
