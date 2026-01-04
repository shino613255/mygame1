using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownManager : MonoBehaviour
{    
    private void Start()
    {
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "街に到着した。",           
        });
    }
    public void OnToQuestButton() // タウンへ戻るボタンが押されたとき
    {
        SoundManager.instance.PlayButtonSE(0); // ボタンSE再生
    }
}
