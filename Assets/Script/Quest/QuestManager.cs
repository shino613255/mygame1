using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FloorData;

public class QuestManager : MonoBehaviour
{
    public PlayerManager player;                                                                                
    public PlayerUIManager playerUI;                                                                            
    public StageUIManager stageUI;                                                                              
    public BattleManager battleManager;                                                                         
    public SceneTransitionManager sceneTransitionManager;
    [SerializeField] private List<FloorData> floors = new();
    public GameObject QuestBG;

    // 1なら遭遇しない、0なら遭遇
    int[] encountTable = { 0, 0, 0, 0, 1};                                                              

    int currentStage = 0;                                                                                       
    private void Start()
    {
        PlayerData data = PlayerSelectionManager.Instance.selectedPlayer;

        if (data != null)
        {
            player.Setup(data);
        }
        else
        {
            Debug.LogError("プレイヤーデータが選択されていません！");
        }

        playerUI.UpdateUI(player);                                                                              

        stageUI.UpdateUI(currentStage);                                                                         

        DialogTextManager.instance.SetScenarios(new string[]
        {
            "クエストに出発した！",
            "森の中を進んでいく。",            
        });
    }

    IEnumerator Searching()
    {
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "周囲を探索している...",
        });

        QuestBG.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 1.0f)                                          
            .OnComplete(() => QuestBG.transform.localScale = new Vector3(0.93f, 0.93f, 1));                     

        SpriteRenderer questBGRenderer = QuestBG.GetComponent<SpriteRenderer>();

        // 1.0秒かけてBGを透明(0)にする
        questBGRenderer.DOFade(0, 1.0f)                                                                         
            .OnComplete(() => questBGRenderer.DOFade(1, 0));                                                    

        yield return new WaitForSeconds(1.0f);
        
        currentStage++;                                                             

        stageUI.UpdateUI(currentStage);

        if (encountTable.Length <= currentStage)                                                                
        {
            Debug.Log("クエストクリア");
            QuestClear();　
        }
        else if (encountTable[currentStage] == 0)
        {
            EncountEnemy();
        }
        else
        {
            stageUI.ShowButtons();                                                                              
        }
    }

    public void OnNextButton()
    {
        SoundManager.instance.PlayButtonSE(0);                                                                  
        stageUI.HideButtons();                                                                                  
        StartCoroutine(Searching());                                                                            
    }

    public void OnToTownButton()
    {
        SoundManager.instance.PlayButtonSE(0);                                                                  
    }

    void EncountEnemy()
    {
        stageUI.HideButtons();

        FloorData currentFloor = floors[currentStage - 1];

        if(currentFloor.enemyDatas == null || currentFloor.enemyDatas.Count == 0)                                                        
        {
            stageUI.ShowButtons();
            return;
        }

        DialogTextManager.instance.SetScenarios(new string[]
        {
            "敵が現れた！"
        });

        EnemyData selectedData = currentFloor.enemyDatas[0];

        GameObject enemyObj = Instantiate(selectedData.prefab);                                                     
        EnemyManager enemy = enemyObj.GetComponent<EnemyManager>();                                             
        enemy.data = selectedData;
        battleManager.Setup(enemy);                                                                             
    }

    public void EndBattle()
    {
        stageUI.ShowButtons();                                                                                  
    }

    void QuestClear()
    {
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "クエストクリア！",
            "街に戻ろう。"
        });
        SoundManager.instance.StopBGM();                                                                        
        SoundManager.instance.PlayButtonSE(2);                                                                  
        stageUI.ShowStageClear();                                                                               
    }
}
