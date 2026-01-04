using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // シングルトン
    // 利用場所：シーン間でのデータ共有
    public static SoundManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject); // シーンが変わっても破棄しない
        }
        else
        {
            Destroy(this.gameObject); // 既に存在する場合は破棄
        }
    }

    public AudioSource AudioSourceBGM; // BGM用のスピーカー
    public AudioClip[] AudioClipsBGM; // BGMリスト(0:タイトル, 1:タウン, 2:クエスト, 3:バトル)

    public AudioSource AudioSourceSE; // SE用のスピーカー
    public AudioClip[] ButtonSE; // ボタンSE
    
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
        AudioSourceSE.PlayOneShot(ButtonSE[index]); // ボタンSEを一度だけ再生
    }
}