using UnityEngine;

public class WindowCloseButton : MonoBehaviour
{
    public GameObject targetWindow;

    public void CloseWindow()
    {
        if (targetWindow != null)
            targetWindow.SetActive(false);
    }
}