using UnityEngine;
using UnityEngine.UI;


public class ScrollViewManager : MonoBehaviour
{

    [Header("¹ö¶¯ÇøÓò")]
    public ScrollRect scrollRect;

    public void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
