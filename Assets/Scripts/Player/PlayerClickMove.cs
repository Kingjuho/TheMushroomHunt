using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerClickMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Raycast")]
    [SerializeField] private LayerMask groundMask = ~0;
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
        // 1차 구현에서는 우클릭 이동만 처리합니다.
        if (!Input.GetMouseButtonDown(1))
        {
            return;
        }

        if (mainCamera == null)
        {
            return;
        }

        // 마우스 위치에서 월드로 레이를 쏴서 클릭 지점을 찾습니다.
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // 클릭한 지점이 NavMesh 가장자리이거나 살짝 벗어나도 가까운 유효 지점을 찾도록 보정합니다.
        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return;
        }

        _agent.SetDestination(navMeshHit.position);
    }
}