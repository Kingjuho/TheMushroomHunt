using UnityEngine;

/// <summary>
/// 공용 정보 패널에 표시할 표지판 문구 데이터.
/// 실제 표시 로직은 WorldSelectionController와 InformationPanelPresenter가 담당
/// </summary>
public class InformationSign : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private string displayTitle = "안내";
    [TextArea(3, 8)]
    [SerializeField] private string displayBody = string.Empty;

    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayTitle))
            {
                return displayTitle;
            }

            return gameObject.name;
        }
    }

    public string DisplayBody => displayBody;
}
