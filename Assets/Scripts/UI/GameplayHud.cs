using TMPro;
using UnityEngine;

public class GameplayHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGoldWallet goldWallet;                  // 플레이어 골드 데이터
    [SerializeField] private PlayerHarvestController harvestController;    // 현재 타겟 정보

    [Header("UI")]
    [SerializeField] private TMP_Text goldText;                            // 현재 골드 텍스트
    [SerializeField] private TMP_Text targetNameText;                      // 현재 타겟 이름 텍스트
    [SerializeField] private TMP_Text targetHpText;                        // 현재 타겟 HP 텍스트

    private void Awake()
    {
        // HUD는 게임플레이 데이터를 읽기만 하는 역할이므로
        // 자동 탐색보다 Inspector 수동 연결을 강제하는 편이 안전
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // 골드는 값이 바뀌는 순간만 갱신하면 되므로
        // 지갑의 이벤트에 구독해 불필요한 갱신을 줄임
        goldWallet.GoldChanged += HandleGoldChanged;

        RefreshAll();
    }

    private void OnDisable()
    {
        // OnEnable에서 등록한 이벤트는 반드시 해제합니다.
        // 씬 전환이나 오브젝트 비활성화 시 중복 구독을 막기 위함임
        if (goldWallet != null)
        {
            goldWallet.GoldChanged -= HandleGoldChanged;
        }
    }

    private void Update()
    {
        // 현재 타겟 이름 / HP는 공격과 타겟 전환에 따라 계속 바뀌므로
        // 매 프레임 동기화하는 편이 가장 단순하고 안전함
        RefreshTargetText();
    }

    /// <summary>
    /// HUD가 정상 동작하기 위한 참조가 모두 연결되어 있는지 검사
    /// 누락 시 경고를 출력하고 스크립트를 비활성화
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (goldWallet == null)
        {
            Debug.LogWarning($"{nameof(GameplayHud)}: goldWallet reference is missing.", this);
            isValid = false;
        }

        if (harvestController == null)
        {
            Debug.LogWarning($"{nameof(GameplayHud)}: harvestController reference is missing.", this);
            isValid = false;
        }

        if (goldText == null)
        {
            Debug.LogWarning($"{nameof(GameplayHud)}: goldText reference is missing.", this);
            isValid = false;
        }

        if (targetNameText == null)
        {
            Debug.LogWarning($"{nameof(GameplayHud)}: targetNameText reference is missing.", this);
            isValid = false;
        }

        if (targetHpText == null)
        {
            Debug.LogWarning($"{nameof(GameplayHud)}: targetHpText reference is missing.", this);
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// 골드가 바뀌었을 때 호출되는 이벤트 콜백
    /// 현재 단계에서는 텍스트만 다시 그리면 충분함
    /// </summary>
    private void HandleGoldChanged(int currentGold, int delta)
    {
        RefreshGoldText();
    }

    /// <summary>
    /// HUD 전체를 한 번에 초기화
    /// OnEnable 직후 현재 상태를 즉시 반영할 때 사용
    /// </summary>
    private void RefreshAll()
    {
        RefreshGoldText();
        RefreshTargetText();
    }

    /// <summary>
    /// 현재 골드 텍스트 갱신
    /// </summary>
    private void RefreshGoldText()
    {
        goldText.text = $"Gold: {goldWallet.CurrentGold}";
    }

    /// <summary>
    /// 현재 타겟 이름 / HP 표시를 갱신
    /// 타겟이 없거나 이미 채집된 상태라면 기본 문구로 되돌림
    /// </summary>
    private void RefreshTargetText()
    {
        Mushroom target = harvestController.DisplayTarget;

        if (target == null || !target.IsHarvestable)
        {
            targetNameText.text = string.Empty;
            targetHpText.text = string.Empty;
            return;
        }

        targetNameText.text = $"{target.DisplayName}";
        targetHpText.text = $"HP: {target.CurrentHp} / {target.MaxHp}";
    }
}
