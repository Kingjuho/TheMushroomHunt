using System.Collections;
using UnityEngine;

public class Mushroom : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;        // 상호작용 기준점
    [SerializeField] private float interactionRadius = 1.0f;    // 상호작용 반경

    [Header("Stats")]
    [SerializeField] private int maxHp = 50;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.1f;

    private Renderer[] _renderers;          // 렌더러 컴포넌트
    private Collider[] _colliders;          // 콜라이더 컴포넌트
    private Coroutine _respawnCoroutine;    // 리스폰 코루틴

    private int _currentHp;
    private bool _isHarvestable = true;     // 현재 채집 가능 여부

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

    // 외부 읽기 전용 프로퍼티
    public float InteractionRadius => interactionRadius;
    public int MaxHp => maxHp;
    public int CurrentHp => _currentHp;
    public bool IsHarvestable => _isHarvestable;

    private void Awake()
    {
        // 렌더러와 충돌체를 모두 가지고 옴
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);

        ResetState();
    }

    /// <summary>
    /// 공격을 받았을 때 데미지 계산 및 채집 여부 계산
    /// </summary>
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

    /// <summary>
    /// 버섯이 완전히 채집되었을 때, 모습을 감추고 리스폰 타이머 작동
    /// </summary>
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

    /// <summary>
    /// respawnDelay만큼 기다렸다가, 다시 원래의 모습으로 리스폰
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        ResetState();
        _respawnCoroutine = null;
    }

    /// <summary>
    /// 버섯 초기화
    /// </summary>
    private void ResetState()
    {
        _currentHp = maxHp;
        _isHarvestable = true;
        SetVisualState(true);
    }

    /// <summary>
    /// 렌더러, 콜라이더 온오프 함수
    /// </summary>
    private void SetVisualState(bool isVisible)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].enabled = isVisible;
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].enabled = isVisible;
        }
    }
}
