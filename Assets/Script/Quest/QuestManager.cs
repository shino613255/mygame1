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

    private int currentFloorIndex = 0;      // 今何層目か
    private int currentEnemyIndex = 0;      // その階層の何体目の敵か
    int currentStage = 0;

    private bool hasActiveEnemy = false;    // 現在戦闘対象の敵が存在しているかどうかのフラグ
    private bool isQuestCleared = false;    // クエストクリア済みかどうかのフラグ
    private bool isQuestFailed = false;

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
        if (isQuestCleared || isQuestFailed) yield break;
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

        EncountEnemy();
    }

    public void OnNextButton()
    {
        if (isQuestCleared || isQuestFailed) return;
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
        if (isQuestCleared || isQuestFailed) return;
        if (hasActiveEnemy) return;

        if(currentFloorIndex >= floors.Count)                                                                        
        {
            QuestClear();
            return;
        }

        FloorData currentFloor = floors[currentFloorIndex];

        if(currentFloor.enemyDatas == null ||
            currentFloor.enemyDatas.Count == 0)                                                    
        {
            stageUI.ShowButtons();
            return;
        }

        EnemyData selectedData 
            = currentFloor.enemyDatas[currentEnemyIndex];        // 現在の階層の敵データを取得

        if (selectedData == null || selectedData.prefab == null)
        {
            Debug.LogWarning("EnemyData または Prefab が未設定です。");
            stageUI.ShowButtons();
            return;
        }

        stageUI.HideButtons();

        DialogTextManager.instance.SetScenarios(new string[]
        {
            "敵が現れた！"
        });

        GameObject enemyObj =
            Instantiate(selectedData.prefab);
        
        EnemyManager enemy =
            enemyObj.GetComponent<EnemyManager>();

        if (enemy == null)
        {
            Destroy(enemyObj);
            stageUI.ShowButtons();
            return;
        }

        enemy.Setup(selectedData);

        hasActiveEnemy = true;

        battleManager.Setup(enemy);                                                                             
    }

    private void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.BattleEnded += EndBattle;
        }
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.BattleEnded -= EndBattle;
        }
    }

    public void EndBattle()
    {
        Debug.Log("QuestManager：戦闘終了通知を受信");

        if (isQuestCleared) return;
        if (!hasActiveEnemy) return;

        hasActiveEnemy = false;

        FloorData currentFloor = floors[currentFloorIndex];                 // 現在の階層のデータを取得

        currentEnemyIndex++;

        if (currentEnemyIndex < currentFloor.enemyDatas.Count)              // その階層の敵が無くなるまで戦闘
        {
            EncountEnemy();
            return;
        }

        currentEnemyIndex = 0;                                              // 次の階層に進むために敵インデックスをリセット
        currentFloorIndex++;

        if (currentFloorIndex >= floors.Count)
        {
            QuestClear();
            return;
        }

        stageUI.ShowButtons();
    }

    void QuestClear()
    {
        if (isQuestCleared) return;

        isQuestCleared = true;
        hasActiveEnemy = false;

        DialogTextManager.instance.SetScenarios(new string[]
        {
            "クエストクリア！",
            "街に戻ろう。"
        });

        SoundManager.instance.StopBGM();                                                                        
        SoundManager.instance.PlayButtonSE(2);                                                                  
        stageUI.ShowStageClear();                                                                               
    }

    public void QuestFailed()
    {
        if (isQuestCleared || isQuestFailed)
            return;

        isQuestFailed = true;
        hasActiveEnemy = false;

        DialogTextManager.instance.SetScenarios(
            new string[]
            {
            "プレイヤーは倒れた。",
            "クエスト失敗..."
            }
        );

        SoundManager.instance.StopBGM();

        stageUI.HideButtons();

        sceneTransitionManager.LoadTo("Town");
    }
}
