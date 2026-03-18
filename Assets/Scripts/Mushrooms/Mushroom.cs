using UnityEngine;

public class Mushroom : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRadius = 1.0f;

    // 버섯 모델의 pivot이 애매할 수 있어서,
    // 필요하면 별도 interactionPoint를 두고 그 위치를 목적지 기준으로 씁니다.
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

    // 다음 단계의 자동 채집에서 사용할 반경입니다.
    public float InteractionRadius => interactionRadius;
}