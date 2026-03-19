using UnityEngine;

/// <summary>
/// 애니메이션 클립에 삽입할 이벤트 함수
/// </summary>
public class PlayerAnimationEventRelay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHarvestController harvestController;

    private void Awake()
    {
        if (harvestController == null)
        {
            harvestController = GetComponentInParent<PlayerHarvestController>();
        }
    }

    // Bound to the impact frame of the attack clip.
    public void OnAttackImpact()
    {
        if (harvestController == null)
        {
            return;
        }

        harvestController.OnAttackImpactAnimationEvent();
    }

    // Bound to the end of the attack clip.
    public void OnAttackAnimationFinished()
    {
        if (harvestController == null)
        {
            return;
        }

        harvestController.OnAttackAnimationFinishedEvent();
    }
}
