using System.Collections;
using UnityEngine;

public class Mushroom : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRadius = 1.0f;

    [Header("Stats")]
    [SerializeField] private int maxHp = 50;

    [Header("Hit Feedback")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform hitEffectSpawnPoint;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifetime = 1.0f;
    [SerializeField] private float hitReactionDuration = 0.12f;
    [SerializeField] private float hitScaleMultiplier = 1.12f;
    [SerializeField] private float hitTiltAngle = 6f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 8.0f;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Coroutine _respawnCoroutine;
    private Coroutine _hitReactionCoroutine;

    private Vector3 _defaultVisualLocalScale;
    private Quaternion _defaultVisualLocalRotation;

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
        // 버섯 프리팹마다 자식 구조가 다를 수 있으므로
        // Renderer / Collider는 자식 포함으로 한 번만 캐싱해 재사용합니다.
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);

        // visualRoot를 따로 지정하지 않으면 루트를 그대로 반응 대상으로 사용합니다.
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        // 타격 이펙트 위치를 지정하지 않았으면 상호작용 지점 또는 루트를 사용합니다.
        if (hitEffectSpawnPoint == null)
        {
            hitEffectSpawnPoint = interactionPoint != null ? interactionPoint : transform;
        }

        _defaultVisualLocalScale = visualRoot.localScale;
        _defaultVisualLocalRotation = visualRoot.localRotation;

        ResetState();
    }

    /// <summary>
    /// 플레이어의 1회 공격이 들어왔을 때 호출. 
    /// 이번 공격으로 채집 완료되었는지 여부를 반환
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

        // 타격 자체는 성공했으므로 이펙트 발생
        SpawnHitEffect();

        // 아직 체력이 남아 있다면 피격 반응만 재생
        if (_currentHp > 0)
        {
            PlayHitReaction();
            return false;
        }

        // 체력이 0이 되면 채집 완료 처리
        Harvest();
        return true;
    }

    /// <summary>
    /// 버섯이 완전히 채집되었을 때, 모습을 감추고 리스폰 타이머 작동
    /// </summary>
    private void Harvest()
    {
        _isHarvestable = false;

        // 수확 시점에는 시각 상태를 기본값으로 되돌린 뒤 숨겨야
        // 다음 리스폰 때 스케일/회전이 꼬이지 않음
        StopHitReaction();
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
        // 리스폰 시에는 체력과 시각 상태를 함께 초기화
        _currentHp = maxHp;
        _isHarvestable = true;

        StopHitReaction();
        SetVisualState(true);
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = hitEffectSpawnPoint != null
            ? hitEffectSpawnPoint.position
            : InteractionPosition;

        GameObject effectInstance = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);
        Destroy(effectInstance, hitEffectLifetime);
    }

    private void PlayHitReaction()
    {
        if (visualRoot == null)
        {
            return;
        }

        // 연속 타격 시 반응이 겹쳐 꼬이지 않도록 이전 코루틴을 정리하고 다시 시작
        StopHitReaction();
        _hitReactionCoroutine = StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator HitReactionRoutine()
    {
        Vector3 targetScale = _defaultVisualLocalScale * hitScaleMultiplier;
        Quaternion targetRotation = _defaultVisualLocalRotation * Quaternion.Euler(
            Random.Range(-hitTiltAngle, hitTiltAngle),
            0f,
            Random.Range(-hitTiltAngle, hitTiltAngle));

        float halfDuration = hitReactionDuration * 0.5f;
        float elapsed = 0f;

        // 1단계: 순간적으로 커지고 기울어지며 "맞은 느낌"을 만듦
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            visualRoot.localScale = Vector3.Lerp(_defaultVisualLocalScale, targetScale, t);
            visualRoot.localRotation = Quaternion.Slerp(_defaultVisualLocalRotation, targetRotation, t);

            yield return null;
        }

        elapsed = 0f;

        // 2단계: 원래 크기와 각도로 빠르게 복귀시킴
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            visualRoot.localScale = Vector3.Lerp(targetScale, _defaultVisualLocalScale, t);
            visualRoot.localRotation = Quaternion.Slerp(targetRotation, _defaultVisualLocalRotation, t);

            yield return null;
        }

        ResetVisualTransform();
        _hitReactionCoroutine = null;
    }

    private void StopHitReaction()
    {
        if (_hitReactionCoroutine != null)
        {
            StopCoroutine(_hitReactionCoroutine);
            _hitReactionCoroutine = null;
        }

        ResetVisualTransform();
    }

    private void ResetVisualTransform()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localScale = _defaultVisualLocalScale;
        visualRoot.localRotation = _defaultVisualLocalRotation;
    }

    private void SetVisualState(bool isVisible)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].enabled = isVisible;
        }

        // 채집 중에는 다시 클릭되지 않도록 Collider도 함께 끔
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].enabled = isVisible;
        }
    }
}
