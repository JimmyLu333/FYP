using UnityEngine;

public class ProfilePanelController : MonoBehaviour
{
    public GameObject profilePanel;

    void Start()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
        }
    }

    public void OpenProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
        }
    }

    public void CloseProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
        }
    }
}