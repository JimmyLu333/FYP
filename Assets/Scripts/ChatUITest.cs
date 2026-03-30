using UnityEngine;

public class ChatUITest : MonoBehaviour
{
    public ChatUIManager chatUIManager;

    void Start()
    {
        chatUIManager.AddLeftMessage("王勇男", "任子啊，我是你舅舅王从心啊，还记得我不？");
        chatUIManager.AddRightMessage("那个已经很久没出现过的王勇男么");
        chatUIManager.AddLeftMessage("王勇男", "诶呀是我是我，我最近听说你刚毕业是不是不好找工作？");
    }
}