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

    private int currentFloorIndex = 0;      
    private int currentEnemyIndex = 0;      
    int currentStage = 0;

    private bool hasActiveEnemy = false;
    private bool isQuestCleared = false;
    private bool isQuestFailed = false;

    // 「次へ」ボタン連打によるSearching()の二重実行を防ぐ
    private bool isSearching = false;

    private bool isWaitingGrowthSelection = false;

    [SerializeField]private GrowthSelectionManager growthSelectionManager;

    [SerializeField]private PlayerData defaultMageData;

    private void Start()
    {
        PlayerData data = defaultMageData;

        if (PlayerSelectionManager.Instance != null &&
            PlayerSelectionManager.Instance.selectedPlayer != null)
        {
            data =
                PlayerSelectionManager.Instance.selectedPlayer;
        }

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
        if (isQuestCleared || isQuestFailed)
        {
            isSearching = false;
            yield break;
        }

        DialogTextManager.instance.SetScenarios(new string[]
        {
        "周囲を探索している...",
        });

        QuestBG.transform
            .DOScale(
                new Vector3(1.2f, 1.2f, 1.2f),
                1.0f
            )
            .OnComplete(() =>
                QuestBG.transform.localScale =
                    new Vector3(1.1f, 1.1f, 1)
            );

        SpriteRenderer questBGRenderer =
            QuestBG.GetComponent<SpriteRenderer>();

        questBGRenderer
            .DOFade(0, 1.0f)
            .OnComplete(() =>
                questBGRenderer.DOFade(1, 0)
            );

        yield return new WaitForSeconds(1.0f);

        currentStage++;

        stageUI.UpdateUI(currentStage);

        EncountEnemy();

        isSearching = false;
    }

    public void OnNextButton()
    {
        if (isQuestCleared || isQuestFailed)
            return;

        if (isSearching)
            return;

        if (hasActiveEnemy)
            return;

        isSearching = true;

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
        if (isQuestCleared || isQuestFailed)
            return;

        if (hasActiveEnemy)
            return;

        if (floors == null || floors.Count == 0)
        {
            Debug.LogWarning(
                "Floorが1つも設定されていません。"
            );

            QuestClear();
            return;
        }

        if (
            currentFloorIndex < 0 ||
            currentFloorIndex >= floors.Count
        )
        {
            Debug.LogWarning(
                $"FloorIndexが範囲外です。index={currentFloorIndex}"
            );

            QuestClear();
            return;
        }


        FloorData currentFloor =
            floors[currentFloorIndex];

        if (currentFloor == null)
        {
            Debug.LogWarning(
                $"Floor {currentFloorIndex} がnullです。次のFloorへ進みます。"
            );

            SkipCurrentFloor();
            return;
        }

        if (
            currentFloor.enemyDatas == null ||
            currentFloor.enemyDatas.Count == 0
        )
        {
            Debug.LogWarning(
                $"Floor {currentFloorIndex} にEnemyDataがありません。次のFloorへ進みます。"
            );

            SkipCurrentFloor();
            return;
        }

        if (
            currentEnemyIndex < 0 ||
            currentEnemyIndex >= currentFloor.enemyDatas.Count
        )
        {
            Debug.LogWarning(
                $"EnemyIndexが範囲外です。" +
                $" index={currentEnemyIndex}" +
                $" count={currentFloor.enemyDatas.Count}"
            );

            SkipCurrentFloor();
            return;
        }

        EnemyData selectedData;

        if (currentFloor.isBossFloor)
        {
            selectedData =
                currentFloor.enemyDatas[0];
        }
        else
        {
            int randomIndex =
                Random.Range(
                    0,
                    currentFloor.enemyDatas.Count
                );

            selectedData =
                currentFloor.enemyDatas[randomIndex];
        }

        if (selectedData == null)
        {
            Debug.LogWarning(
                $"Floor {currentFloorIndex} / Enemy {currentEnemyIndex} のEnemyDataがnullです。"
            );

            SkipInvalidEnemy(currentFloor);
            return;
        }

        if (selectedData.prefab == null)
        {
            Debug.LogWarning(
                $"{selectedData.enemyName} のPrefabが設定されていません。"
            );

            SkipInvalidEnemy(currentFloor);
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
            Debug.LogError(
                $"{selectedData.enemyName} のPrefabにEnemyManagerがありません。"
            );

            Destroy(enemyObj);

            SkipInvalidEnemy(currentFloor);
            return;
        }


        enemy.Setup(selectedData);

        hasActiveEnemy = true;

        battleManager.Setup(enemy);
    }

    private void SkipCurrentFloor()
    {
        // 次のFloorではEnemyIndexを0から始める
        currentEnemyIndex = 0;

        currentFloorIndex++;

        if (currentFloorIndex >= floors.Count)
        {
            QuestClear();
            return;
        }

        stageUI.ShowButtons();
    }

    private void SkipInvalidEnemy(FloorData currentFloor)
    {
        // 問題のあるEnemyDataだけ飛ばす
        currentEnemyIndex++;

        if (currentEnemyIndex < currentFloor.enemyDatas.Count)
        {
            EncountEnemy();
            return;
        }

        currentEnemyIndex = 0;

        currentFloorIndex++;

        if (currentFloorIndex >= floors.Count)
        {
            QuestClear();
            return;
        }

        stageUI.ShowButtons();
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

        if (isQuestCleared)
            return;

        if (!hasActiveEnemy)
            return;

        hasActiveEnemy = false;

        isWaitingGrowthSelection = true;

        Debug.Log(
            "敵を倒したので、成長報酬の選択を待ちます"
        );

        growthSelectionManager.Open();
    }

    public void ContinueAfterGrowthSelection()
    {
        if (!isWaitingGrowthSelection)
            return;

        isWaitingGrowthSelection = false;

        Debug.Log(
            "成長報酬を選択したのでクエストを再開します"
        );        

        currentFloorIndex++;

        if (
            currentFloorIndex >=
            floors.Count
        )
        {
            QuestClear();
            return;
        }

        isSearching = true;

        StartCoroutine(
            Searching()
        );
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
