using UnityEngine;

public class UpgradeZoneTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerUpgradePanel upgradePanel;   // 표시 / 숨김 대상 패널

    private PlayerClickMove _currentPlayer;                     // 현재 영역 안에 들어온 플레이어

    private void Awake()
    {
        // 패널 참조가 비어 있으면 조용히 실패하지 않도록 경고 후 비활성화
        if (upgradePanel == null)
        {
            Debug.LogWarning($"{nameof(UpgradeZoneTrigger)}: upgradePanel reference is missing.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        // 씬 시작 시 패널이 미리 열려 있지 않도록 한 번 닫음
        if (upgradePanel != null)
        {
            upgradePanel.ClosePanel();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 아닌 오브젝트는 무시
        if (!TryGetPlayer(other, out PlayerClickMove player))
        {
            return;
        }

        _currentPlayer = player;
        upgradePanel.OpenPanel();
    }

    private void OnTriggerExit(Collider other)
    {
        // 현재 들어와 있는 플레이어가 없으면 무시
        if (_currentPlayer == null)
        {
            return;
        }

        // 나간 대상이 현재 플레이어가 아니면 무시
        PlayerClickMove player = other.GetComponentInParent<PlayerClickMove>();

        if (player != _currentPlayer)
        {
            return;
        }

        _currentPlayer = null;
        upgradePanel.ClosePanel();
    }

    /// <summary>
    /// Trigger에 들어온 Collider가 실제 플레이어 소속인지 판별
    /// 현재 프로젝트에서는 PlayerClickMove를 기준으로 플레이어를 식별
    /// </summary>
    private bool TryGetPlayer(Collider other, out PlayerClickMove player)
    {
        player = other.GetComponentInParent<PlayerClickMove>();
        return player != null;
    }
}
