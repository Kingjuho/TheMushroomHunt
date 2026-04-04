using UnityEngine;

/// <summary>
/// 플레이어가 저장 지점 Trigger에 진입했을 때 단일 슬롯 저장을 요청하는 스크립트
/// 저장 파일 생성/쓰기 자체는 SaveLoadCoordinator가 담당하고,
/// 이 클래스는 "어느 순간 저장을 호출할지"만 결정
/// </summary>
[RequireComponent(typeof(Collider))]
public class SaveTriggerZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SaveLoadCoordinator saveLoadCoordinator;

    private PlayerClickMove _currentPlayer;

    private void Awake()
    {
        if (saveLoadCoordinator == null)
        {
            Debug.LogWarning($"{nameof(SaveTriggerZone)}: saveLoadCoordinator reference is missing.", this);
            enabled = false;
            return;
        }

        Collider triggerCollider = GetComponent<Collider>();

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{nameof(SaveTriggerZone)}: collider must be set as Trigger.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayer(other, out PlayerClickMove player))
            return;

        // 플레이어가 여러 Collider를 가졌더라도
        // 한 번 영역 안에 들어온 동안에는 중복 저장하지 않도록 막는다.
        if (_currentPlayer == player)
            return;

        _currentPlayer = player;

        // 로드 직후 저장 지점 안에서 시작한 경우,
        // 첫 Trigger 진입을 즉시 재저장으로 취급하지 않도록 짧게 차단
        if (saveLoadCoordinator.IsAutoSaveBlockedAfterLoad)
            return;

        saveLoadCoordinator.SaveNow();
    }

    private void OnTriggerExit(Collider other)
    {
        if (_currentPlayer == null)
        {
            return;
        }

        PlayerClickMove player = other.GetComponentInParent<PlayerClickMove>();

        if (player != _currentPlayer)
        {
            return;
        }

        _currentPlayer = null;
    }

    /// <summary>
    /// Trigger에 들어온 Collider가 실제 플레이어 소속인지 판별
    /// 현재 프로젝트에서는 PlayerClickMove가 붙은 루트를 플레이어 식별 기준으로 사용
    /// </summary>
    private bool TryGetPlayer(Collider other, out PlayerClickMove player)
    {
        player = other.GetComponentInParent<PlayerClickMove>();
        return player != null;
    }
}
