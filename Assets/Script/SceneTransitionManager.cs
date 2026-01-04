using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public void LoadTo(string sceneName)
    {
        FadeIOManager.Instance.FadeOutToIn(() => Lioad(sceneName));        
    }
    void Lioad(string sceneName)
    {
        SoundManager.instance.PlayBGM(sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
