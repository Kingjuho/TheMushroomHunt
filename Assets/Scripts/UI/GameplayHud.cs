using TMPro;
using UnityEngine;

public class GameplayHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGoldWallet goldWallet;                  // 플레이어 골드 데이터
    [SerializeField] private PlayerHarvestController harvestController;    // 현재 타겟 정보
    [SerializeField] private InformationPanelPresenter informationPanelPresenter;

    [Header("UI")]
    [SerializeField] private TMP_Text goldText;                            // 현재 골드 텍스트
    [SerializeField] private TMP_Text targetNameText;                      // 현재 타겟 이름 텍스트
    [SerializeField] private TMP_Text targetHpText;                        // 현재 타겟 HP 텍스트

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        goldWallet.GoldChanged += HandleGoldChanged;

        if (informationPanelPresenter != null)
        {
            informationPanelPresenter.SetSelectionPresentationEnabled(true);
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (goldWallet != null)
            goldWallet.GoldChanged -= HandleGoldChanged;

        if (informationPanelPresenter != null)
            informationPanelPresenter.SetSelectionPresentationEnabled(false);
    }

    private void Update()
    {
        // 공용 정보 패널이 연결된 MainScene에서는
        // Text_TargetName / Text_TargetHP의 소유권을 Presenter에 넘긴다.
        if (ShouldDeferTargetTextToInformationPanel())
            return;

        RefreshTargetText();
    }

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

    private void HandleGoldChanged(int currentGold, int delta)
    {
        RefreshGoldText();
    }

    private void RefreshAll()
    {
        RefreshGoldText();

        if (ShouldDeferTargetTextToInformationPanel())
        {
            return;
        }

        RefreshTargetText();
    }

    private void RefreshGoldText()
    {
        goldText.text = $"Gold: {goldWallet.CurrentGold}";
    }

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

    private bool ShouldDeferTargetTextToInformationPanel()
    {
        return informationPanelPresenter != null && informationPanelPresenter.IsSelectionPresentationActive;
    }
}
