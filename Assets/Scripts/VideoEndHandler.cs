using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoEndHandler : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "main menu";

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.LoadSceneWithFade(nextSceneName);
        }
        else
        {
            Debug.LogWarning("FadeManager 不存在，直接切场景");

            SceneManager.LoadScene(nextSceneName);
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
        }
    }
}