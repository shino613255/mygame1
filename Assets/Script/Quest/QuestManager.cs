using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class QuestManager : MonoBehaviour
{
    public PlayerManager player;                                                                                // PlayerManagerの登録
    public PlayerUIManager playerUI;                                                                            // PlayerUIManagerの登録
    public StageUIManager stageUI;                                                                              // StageUIManagerの登録
    public BattleManager battleManager;                                                                         // BattleManagerの登録
    public SceneTransitionManager sceneTransitionManager;                                                       // SceneTransitionManagerの登録
    [SerializeField] private EnemyData[] enemyDatas;                                                            // 敵のデータを格納する配列
    public GameObject QuestBG;                                                                                  // クエスト背景

    int[] encountTable = { 0, 0, 0, 0, 1};                                                                      // 敵に遭遇するテーブル：1なら遭遇しない、0なら遭遇

    int currentStage = 0;                                                                                       //ステージ進行度
    private void Start()
    {
        PlayerData data = PlayerSelectionManager.Instance.selectedPlayer;                                       // キャラ選択画面で選んだプレイヤーデータを取得

        if (data != null)
        {
            player.Setup(data);                                                                                 //  PlayerDateをPlayerManagerにセットアップ
        }
        else
        {
            Debug.LogError("プレイヤーデータが選択されていません！");
        }

        playerUI.UpdateUI(player);                                                                              // PlayerUIの更新

        stageUI.UpdateUI(currentStage);                                                                         // 進行度のUIの更新を繁栄

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

        QuestBG.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 1.0f)                                          // 1.0秒かけてx,y,zを1.2倍に拡大
            .OnComplete(() => QuestBG.transform.localScale = new Vector3(0.93f, 0.93f, 1));                     // 拡大後に元のサイズに戻す

        SpriteRenderer questBGRenderer = QuestBG.GetComponent<SpriteRenderer>();
        questBGRenderer.DOFade(0, 1.0f)                                                                         // 1.5秒かけてBGを透明(0)にする
            .OnComplete(() => questBGRenderer.DOFade(1, 0));                                                    // 透明化後に元の不透明度(1)に戻す

        yield return new WaitForSeconds(1.0f);
        
        currentStage++;                                                             

        stageUI.UpdateUI(currentStage);

        if (encountTable.Length <= currentStage)                                                                // 進行度がテーブルの長さを超えた場合はクエストクリア
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
            stageUI.ShowButtons();                                                                              // 進むボタンを表示
        }
    }

    public void OnNextButton()
    {
        SoundManager.instance.PlayButtonSE(0);                                                                  // ボタンSE再生
        stageUI.HideButtons();                                                                                  // ボタンを隠す
        StartCoroutine(Searching());                                                                            // Searchingコルーチンを開始
    }

    public void OnToTownButton()
    {
        SoundManager.instance.PlayButtonSE(0);                                                                  // ボタンSE再生
    }

    private EnemyData GetRandomEnemyData()
    {
        int index = Random.Range(0, enemyDatas.Length);
        return enemyDatas[index];
    }

    void EncountEnemy()
    {

        DialogTextManager.instance.SetScenarios(new string[]
        {
            "敵が現れた！"
        });

        stageUI.HideButtons();                                                                                  // ボタンを隠す

        EnemyData selectedData = GetRandomEnemyData();

        GameObject enemyObj = Instantiate(selectedData.prefab);                                                 // 敵のプレハブを生成

        EnemyManager enemy = enemyObj.GetComponent<EnemyManager>();                                             // EnemyManagerを取得
        enemy.data = selectedData;
        battleManager.Setup(enemy);                                                                             // BattleManagerに敵をセットアップ
    }

    public void EndBattle()
    {
        stageUI.ShowButtons();                                                                                  // 進むボタンを表示
    }

    void QuestClear()
    {
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "クエストクリア！",
            "街に戻ろう。"
        });
        SoundManager.instance.StopBGM();                                                                        // BGM停止
        SoundManager.instance.PlayButtonSE(2);                                                                  // クエストクリアSE再生
        stageUI.ShowStageClear();                                                                               // ステージクリアUIを表示
    }
}
