using UnityEngine;

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

    // Attack 클립의 타격 프레임에 Animation Event로 연결합니다.
    public void OnAttackImpact()
    {
        if (harvestController == null)
        {
            return;
        }

        harvestController.OnAttackImpactAnimationEvent();
    }

    // Attack 클립의 끝부분에 Animation Event로 연결합니다.
    public void OnAttackAnimationFinished()
    {
        if (harvestController == null)
        {
            return;
        }

        harvestController.OnAttackAnimationFinishedEvent();
    }
}
