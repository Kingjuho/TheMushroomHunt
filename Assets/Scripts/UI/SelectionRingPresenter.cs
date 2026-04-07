using UnityEngine;

/// <summary>
/// 월드 공간의 단일 선택 고리를 재사용해 현재 선택 대상을 시각적으로 강조
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SelectionRingPresenter : MonoBehaviour
{
    private enum RingTargetType
    {
        None,
        Player,
        NonPlayer
    }

    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Style")]
    [SerializeField] private Color playerColor = new Color(0.2f, 1.0f, 0.2f, 0.95f);
    [SerializeField] private Color nonPlayerColor = new Color(1.0f, 0.9f, 0.2f, 0.95f);
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float yOffset = 0.25f;
    [SerializeField] private float radiusPadding = 0.2f;
    [SerializeField] private float minRadius = 0.3f;
    [SerializeField] private float maxRadius = 1.8f;
    [SerializeField] private int segmentCount = 40;

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _propertyBlock;
    private Transform _target;
    private RingTargetType _targetType = RingTargetType.None;
    private Mushroom _targetMushroom;
    private Vector3 _cachedCenter;
    private float _cachedRadius;
    private bool _followTargetPosition;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            Debug.LogWarning($"{nameof(SelectionRingPresenter)}: lineRenderer reference is missing.", this);
            enabled = false;
            return;
        }

        segmentCount = Mathf.Max(12, segmentCount);
        lineWidth = Mathf.Max(0.001f, lineWidth);
        minRadius = Mathf.Max(0.05f, minRadius);
        maxRadius = Mathf.Max(minRadius, maxRadius);
        _propertyBlock = new MaterialPropertyBlock();

        ConfigureRenderer();
        Clear();
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        if (!IsCurrentTargetValid())
        {
            Clear();
            return;
        }

        Vector3 center = _cachedCenter;

        // 플레이어만 위치를 따라가고, 반지름은 최초 선택 시 계산값을 유지
        if (_followTargetPosition)
        {
            center = new Vector3(_target.position.x, _target.position.y + yOffset, _target.position.z);
        }

        ApplyColor(_targetType == RingTargetType.Player ? playerColor : nonPlayerColor);
        DrawCircle(center, _cachedRadius);

        if (!lineRenderer.enabled)
        {
            lineRenderer.enabled = true;
        }
    }

    public void ShowForPlayer(Transform target)
    {
        Show(target, RingTargetType.Player);
    }

    public void ShowForNonPlayer(Transform target)
    {
        Show(target, RingTargetType.NonPlayer);
    }

    public void Clear()
    {
        _target = null;
        _targetMushroom = null;
        _targetType = RingTargetType.None;
        _cachedCenter = default;
        _cachedRadius = minRadius;
        _followTargetPosition = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.SetPropertyBlock(null);
        }
    }

    private void Show(Transform target, RingTargetType targetType)
    {
        if (target == null)
        {
            Clear();
            return;
        }

        if (!TryCachePlacement(target, targetType, out Vector3 center, out float radius, out Mushroom mushroom, out bool followTargetPosition))
        {
            Clear();
            return;
        }

        _target = target;
        _targetType = targetType;
        _targetMushroom = mushroom;
        _cachedCenter = center;
        _cachedRadius = radius;
        _followTargetPosition = followTargetPosition;
    }

    private void ConfigureRenderer()
    {
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = segmentCount + 1;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // 라인 자체는 단순 표시용이라 lighting 영향 최소화
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;

        if (lineRenderer.sharedMaterial == null)
        {
            Debug.LogWarning(
                $"{nameof(SelectionRingPresenter)}: LineRenderer material is missing. Assign a URP-compatible unlit material in the Inspector.",
                this);
        }
    }

    private bool TryCachePlacement(
        Transform target,
        RingTargetType targetType,
        out Vector3 center,
        out float radius,
        out Mushroom mushroom,
        out bool followTargetPosition)
    {
        center = default;
        radius = minRadius;
        mushroom = null;
        followTargetPosition = false;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        Bounds bounds;

        if (targetType == RingTargetType.Player)
        {
            if (!TryGetStablePlayerBounds(target, out bounds))
            {
                return false;
            }

            center = new Vector3(target.position.x, target.position.y + yOffset, target.position.z);
            radius = Mathf.Clamp(
                Mathf.Max(bounds.extents.x, bounds.extents.z) + radiusPadding,
                minRadius,
                maxRadius);

            followTargetPosition = true;
            return true;
        }

        mushroom = target.GetComponent<Mushroom>();

        // 버섯은 피격 반응으로 visualRoot가 흔들리므로
        // Renderer bounds 대신 InteractionPosition / InteractionRadius를 캐싱
        if (mushroom != null)
        {
            if (!mushroom.IsHarvestable)
            {
                return false;
            }

            Vector3 interactionPosition = mushroom.InteractionPosition;
            center = new Vector3(
                interactionPosition.x,
                interactionPosition.y + yOffset,
                interactionPosition.z);

            radius = Mathf.Clamp(
                mushroom.InteractionRadius + radiusPadding,
                minRadius,
                maxRadius);

            return true;
        }

        if (!TryGetTargetBounds(target, out bounds))
        {
            return false;
        }

        center = new Vector3(bounds.center.x, bounds.min.y + yOffset, bounds.center.z);
        radius = Mathf.Clamp(
            Mathf.Max(bounds.extents.x, bounds.extents.z) + radiusPadding,
            minRadius,
            maxRadius);

        return true;
    }

    private bool IsCurrentTargetValid()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (_targetMushroom != null && !_targetMushroom.IsHarvestable)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 플레이어는 루트 CapsuleCollider가 이미 안정적인 바닥 기준이므로
    /// 애니메이션으로 흔들리는 자식 renderer bounds 대신 이쪽을 우선 사용
    /// </summary>
    private bool TryGetStablePlayerBounds(Transform target, out Bounds bounds)
    {
        bounds = default;

        Collider rootCollider = target.GetComponent<Collider>();

        if (rootCollider != null && rootCollider.enabled)
        {
            bounds = rootCollider.bounds;
            return true;
        }

        CharacterController characterController = target.GetComponent<CharacterController>();

        if (characterController != null && characterController.enabled)
        {
            bounds = characterController.bounds;
            return true;
        }

        return false;
    }

    private bool TryGetTargetBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        Collider[] colliders = target.GetComponentsInChildren<Collider>(false);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null || !collider.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(false);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void ApplyColor(Color color)
    {
        // LineRenderer vertex color를 사용하는 셰이더 대응
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        if (lineRenderer.sharedMaterial == null)
        {
            return;
        }

        // vertex color를 무시하고 material tint만 보는 셰이더 대응
        _propertyBlock.Clear();

        if (lineRenderer.sharedMaterial.HasProperty(BaseColorPropertyId))
        {
            _propertyBlock.SetColor(BaseColorPropertyId, color);
        }

        if (lineRenderer.sharedMaterial.HasProperty(ColorPropertyId))
        {
            _propertyBlock.SetColor(ColorPropertyId, color);
        }

        lineRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        if (lineRenderer.positionCount != segmentCount + 1)
        {
            lineRenderer.positionCount = segmentCount + 1;
        }

        float step = Mathf.PI * 2f / segmentCount;

        for (int i = 0; i <= segmentCount; i++)
        {
            float angle = step * i;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            lineRenderer.SetPosition(i, center + offset);
        }
    }
}
