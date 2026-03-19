using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerClickMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;             // 메인 카메라

    [Header("Raycast")]
    [SerializeField] private LayerMask groundMask;          // 땅 레이어
    [SerializeField] private LayerMask mushroomMask;        // 버섯 레이어
    [SerializeField] private float maxRayDistance = 500f;   // 최대 탐색 거리

    [Header("NavMesh")]
    // 클릭한 위치가 갈 수 없는 위치일 때, 유효한 길을 찾기 위한 최대 반경
    [SerializeField] private float navMeshSampleDistance = 1.5f;
    // 버섯과 겹침 방지를 위한 거리
    [SerializeField] private float extraMushroomStopDistance = 0.15f;

    private NavMeshAgent _agent;                            // NavMesh 컴포넌트
    private float _defaultStoppingDistance;                 // 기본 정지 거리
    private Mushroom _targetMushroom;                       // 타겟 버섯

    public bool InputLocked { get; set; }                   // 인풋 잠금

    public Mushroom TargetMushroom => _targetMushroom;      // 외부 읽기 전용 프로퍼티

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _defaultStoppingDistance = _agent.stoppingDistance;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        // 인풋 가능 상태인지 확인
        if (!CanHandleClickInput()) return;

        // 마우스 클릭 지점 탐색
        if (!TryGetClickHit(out RaycastHit hit)) return;

        // 클릭한 오브젝트가 버섯일 경우
        if (TryHandleMushroomClick(hit)) return;

        // 클릭한 오브젝트가 땅일 경우
        HandleGroundClick(hit.point);
    }

    /// <summary>
    /// 조작 잠금 상태, 우클릭 여부, 에이전트 활성화 여부 등을 종합하여 클릭을 처리할지 결정
    /// </summary>
    private bool CanHandleClickInput()
    {
        if (InputLocked) return false;

        if (!Input.GetMouseButtonDown(1)) return false;

        if (mainCamera == null) return false;

        return _agent.enabled && _agent.isOnNavMesh;
    }

    /// <summary>
    /// 마우스 위치에서 Ray를 쏴서 땅이나 버섯에 맞았는지 확인
    /// </summary>
    private bool TryGetClickHit(out RaycastHit hit)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int clickMask = groundMask | mushroomMask;

        return Physics.Raycast(ray, out hit, maxRayDistance, clickMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// 클릭한 대상이 버섯인지 확인하고, 채집 가능하면 목표로 삼아 이동 시작
    /// </summary>
    private bool TryHandleMushroomClick(RaycastHit hit)
    {
        Mushroom mushroom = hit.collider.GetComponentInParent<Mushroom>();

        // 버섯이 아니면 false
        if (mushroom == null) return false;
        // 이미 채집된 버섯이면 바로 true 반환하여 이동 X
        if (!mushroom.IsHarvestable) return true;

        MoveToMushroom(mushroom);
        return true;
    }

    /// <summary>
    /// 버섯을 향해 이동할 때, 버섯과 겹치지 않도록 정지 거리(stoppingDistance) 조절 후 출발
    /// </summary>
    private void MoveToMushroom(Mushroom mushroom)
    {
        _targetMushroom = mushroom;

        // 기본 정지 거리와 (버섯의 크기 + 여유 거리) 중 더 큰 값을 선택해 안전하게 정지
        _agent.stoppingDistance = Mathf.Max(
            _defaultStoppingDistance,
            mushroom.InteractionRadius + extraMushroomStopDistance);

        TryMoveToPosition(mushroom.InteractionPosition);
    }

    /// <summary>
    /// 땅을 클릭했을 때 호출되며, 기존의 타겟 버섯을 해제한 후 해당 좌표로 이동
    /// </summary>
    private void HandleGroundClick(Vector3 hitPoint)
    {
        ClearTargetMushroom();
        TryMoveToPosition(hitPoint);
    }

    /// <summary>
    /// 지정된 월드 좌표 근처의 경로(NavMesh)을 찾아 이동 명령 부여
    /// </summary>
    private void TryMoveToPosition(Vector3 worldPosition)
    {
        if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navMeshHit, navMeshSampleDistance, NavMesh.AllAreas))
            return;

        _agent.SetDestination(navMeshHit.position);
    }

    /// <summary>
    /// 타겟 버섯에 완벽하게 도달했는지 검증
    /// </summary>
    public bool HasReachedMushroom(Mushroom mushroom)
    {
        if (mushroom == null || mushroom != _targetMushroom)
            return false;

        if (_agent.pathPending) return false;

        if (_agent.remainingDistance > _agent.stoppingDistance + 0.05f)
            return false;

        // 완벽히 정지했거나, 남은 속도가 거의 0에 수렴할 때 도착으로 판정
        return !_agent.hasPath || _agent.desiredVelocity.sqrMagnitude < 0.0001f;
    }

    /// <summary>
    /// 이동을 즉시 정지시킴 (버섯 앞 도착 시, 혹은 강제 정지 시 사용)
    /// </summary>
    public void StopImmediately()
    {
        if (_agent.hasPath)
        {
            _agent.ResetPath();
        }
    }

    /// <summary>
    /// 타겟 해제 및 정지 거리 초기화
    /// </summary>
    public void ClearTargetMushroom()
    {
        _targetMushroom = null;
        _agent.stoppingDistance = _defaultStoppingDistance;
    }
}
