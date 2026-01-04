using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    public void OnToTownButton() // タウンへ戻るボタンが押されたとき
    {
        SoundManager.instance.PlayButtonSE(0); // ボタンSE再生
    }
}
