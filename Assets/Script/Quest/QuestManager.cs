using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

//クエスト全体を管理
public class QuestManager : MonoBehaviour
{
    public PlayerManager player;
    public PlayerUIManager playerUI; 
    public StageUIManager stageUI;
    public GameObject enemyPrefab;
    public BattleManager battleManager;
    public SceneTransitionManager sceneTransitionManager;
    public GameObject QuestBG;
    // 敵に遭遇するテーブル：1なら遭遇しない、0なら遭遇
    int[] encountTable = { 1, 0, 0, 1, 0, 1 };

    int currentStage = 0; //現在のステージ進行度
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

        playerUI.UpdateUI(player); // UIがあるなら

        // 進行度をUIに反映
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
        // 背景を大きく
        QuestBG.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 1.5f)
            .OnComplete(() => QuestBG.transform.localScale = new Vector3(0.93f, 0.93f, 1));
        // フェードアウト
        SpriteRenderer questBGRenderer = QuestBG.GetComponent<SpriteRenderer>();
        questBGRenderer.DOFade(0, 1.5f)
            .OnComplete(() => questBGRenderer.DOFade(1, 0));
        //1秒缶処理を待機する
        yield return new WaitForSeconds(1.5f);
        
        currentStage++;
        // 進行度をUIに反映
        stageUI.UpdateUI(currentStage);

        if (encountTable.Length <= currentStage)
        {
            Debug.Log("クエストクリア");
            QuestClear();
            // クリア処理
        }
        else if (encountTable[currentStage] == 0)
        {
            EncountEnemy();
        }
        else
        {
            // ボタンを再表示
            stageUI.ShowButtons();
        }
    }

    // Nextボタンが押されたとき
    public void OnNextButton()
    {
        SoundManager.instance.PlayButtonSE(0); // ボタンSE再生
        stageUI.HideButtons(); // ボタンを隠す
        StartCoroutine(Searching());        
    }

    public void OnToTownButton()
    {
        SoundManager.instance.PlayButtonSE(0); // ボタンSE再生
    }

    void EncountEnemy()
    {
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "敵が現れた！"
        });
        stageUI.HideButtons();
        GameObject enemyOgj = Instantiate(enemyPrefab);
        EnemyManager enemy = enemyOgj.GetComponent<EnemyManager>();
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
        SoundManager.instance.PlayButtonSE(2); // クエストクリアSE再生
        stageUI.ShowStageClear();
    }
}
