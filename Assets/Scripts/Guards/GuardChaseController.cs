using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 단일 경비원 1개체의 추적/접촉 담당
/// 제재 정책(골드 차감, Warp, 전체 리셋)은 GuardEncounterController가 소유
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GuardChaseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Collider contactTrigger;
    [SerializeField] private Rigidbody physicsBody;

    [Header("Chase")]
    [SerializeField] private float repathInterval = 0.15f;
    [SerializeField] private float spawnNavMeshSampleDistance = 1.5f;

    [Header("Contact")]
    [SerializeField] private float contactDistance = 1.1f;
    [SerializeField] private float contactHeightTolerance = 1.5f;

    private GuardEncounterController _encounterController;
    private PlayerClickMove _trackedPlayer;
    private float _nextRepathTime;
    private bool _isChasing;
    private bool _isInitialized;
    private bool _hasReportedContact;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDisable()
    {
        _isChasing = false;
        _trackedPlayer = null;
        _encounterController = null;
        _hasReportedContact = false;
        StopMovement();
    }

    private void Update()
    {
        if (!_isChasing || _trackedPlayer == null)
        {
            return;
        }

        if (Time.time >= _nextRepathTime)
        {
            _nextRepathTime = Time.time + repathInterval;

            if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(_trackedPlayer.transform.position);
            }
        }

        TryReportContactByDistance();
    }

    /// <summary>
    /// GuardEncounterController가 30초 타이머 완료 후 호출
    /// 스폰 위치로 먼저 보정한 뒤 플레이어 추적을 시작
    /// </summary>
    public bool BeginChase(
        PlayerClickMove player,
        GuardEncounterController encounterController,
        Vector3 spawnPosition,
        Quaternion spawnRotation)
    {
        if (!EnsureInitialized())
        {
            return false;
        }

        if (player == null || encounterController == null)
        {
            return false;
        }

        gameObject.SetActive(true);

        if (!TryPlaceAtSpawn(spawnPosition, spawnRotation))
        {
            gameObject.SetActive(false);
            return false;
        }

        _trackedPlayer = player;
        _encounterController = encounterController;
        _isChasing = true;
        _nextRepathTime = 0f;
        _hasReportedContact = false;
        return true;
    }

    /// <summary>
    /// 접촉 제재 또는 거점 복귀 시 경비원을 스폰 위치로 되돌리고 비활성화
    /// </summary>
    public void ResetToSpawnAndDisable(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (!EnsureInitialized())
        {
            gameObject.SetActive(false);
            return;
        }

        _isChasing = false;
        _trackedPlayer = null;
        _encounterController = null;
        _hasReportedContact = false;

        StopMovement();
        TryPlaceAtSpawn(spawnPosition, spawnRotation);

        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isChasing || _encounterController == null)
        {
            return;
        }

        PlayerClickMove player = other.GetComponentInParent<PlayerClickMove>();

        if (player == null || player != _trackedPlayer)
        {
            return;
        }

        ReportContact();
    }

    private bool EnsureInitialized()
    {
        if (_isInitialized)
        {
            return true;
        }

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (contactTrigger == null)
        {
            contactTrigger = GetComponent<Collider>();
        }

        if (physicsBody == null)
        {
            physicsBody = GetComponent<Rigidbody>();
        }

        repathInterval = Mathf.Max(0.05f, repathInterval);
        spawnNavMeshSampleDistance = Mathf.Max(0.1f, spawnNavMeshSampleDistance);
        
        contactDistance = Mathf.Max(0.1f, contactDistance);
        contactHeightTolerance = Mathf.Max(0.1f, contactHeightTolerance);

        if (navMeshAgent == null || contactTrigger == null || physicsBody == null)
        {
            Debug.LogWarning($"{nameof(GuardChaseController)}: required component is missing.", this);
            return false;
        }

        if (!contactTrigger.isTrigger)
        {
            Debug.LogWarning($"{nameof(GuardChaseController)}: contactTrigger must be set as Trigger.", this);
            return false;
        }

        // 플레이어 쪽에는 Rigidbody가 없으므로, Trigger 판정을 위해 경비원 쪽 Rigidbody를 항상 kinematic으로 설정
        physicsBody.isKinematic = true;
        physicsBody.useGravity = false;

        _isInitialized = true;
        return true;
    }

    private bool TryPlaceAtSpawn(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        transform.rotation = spawnRotation;

        if (!NavMesh.SamplePosition(
            spawnPosition,
            out NavMeshHit navMeshHit,
            spawnNavMeshSampleDistance,
            NavMesh.AllAreas))
        {
            Debug.LogWarning(
                $"{nameof(GuardChaseController)}: guard spawn position is not on NavMesh.",
                this);

            return false;
        }

        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            StopMovement();
            return navMeshAgent.Warp(navMeshHit.position);
        }

        bool wasEnabled = navMeshAgent.enabled;

        if (wasEnabled)
        {
            navMeshAgent.enabled = false;
        }

        transform.position = navMeshHit.position;

        if (wasEnabled)
        {
            navMeshAgent.enabled = true;
        }

        return true;
    }

    private void StopMovement()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        navMeshAgent.ResetPath();
    }

    /// <summary>
    /// Trigger 이벤트가 누락되는 경우를 대비한 거리 기반 접촉 fallback
    /// NavMeshAgent + kinematic Rigidbody 조합에서 OnTriggerEnter가 불안정할 때를 막기 위한 안전장치
    /// </summary>
    private void TryReportContactByDistance()
    {
        if (_hasReportedContact || _trackedPlayer == null || _encounterController == null)
        {
            return;
        }

        Vector3 playerPosition = _trackedPlayer.transform.position;
        Vector3 guardPosition = transform.position;

        if (Mathf.Abs(playerPosition.y - guardPosition.y) > contactHeightTolerance)
        {
            return;
        }

        Vector3 toPlayer = playerPosition - guardPosition;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > contactDistance * contactDistance)
        {
            return;
        }

        ReportContact();
    }

    private void ReportContact()
    {
        if (_hasReportedContact || _trackedPlayer == null || _encounterController == null)
        {
            return;
        }

        _hasReportedContact = true;
        _encounterController.NotifyGuardContact(_trackedPlayer);
    }
}
