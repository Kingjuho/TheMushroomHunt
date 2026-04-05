using UnityEngine;

/// <summary>
/// 플레이어가 광대버섯 섬 진입 존 또는 거점 복귀 존에 들어왔는지 GuardEncounterController에 전달
/// </summary>
[RequireComponent(typeof(Collider))]
public class GuardZoneNotifier : MonoBehaviour
{
    private enum GuardZoneType
    {
        IslandBoundary,
        BaseReturn
    }

    [Header("References")]
    [SerializeField] private GuardEncounterController guardEncounterController;

    [Header("Zone")]
    [SerializeField] private GuardZoneType zoneType;

    private PlayerClickMove _currentPlayer;

    private void Awake()
    {
        if (guardEncounterController == null)
        {
            Debug.LogWarning($"{nameof(GuardZoneNotifier)}: guardEncounterController reference is missing.", this);
            enabled = false;
            return;
        }

        Collider zoneCollider = GetComponent<Collider>();

        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning($"{nameof(GuardZoneNotifier)}: collider must be set as Trigger.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayer(other, out PlayerClickMove player))
        {
            return;
        }

        if (_currentPlayer == player)
        {
            return;
        }

        _currentPlayer = player;

        switch (zoneType)
        {
            case GuardZoneType.IslandBoundary:
                guardEncounterController.NotifyIslandZoneEntered(player);
                break;

            case GuardZoneType.BaseReturn:
                guardEncounterController.NotifyBaseReturnZoneEntered(player);
                break;
        }
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

        if (zoneType == GuardZoneType.IslandBoundary)
        {
            guardEncounterController.NotifyIslandZoneExited(player);
        }

        _currentPlayer = null;
    }

    private bool TryGetPlayer(Collider other, out PlayerClickMove player)
    {
        player = other.GetComponentInParent<PlayerClickMove>();
        return player != null;
    }
}
