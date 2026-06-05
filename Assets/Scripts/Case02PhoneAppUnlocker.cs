using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

public class Case02PhoneAppUnlocker : MonoBehaviour
{
    [Header("Case02 电话App，也就是 App4")]
    public Button app4Button;

    [Header("Dialogue变量名")]
    public string unlockVariableName = "UnlockPhoneApp";

    private bool unlocked = false;

    void Start()
    {
        LockApp4();

        DialogueLua.SetVariable(unlockVariableName, false);
    }

    void Update()
    {
        if (unlocked) return;

        if (DialogueLua.GetVariable(unlockVariableName).asBool)
        {
            UnlockApp4();
            DialogueLua.SetVariable(unlockVariableName, false);
        }
    }

    public void LockApp4()
    {
        unlocked = false;

        if (app4Button != null)
            app4Button.interactable = false;
    }

    public void UnlockApp4()
    {
        unlocked = true;

        if (app4Button != null)
            app4Button.interactable = true;

        Debug.Log("Case02 Phone App 已解锁");
    }
}