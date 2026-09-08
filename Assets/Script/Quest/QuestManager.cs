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

    private bool hasActiveEnemy = false;
    private bool isQuestCleared = false;
    private bool isQuestFailed = false;

    // 「次へ」ボタン連打によるSearching()の二重実行を防ぐ
    private bool isSearching = false;

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
                    new Vector3(0.93f, 0.93f, 1)
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

        // 探索終了
        isSearching = false;
    }

    public void OnNextButton()
    {
        // クエスト終了後は操作不可
        if (isQuestCleared || isQuestFailed)
            return;

        // Searching中にもう一度押されても何もしない
        if (isSearching)
            return;

        // 敵が存在している間は次の探索を開始しない
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
        // クエスト終了後は敵を生成しない
        if (isQuestCleared || isQuestFailed)
            return;

        // すでに敵が存在する場合は二重生成しない
        if (hasActiveEnemy)
            return;


        // floors自体がnull、または0件
        if (floors == null || floors.Count == 0)
        {
            Debug.LogWarning(
                "Floorが1つも設定されていません。"
            );

            QuestClear();
            return;
        }


        // Floorのindexが範囲外
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


        // FloorData自体がnull
        if (currentFloor == null)
        {
            Debug.LogWarning(
                $"Floor {currentFloorIndex} がnullです。次のFloorへ進みます。"
            );

            SkipCurrentFloor();
            return;
        }


        // Enemy Listがnullまたは空
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


        // Enemyのindexが範囲外
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


        EnemyData selectedData =
            currentFloor.enemyDatas[currentEnemyIndex];


        // EnemyDataがnull
        if (selectedData == null)
        {
            Debug.LogWarning(
                $"Floor {currentFloorIndex} / Enemy {currentEnemyIndex} のEnemyDataがnullです。"
            );

            SkipInvalidEnemy(currentFloor);
            return;
        }


        // EnemyDataのPrefabがnull
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


        // PrefabにEnemyManagerが付いていない
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

        // 最後のFloorまで終わった
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

        // 同じFloorに次の敵がいる
        if (currentEnemyIndex < currentFloor.enemyDatas.Count)
        {
            EncountEnemy();
            return;
        }

        // このFloorの敵を全部確認した
        currentEnemyIndex = 0;

        currentFloorIndex++;

        // 最後まで終了
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
