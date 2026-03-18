using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerClickMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Raycast")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask mushroomMask;
    [SerializeField] private float maxRayDistance = 500f;

    [Header("NavMesh")]
    [SerializeField] private float navMeshSampleDistance = 1.5f;
    [SerializeField] private float extraMushroomStopDistance = 0.15f;

    private NavMeshAgent _agent;
    private float _defaultStoppingDistance;
    private Mushroom _targetMushroom;

    // 채집 중 입력을 잠그기 위해 외부에서 제어할 수 있게 둡니다.
    public bool InputLocked { get; set; }

    // 현재 이동 대상으로 지정된 버섯을 외부에서 읽을 수 있게 둡니다.
    public Mushroom TargetMushroom => _targetMushroom;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _defaultStoppingDistance = _agent.stoppingDistance;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (InputLocked)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(1))
        {
            return;
        }

        if (mainCamera == null)
        {
            return;
        }

        if (!_agent.enabled || !_agent.isOnNavMesh)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int clickMask = groundMask | mushroomMask;

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, clickMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // 버섯을 먼저 판정합니다.
        Mushroom mushroom = hit.collider.GetComponentInParent<Mushroom>();

        if (mushroom != null)
        {
            if (!mushroom.IsHarvestable)
            {
                return;
            }

            MoveToMushroom(mushroom);
            return;
        }

        // 버섯이 아니면 기본 바닥 이동으로 처리합니다.
        ClearTargetMushroom();
        TryMoveToPosition(hit.point);
    }

    private void MoveToMushroom(Mushroom mushroom)
    {
        _targetMushroom = mushroom;

        // 버섯 중심까지 파고들지 않도록,
        // 버섯의 상호작용 반경만큼 stoppingDistance를 늘립니다.
        _agent.stoppingDistance = Mathf.Max(
            _defaultStoppingDistance,
            mushroom.InteractionRadius + extraMushroomStopDistance);

        TryMoveToPosition(mushroom.InteractionPosition);
    }

    private void TryMoveToPosition(Vector3 worldPosition)
    {
        if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navMeshHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return;
        }

        _agent.SetDestination(navMeshHit.position);
    }

    // 채집 컨트롤러가 "도착했는지" 확인할 때 쓰는 판정입니다.
    public bool HasReachedMushroom(Mushroom mushroom)
    {
        if (mushroom == null || mushroom != _targetMushroom)
        {
            return false;
        }

        if (_agent.pathPending)
        {
            return false;
        }

        if (_agent.remainingDistance > _agent.stoppingDistance + 0.05f)
        {
            return false;
        }

        // NavMeshAgent는 도착 직후에도 path/velocity 값이 잠깐 남을 수 있어
        // desiredVelocity까지 같이 확인해 안정적으로 도착 판정을 냅니다.
        return !_agent.hasPath || _agent.desiredVelocity.sqrMagnitude < 0.0001f;
    }

    // 채집 시작 시 경로를 끊어 즉시 멈추게 합니다.
    public void StopImmediately()
    {
        if (_agent.hasPath)
        {
            _agent.ResetPath();
        }
    }

    // 바닥 이동으로 복귀할 때 기본 stoppingDistance를 되돌립니다.
    public void ClearTargetMushroom()
    {
        _targetMushroom = null;
        _agent.stoppingDistance = _defaultStoppingDistance;
    }
}
