using UnityEngine;
using UnityEngine.UI;

public class Case02PhoneAppUnlocker : MonoBehaviour
{
    [Header("Case02 电话App，也就是 App4")]
    public Button app4Button;

    void Start()
    {
        LockApp4();
    }

    public void LockApp4()
    {
        if (app4Button != null)
            app4Button.interactable = false;
    }

    public void UnlockApp4()
    {
        if (app4Button != null)
            app4Button.interactable = true;
    }
}