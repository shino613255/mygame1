using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// StageUIを管理(ステージ数のUI/信仰ボタン/街に戻るボタン)の管理
public class StageUIManager : MonoBehaviour
{
    public Text stageText; 
    public GameObject nextButton;
    public GameObject toTownButton;
    public GameObject stageClearText;

    private void Start()
    {
        stageClearText.SetActive(false);
    }

    public void UpdateUI(int currentStage)
    {
        stageText.text = string.Format("ステージ:{0}",currentStage+1);
    }

    public void HideButtons()
    {
        nextButton.SetActive(false);
        toTownButton.SetActive(false);
    }
    public void ShowButtons()
    {
        nextButton.SetActive(true);
        toTownButton.SetActive(true);
    }

    public void ShowStageClear()
    {
        stageClearText.SetActive(true);
        nextButton.SetActive(false);
        toTownButton.SetActive(true);
    }
}
