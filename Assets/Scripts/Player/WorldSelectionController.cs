using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 좌클릭 전용 월드 선택 컨트롤러. 정보 패널에 무엇을 보여줄지만 결정
/// </summary>
public class WorldSelectionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private InformationPanelPresenter informationPanelPresenter;
    [SerializeField] private PlayerClickMove playerClickMove;
    [SerializeField] private PlayerHarvestController playerHarvestController;

    [Header("Raycast")]
    [SerializeField] private LayerMask selectionMask = ~0;
    [SerializeField] private float maxRayDistance = 500f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (playerClickMove == null)
        {
            playerClickMove = GetComponent<PlayerClickMove>();
        }

        if (playerHarvestController == null)
        {
            playerHarvestController = GetComponent<PlayerHarvestController>();
        }

        if (!ValidateReferences())
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (mainCamera == null || informationPanelPresenter == null)
        {
            return;
        }

        // GameplayHud가 공용 패널 모드를 실제로 활성화하지 않은 상태라면
        // 선택 패널은 쓰지 않고, 기존 HUD 경로를 그대로 살림
        if (!informationPanelPresenter.IsSelectionPresentationActive)
        {
            return;
        }

        // UI 위 클릭은 월드 선택으로 관통시키지 않음
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (!TryGetSelectionHit(out RaycastHit hit))
        {
            informationPanelPresenter.ClearSelection();
            return;
        }

        if (TrySelectSign(hit))
        {
            return;
        }

        if (TrySelectPlayer(hit))
        {
            return;
        }

        if (TrySelectMushroom(hit))
        {
            return;
        }

        informationPanelPresenter.ClearSelection();
    }

    private void OnDisable()
    {
        if (informationPanelPresenter != null && informationPanelPresenter.IsSelectionPresentationActive)
        {
            informationPanelPresenter.ClearSelection();
        }
    }

    private bool TryGetSelectionHit(out RaycastHit hit)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, maxRayDistance, selectionMask, QueryTriggerInteraction.Ignore);
    }

    private bool TrySelectSign(RaycastHit hit)
    {
        InformationSign sign = hit.collider.GetComponentInParent<InformationSign>();

        if (sign == null)
        {
            return false;
        }

        informationPanelPresenter.ShowSign(sign);
        return true;
    }

    private bool TrySelectPlayer(RaycastHit hit)
    {
        PlayerClickMove clickedPlayerClickMove = hit.collider.GetComponentInParent<PlayerClickMove>();
        PlayerHarvestController clickedPlayerHarvestController = hit.collider.GetComponentInParent<PlayerHarvestController>();

        if (clickedPlayerClickMove == null || clickedPlayerHarvestController == null)
        {
            return false;
        }

        if (playerClickMove != null && clickedPlayerClickMove != playerClickMove)
        {
            return false;
        }

        informationPanelPresenter.ShowPlayer(clickedPlayerHarvestController, clickedPlayerClickMove);
        return true;
    }

    private bool TrySelectMushroom(RaycastHit hit)
    {
        Mushroom mushroom = hit.collider.GetComponentInParent<Mushroom>();

        if (mushroom == null)
        {
            return false;
        }

        informationPanelPresenter.ShowMushroom(mushroom);
        return true;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (mainCamera == null)
        {
            Debug.LogWarning($"{nameof(WorldSelectionController)}: mainCamera reference is missing.", this);
            isValid = false;
        }

        if (informationPanelPresenter == null)
        {
            Debug.LogWarning($"{nameof(WorldSelectionController)}: informationPanelPresenter reference is missing.", this);
            isValid = false;
        }

        if (playerClickMove == null)
        {
            Debug.LogWarning($"{nameof(WorldSelectionController)}: playerClickMove reference is missing.", this);
            isValid = false;
        }

        if (playerHarvestController == null)
        {
            Debug.LogWarning($"{nameof(WorldSelectionController)}: playerHarvestController reference is missing.", this);
            isValid = false;
        }

        return isValid;
    }
}
