using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject); 
        }
        else
        {
            Destroy(this.gameObject); 
        }
    }

    public AudioSource AudioSourceBGM;
    // BGMリスト(0:タイトル, 1:タウン, 2:クエスト, 3:バトル)
    public AudioClip[] AudioClipsBGM; 

    public AudioSource AudioSourceSE; 
    public AudioClip[] ButtonSE; 
    
    public void StopBGM() 
    {
        AudioSourceBGM.Stop();
    }
    public void PlayBGM(string sceneName)
    {
        AudioSourceBGM.Stop();
        switch (sceneName)
        {
            default:
            case "Title":
                AudioSourceBGM.clip = AudioClipsBGM[0];
                break;
            case "Town":
                AudioSourceBGM.clip = AudioClipsBGM[1];
                break;
            case "Quest":
                AudioSourceBGM.clip = AudioClipsBGM[2];
                break;
            case "Battle":
                AudioSourceBGM.clip = AudioClipsBGM[3];
                break;
        }
        AudioSourceBGM.Play();
    }
    public void PlayButtonSE(int index)
    {
        AudioSourceSE.PlayOneShot(ButtonSE[index]); 
    }
}