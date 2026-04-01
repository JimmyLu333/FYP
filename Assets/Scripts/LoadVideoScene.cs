using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadVideoScene : MonoBehaviour
{
    public string sceneName = "BeginningCG";

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}