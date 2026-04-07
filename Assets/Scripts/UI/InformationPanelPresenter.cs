using TMPro;
using UnityEngine;

/// <summary>
/// Panel_Information의 제목 / 본문 텍스트를 공용 선택 패널로 사용하는 프리젠터.
/// 패널 전체 활성/비활성 대신 제목과 본문만 초기화해
/// SaveStatus 같은 별도 텍스트와 충돌하지 않도록 함
/// </summary>
public class InformationPanelPresenter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("World Selection")]
    [SerializeField] private SelectionRingPresenter selectionRingPresenter;

    private SelectionType _selectionType = SelectionType.None;
    private InformationSign _selectedSign;
    private PlayerHarvestController _selectedPlayerHarvestController;
    private PlayerClickMove _selectedPlayerClickMove;
    private Mushroom _selectedMushroom;
    private bool _selectionPresentationEnabled;

    /// <summary>
    /// GameplayHud가 공용 패널 모드를 실제로 활성화했는지 나타냄
    /// partial fallback 상태에서는 false로 남겨 WorldSelectionController가 선택 표시를 중단하도록 함
    /// </summary>
    public bool IsSelectionPresentationActive => _selectionPresentationEnabled && isActiveAndEnabled;

    /// <summary>
    /// GameplayHud가 공용 정보 패널 텍스트 소유권을 넘겨줄 때만 true로 설정
    /// false로 내려갈 때는 기존 선택 정보를 지워 텍스트 충돌과 stale 표시를 막음
    /// </summary>
    public void SetSelectionPresentationEnabled(bool isEnabled)
    {
        if (_selectionPresentationEnabled == isEnabled)
        {
            return;
        }

        _selectionPresentationEnabled = isEnabled;

        if (!isEnabled)
        {
            ClearSelection();
        }
    }

    private enum SelectionType
    {
        None,
        Sign,
        Player,
        Mushroom
    }

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ClearSelection();
    }

    private void Update()
    {
        switch (_selectionType)
        {
            case SelectionType.Sign:
                RefreshSignSelection();
                break;

            case SelectionType.Player:
                RefreshPlayerSelection();
                break;

            case SelectionType.Mushroom:
                RefreshMushroomSelection();
                break;
        }
    }

    public void ShowSign(InformationSign sign)
    {
        if (sign == null)
        {
            ClearSelection();
            return;
        }

        _selectionType = SelectionType.Sign;
        _selectedSign = sign;
        _selectedPlayerHarvestController = null;
        _selectedPlayerClickMove = null;
        _selectedMushroom = null;

        ApplyText(sign.DisplayTitle, sign.DisplayBody);

        selectionRingPresenter?.ShowForNonPlayer(sign.transform);
    }

    public void ShowPlayer(PlayerHarvestController harvestController, PlayerClickMove clickMove)
    {
        if (harvestController == null || clickMove == null)
        {
            ClearSelection();
            return;
        }

        _selectionType = SelectionType.Player;
        _selectedSign = null;
        _selectedPlayerHarvestController = harvestController;
        _selectedPlayerClickMove = clickMove;
        _selectedMushroom = null;

        RefreshPlayerSelection();

        selectionRingPresenter?.ShowForPlayer(clickMove.transform);
    }

    public void ShowMushroom(Mushroom mushroom)
    {
        if (mushroom == null || !mushroom.IsHarvestable)
        {
            ClearSelection();
            return;
        }

        _selectionType = SelectionType.Mushroom;
        _selectedSign = null;
        _selectedPlayerHarvestController = null;
        _selectedPlayerClickMove = null;
        _selectedMushroom = mushroom;

        RefreshMushroomSelection();

        selectionRingPresenter?.ShowForNonPlayer(mushroom.transform);
    }

    public void ClearSelection()
    {
        _selectionType = SelectionType.None;
        _selectedSign = null;
        _selectedPlayerHarvestController = null;
        _selectedPlayerClickMove = null;
        _selectedMushroom = null;

        ApplyText(string.Empty, string.Empty);

        selectionRingPresenter?.Clear();
    }

    private void RefreshPlayerSelection()
    {
        if (_selectedPlayerHarvestController == null
            || _selectedPlayerClickMove == null
            || !_selectedPlayerClickMove.gameObject.activeInHierarchy)
        {
            ClearSelection();
            return;
        }

        ApplyText(
            "플레이어",
            $"공격력: {_selectedPlayerHarvestController.AttackPower}\n" +
            $"공격 속도: {_selectedPlayerHarvestController.AttacksPerSecond:0.##}\n" +
            $"이동 속도: {_selectedPlayerClickMove.CurrentMoveSpeed:0.##}");
    }

    private void RefreshMushroomSelection()
    {
        if (_selectedMushroom == null
            || !_selectedMushroom.gameObject.activeInHierarchy
            || !_selectedMushroom.IsHarvestable)
        {
            ClearSelection();
            return;
        }

        ApplyText(
            _selectedMushroom.DisplayName,
            $"HP: {_selectedMushroom.CurrentHp} / {_selectedMushroom.MaxHp}\n" +
            $"보상 골드: {_selectedMushroom.RewardGold}");
    }

    private void RefreshSignSelection()
    {
        if (_selectedSign == null || !_selectedSign.gameObject.activeInHierarchy)
        {
            ClearSelection();
            return;
        }

        ApplyText(_selectedSign.DisplayTitle, _selectedSign.DisplayBody);
    }

    private void ApplyText(string title, string body)
    {
        titleText.text = title;
        bodyText.text = body;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (titleText == null)
        {
            Debug.LogWarning($"{nameof(InformationPanelPresenter)}: titleText reference is missing.", this);
            isValid = false;
        }

        if (bodyText == null)
        {
            Debug.LogWarning($"{nameof(InformationPanelPresenter)}: bodyText reference is missing.", this);
            isValid = false;
        }

        return isValid;
    }
}
