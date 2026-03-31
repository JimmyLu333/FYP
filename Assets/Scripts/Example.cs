using UnityEngine;

public class SimpleExample : MonoBehaviour
{
    // 这个方法必须是 public，否则在按钮的 Inspector 面板里找不到它
    public void ButtonClick()
    {
        Debug.Log("按钮被点击了！逻辑运行正常。");
    }
}