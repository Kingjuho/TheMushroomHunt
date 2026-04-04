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

    // 외부 읽기 전용 프로퍼티
    public Mushroom TargetMushroom => _targetMushroom;                      // 타겟 버섯
    public float CurrentMoveSpeed => _agent != null ? _agent.speed : 0f;    // 현재 이동 속도

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
    /// 땅을 클릭했을 때 호출되며, 기존의 타겟 버섯을 해제한 후 해당 좌표로 이동
    /// </summary>
    private void HandleGroundClick(Vector3 hitPoint)
    {
        ClearTargetMushroom();
        TryMoveToPosition(hitPoint);
    }

    /// <summary>
    /// 버섯을 클릭했을 때 호출되며, 채집 가능하면 목표로 삼아 이동 시작
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
        if (mushroom == null) return;

        // 타겟과 정지 거리를 바꾸기 전에 현재 상태 저장 (실패 시 복구용)
        Mushroom previousTarget = _targetMushroom;
        float previousStoppingDistance = _agent.stoppingDistance;

        _targetMushroom = mushroom;

        // 기본 정지 거리와 (버섯의 크기 + 여유 거리) 중 더 큰 값을 선택해 안전하게 정지
        _agent.stoppingDistance = Mathf.Max(
            _defaultStoppingDistance,
            mushroom.InteractionRadius + extraMushroomStopDistance);

        // 실제 목적지 설정에 성공한 경우에만 새 버섯 타겟을 유지
        // 실제로 끝까지 도달했는 지 검증은 HasReachedMushroom()에서 별도로 수행
        if (TryMoveToPosition(mushroom.InteractionPosition)) return;

        // 이동 실패 시, 이전 타겟과 정지 거리로 복구
        RestoreMoveContext(previousTarget, previousStoppingDistance);
    }

    /// <summary>
    /// 지정된 월드 좌표 근처의 경로(NavMesh)을 찾아 이동 명령 시도
    /// 성공 여부를 반환하여 호출 측이 타겟 유지/복구를 결정할 수 있도록 함
    /// </summary>
    private bool TryMoveToPosition(Vector3 worldPosition)
    {
        // NavMeshAgent 유효성 체크
        if (!_agent.enabled || !_agent.isOnNavMesh) 
            return false;

        // NavMesh 상에서 유효한 위치 탐색 (갈 수 없는 위치 클릭 시 보정)
        if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navMeshHit, navMeshSampleDistance, NavMesh.AllAreas))
            return false;

        return _agent.SetDestination(navMeshHit.position);
    }

    /// <summary>
    /// 타겟 버섯에 완벽하게 도달했는지 검증
    /// </summary>
    public bool HasReachedMushroom(Mushroom mushroom)
    {
        if (mushroom == null || mushroom != _targetMushroom)
            return false;

        if (!_agent.enabled || !_agent.isOnNavMesh)
            return false;

        if (_agent.pathPending) 
            return false;

        // 너무 빡빡한 허용 오차를 쓰면 "특정 위치에서만 공격 가능"한 회귀 발생
        if (_agent.remainingDistance > _agent.stoppingDistance + 0.1f)
            return false;

        // interactionPoint가 실제로 지정된 버섯에만 strict 실제 거리 검증 적용
        if (ShouldValidateActualInteractionDistance(mushroom) && !IsWithinMushroomInteractionRange(mushroom))
            return false;

        // 완벽히 정지했거나, 남은 속도가 거의 0에 수렴할 때 도착으로 판정
        // 0.01: NavMeshAgent의 미세한 떨림 때문에 도착 판정이 지연되는 문제를 줄이기 위한 값
        return !_agent.hasPath || _agent.desiredVelocity.sqrMagnitude < 0.01f;
    }

    /// <summary>
    /// strict 실제 거리 검증을 적용할지 결정
    /// 주의: interactionPoint가 루트와 완전히 같은 위치라면 미지정과 동일하게 취급됨
    /// </summary>
    private bool ShouldValidateActualInteractionDistance(Mushroom mushroom)
    {
        Vector3 interactionOffset = mushroom.InteractionPosition - mushroom.transform.position;
        return interactionOffset.sqrMagnitude > 0.0001f;
    }

    /// <summary>
    /// 이동을 즉시 정지시킴 (버섯 앞 도착 시, 혹은 강제 정지 시 사용)
    /// </summary>
    public void StopImmediately()
    {
        if (_agent.hasPath) _agent.ResetPath();
    }

    /// <summary>
    /// 타겟 해제 및 정지 거리 초기화
    /// </summary>
    public void ClearTargetMushroom()
    {
        _targetMushroom = null;
        _agent.stoppingDistance = _defaultStoppingDistance;
    }

    /// <summary>
    /// 버섯 재지정 이동이 실패했을 때 직전 이동 문맥을 복구
    /// 이 함수는 "실패한 클릭 때문에 타겟 정보가 바뀌지 않도록 한다"는 책임만 담당
    /// </summary>
    private void RestoreMoveContext(Mushroom previousTarget, float previousStoppingDistance)
    {
        _targetMushroom = previousTarget;
        _agent.stoppingDistance = previousStoppingDistance;
    }

    /// <summary>
    /// 실제 버섯 InteractionPosition까지의 평면 거리가 허용 범위 안인지 별도로 확인
    /// explicit interactionPoint가 있다고 판단된 경우에만 호출
    /// </summary>
    private bool IsWithinMushroomInteractionRange(Mushroom mushroom)
    {
        // 이동과 채집 진입은 수평 거리 기준이므로 높이 차이는 제외
        Vector3 toInteraction = mushroom.InteractionPosition - transform.position;
        toInteraction.y = 0f;

        // 실제 상호작용 지점 검증에도 오차를 약간 허용 (버섯 크기 + 여유 거리)
        float allowedDistance = _agent.stoppingDistance + 0.2f;
        return toInteraction.sqrMagnitude <= allowedDistance * allowedDistance;
    }

    /// <summary>
    /// 세이브파일의 플레이어 위치를 복원할 때 호출
    /// NavMesh 위에서만 위치를 적용하고, 실패 시 호출 측이 기본 시작 위치를 유지할 수 있도록 false를 반환
    /// </summary>
    public bool TryWarpToPosition(Vector3 worldPosition, float sampleDistance)
    {
        float effectiveSampleDistance = Mathf.Max(0.01f, sampleDistance);

        if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navMeshHit, effectiveSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }

        ClearTargetMushroom();
        InputLocked = false;
        StopImmediately();

        if (_agent.enabled && _agent.isOnNavMesh)
        {
            return _agent.Warp(navMeshHit.position);
        }

        bool wasEnabled = _agent.enabled;

        if (wasEnabled)
        {
            _agent.enabled = false;
        }

        transform.position = navMeshHit.position;

        if (wasEnabled)
        {
            _agent.enabled = true;
        }

        return true;
    }

    /// <summary>
    /// 세이브파일의 최종 이동속도를 NavMeshAgent에 복원
    /// 잘못된 값이면 기존 Inspector 값을 유지하도록 false 반환
    /// </summary>
    public bool TrySetMoveSpeed(float moveSpeed)
    {
        if (_agent == null)
        {
            return false;
        }

        if (moveSpeed <= 0f)
        {
            Debug.LogWarning($"{nameof(PlayerClickMove)}: moveSpeed must be greater than 0.", this);
            return false;
        }

        _agent.speed = moveSpeed;
        return true;
    }
}
