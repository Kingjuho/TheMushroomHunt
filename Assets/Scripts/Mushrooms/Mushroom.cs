using System.Collections;
using UnityEngine;

public class Mushroom : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRadius = 1.0f;

    [Header("Stats")]
    [SerializeField] private int maxHp = 50;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 8.0f;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Coroutine _respawnCoroutine;

    private int _currentHp;
    private bool _isHarvestable = true;

    public Vector3 InteractionPosition
    {
        get
        {
            if (interactionPoint != null)
            {
                return interactionPoint.position;
            }

            return transform.position;
        }
    }

    public float InteractionRadius => interactionRadius;
    public int MaxHp => maxHp;
    public int CurrentHp => _currentHp;
    public bool IsHarvestable => _isHarvestable;

    private void Awake()
    {
        // 버섯 프리팹마다 Renderer / Collider 위치가 다를 수 있어
        // 자식 포함으로 한 번만 캐싱해 둡니다.
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);

        ResetState();
    }

    // 플레이어의 1회 공격이 들어올 때 호출됩니다.
    // 반환값은 "이번 공격으로 채집 완료됐는가" 입니다.
    public bool TryTakeDamage(int damage)
    {
        if (!_isHarvestable)
        {
            return false;
        }

        if (damage <= 0)
        {
            return false;
        }

        _currentHp = Mathf.Max(0, _currentHp - damage);

        if (_currentHp > 0)
        {
            return false;
        }

        Harvest();
        return true;
    }

    private void Harvest()
    {
        _isHarvestable = false;
        SetVisualState(false);

        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
        }

        _respawnCoroutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        ResetState();
        _respawnCoroutine = null;
    }

    private void ResetState()
    {
        _currentHp = maxHp;
        _isHarvestable = true;
        SetVisualState(true);
    }

    private void SetVisualState(bool isVisible)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].enabled = isVisible;
        }

        // 채집 중에는 클릭도 막아야 하므로 Collider도 같이 끕니다.
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].enabled = isVisible;
        }
    }
}
