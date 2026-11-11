using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    private const float SearchAnimationDuration = 1f;
    private const float SearchZoomScale = 1.2f;
    private const float SearchRestScale = 1.1f;

    public PlayerManager player;
    public PlayerUIManager playerUI;
    public StageUIManager stageUI;
    public BattleManager battleManager;
    public SceneTransitionManager sceneTransitionManager;
    [SerializeField] private List<FloorData> floors = new();
    public GameObject QuestBG;
    private int currentFloorIndex = 0;
    private int currentEnemyIndex = 0;
    private bool hasActiveEnemy = false;
    private bool QuestCleared = false;
    private bool QuestFailed = false;
    private bool EnteringStage = false;
    private bool isWaitingGrowthSelection = false;
    [SerializeField] private GrowthSelectionManager growthSelectionManager;
    [SerializeField] private PlayerData defaultMageData;

    private void Start()
    {
        PlayerData data = defaultMageData;
        if (PlayerSelectionManager.Instance != null &&
            PlayerSelectionManager.Instance.selectedPlayer != null)
        {
            data = PlayerSelectionManager.Instance.selectedPlayer;
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
        stageUI.UpdateUI(currentFloorIndex);
        DialogTextManager.instance.SetScenarios(new string[] { "クエストに出発した！", "森の中を進んでいく。", });
    }

    IEnumerator EnterStage()
    {
        if (QuestCleared || QuestFailed)
        {
            EnteringStage = false;
            yield break;
        }

        DialogTextManager.instance.SetScenarios(new string[] { "周囲を探索している...", });

        QuestBG.transform
            .DOScale(
                new Vector3(SearchZoomScale, SearchZoomScale, SearchZoomScale), 
                SearchAnimationDuration)
            .OnComplete(() => 
                QuestBG.transform.localScale = new Vector3(SearchRestScale, SearchRestScale, 1));

        SpriteRenderer questBGRenderer = QuestBG.GetComponent<SpriteRenderer>();
        questBGRenderer
            .DOFade(0, SearchAnimationDuration)
            .OnComplete(() => questBGRenderer.DOFade(1, 0));

        // 背景の演出を見せてから、次の敵を出すまでの待機時間を設ける
        yield return new WaitForSeconds(SearchAnimationDuration);

        stageUI.UpdateUI(currentFloorIndex);
        EncountEnemy();
        EnteringStage = false;
    }

    public void OnNextButton()
    {
        if (QuestCleared || QuestFailed || EnteringStage || hasActiveEnemy)
        {
            return;
        }

        EnteringStage = true;
        SoundManager.instance.PlayButtonSE(0);
        stageUI.HideButtons();
        StartCoroutine(EnterStage());
    }

    public void OnToTownButton()
    {
        SoundManager.instance.PlayButtonSE(0);
    }

    void EncountEnemy()
    {
        if (QuestCleared || QuestFailed || hasActiveEnemy)
        {
            return;
        }

        if (!TryGetCurrentFloor(out FloorData currentFloor))
        {
            return;
        }

        EnemyData selectedData;

        // ボスフロアの場合は先頭のenemydateをボスとして使う。
        if (currentFloor.isBossFloor)
        {
            selectedData = currentFloor.enemyDatas[0];
        }
        else
        {
            int randomIndex = Random.Range(0, currentFloor.enemyDatas.Count);
            selectedData = currentFloor.enemyDatas[randomIndex];
        }

        if (selectedData == null)
        {
            Debug.LogWarning($"Floor {currentFloorIndex} / Enemy {currentEnemyIndex} のEnemyDataがnullです。");
            SkipInvalidEnemy(currentFloor);
            return;
        }

        if (selectedData.prefab == null)
        {
            Debug.LogWarning($"{selectedData.enemyName} のPrefabが設定されていません。");
            SkipInvalidEnemy(currentFloor);
            return;
        }

        stageUI.HideButtons();
        DialogTextManager.instance.SetScenarios(new string[] { "敵が現れた！" });
        GameObject enemyObj = Instantiate(selectedData.prefab);
        EnemyManager enemy = enemyObj.GetComponent<EnemyManager>();
        if (enemy == null)
        {
            Debug.LogError($"{selectedData.enemyName} のPrefabにEnemyManagerがありません。");
            Destroy(enemyObj);
            SkipInvalidEnemy(currentFloor);
            return;
        }

        enemy.Setup(selectedData);
        hasActiveEnemy = true;
        battleManager.Setup(enemy);
    }
    private bool TryGetCurrentFloor(out FloorData currentFloor)
    {
        currentFloor = null;

        if (floors == null || floors.Count == 0)
        {
            Debug.LogWarning("Floorが1つも設定されていません。");
            QuestClear();
            return false;
        }

        if (currentFloorIndex < 0 || currentFloorIndex >= floors.Count)
        {
            Debug.LogWarning($"FloorIndexが範囲外です。index={currentFloorIndex}");
            QuestClear();
            return false;
        }

        currentFloor = floors[currentFloorIndex];
        if (currentFloor == null)
        {
            Debug.LogWarning($"Floor {currentFloorIndex} がnullです。次のFloorへ進みます。");
            SkipCurrentFloor();
            return false;
        }

        if (currentFloor.enemyDatas == null || currentFloor.enemyDatas.Count == 0)
        {
            Debug.LogWarning($"Floor {currentFloorIndex} にEnemyDataがありません。次のFloorへ進みます。");
            SkipCurrentFloor();
            return false;
        }

        if (currentEnemyIndex < 0 || currentEnemyIndex >= currentFloor.enemyDatas.Count)
        {
            Debug.LogWarning($"EnemyIndexが範囲外です。" + $" index={currentEnemyIndex}" + $" count={currentFloor.enemyDatas.Count}");
            SkipCurrentFloor();
            return false;
        }
        return true;
    }
    private void SkipCurrentFloor()
    {
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
        if (QuestCleared)
        {
            return;
        }

        if (!hasActiveEnemy)
        {
            return;
        }

        hasActiveEnemy = false;
        isWaitingGrowthSelection = true;
        growthSelectionManager.Open();
    }

    public void ContinueAfterGrowthSelection()
    {
        if (!isWaitingGrowthSelection)
        {
            return;
        }

        isWaitingGrowthSelection = false;
        currentFloorIndex++;
        if (currentFloorIndex >= floors.Count)
        {
            QuestClear();
            return;
        }

        EnteringStage = true;
        StartCoroutine(EnterStage());
    }

    void QuestClear()
    {
        if (QuestCleared)
        {
            return;
        }

        QuestCleared = true;
        hasActiveEnemy = false;
        DialogTextManager.instance.SetScenarios(new string[] { "クエストクリア！", "街に戻ろう。" });
        SoundManager.instance.StopBGM();
        SoundManager.instance.PlayButtonSE(2);
        stageUI.ShowStageClear();
    }

    public void QuestFail()
    {
        if (QuestCleared || QuestFailed)
        {
            return;
        }

        QuestFailed = true;
        hasActiveEnemy = false;
        DialogTextManager.instance.SetScenarios(new string[] { "プレイヤーは倒れた。", "クエスト失敗..." });
        SoundManager.instance.StopBGM();
        stageUI.HideButtons();
        sceneTransitionManager.LoadTo("Town");
    }
}
