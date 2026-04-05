using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// StageUIを管理(ステージ数のUI/進むボタン/街に戻るボタン/ステージクリア)の管理
public class StageUIManager : MonoBehaviour
{
    public Text stageText; 
    public GameObject nextButton;
    public GameObject toTownButton;
    public GameObject stageClearText;

    private void Start()        // ステージクリア非表示            
    {
        stageClearText.SetActive(false);
    }

    public void UpdateUI(int currentStage)      // ステージ番号の更新
    {
        stageText.text = string.Format("ステージ:{0}",currentStage+1);
    }

    public void HideButtons()       // ボタンの非表示
    {
        nextButton.SetActive(false);
        toTownButton.SetActive(false);
    }
    public void ShowButtons()       // ボタンの表示
    {
        nextButton.SetActive(true);
        toTownButton.SetActive(true);
    }

    public void ShowStageClear()        // クエストクリア時の表示
    {
        stageClearText.SetActive(true);
        nextButton.SetActive(false);
        toTownButton.SetActive(true);
    }
}
