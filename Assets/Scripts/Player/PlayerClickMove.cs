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

    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        // 현재 단계에서는 우클릭 이동만 처리합니다.
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

        // 마우스 위치에서 월드로 레이를 쏴서 클릭 지점을 찾습니다.
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Ground와 Mushroom만 클릭 대상으로 보고,
        // 가장 먼저 맞은 대상을 기준으로 처리합니다.
        int clickMask = groundMask | mushroomMask;

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, clickMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // 버섯을 먼저 판정합니다.
        Mushroom mushroom = hit.collider.GetComponentInParent<Mushroom>();

        if (mushroom != null)
        {
            TryMoveToPosition(mushroom.InteractionPosition);
            return;
        }

        // 버섯이 아니면 Ground 클릭으로 처리합니다.
        TryMoveToPosition(hit.point);
    }

    private void TryMoveToPosition(Vector3 worldPosition)
    {
        // 클릭 지점이 NavMesh 경계에 걸쳐 있어도 가장 가까운 유효 위치를 찾습니다.
        if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navMeshHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return;
        }

        _agent.SetDestination(navMeshHit.position);
    }
}