using UnityEngine;

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
        {
            return;
        }

        if (_currentPlayer == player)
        {
            return;
        }

        _currentPlayer = player;
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

    private bool TryGetPlayer(Collider other, out PlayerClickMove player)
    {
        player = other.GetComponentInParent<PlayerClickMove>();
        return player != null;
    }
}
