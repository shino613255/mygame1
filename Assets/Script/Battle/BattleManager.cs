using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// PlayerとEnemyの戦闘を管理するクラス
public class BattleManager : MonoBehaviour
{
    [SerializeField] private float playerBaseAccuracy = 0.9f;
    [SerializeField] private float enemyBaseAccuracy = 0.9f;
    [SerializeField] private float elementProcChance = 0.05f; // 5%                                                              
    [SerializeField] private SkillData playerDefaultSkill;    


    public Transform playerDamagePanel;
    public QuestManager questManager;
    public PlayerUIManager playerUI;
    public EnemyUIManager enemyUI;
    public PlayerManager player;
    EnemyManager enemy;
    private BodyPart selectedPart;
    private bool waitingTap;        // タップ待ち中か
    private bool isPlayerTurn;      // 今プレイヤーのターンか

    private List<UnitBase> units = new();   // SPD順の並び替え用


    private void Start()
    {
        enemyUI.gameObject.SetActive(false);
    }


    // 初期設定
    public void Setup(EnemyManager enemymanager)
    {       
        SoundManager.instance.PlayBGM("Battle"); // 戦闘BGM再生
        enemyUI.gameObject.SetActive(true);

        enemy = enemymanager;

        enemyUI.SetupUI(enemy);
        playerUI.SetupUI(player);

        units = new List<UnitBase> { player, enemy };
        StartCoroutine(BattleLoop());

    }

    // ★ここから追加（Setupの下）

    IEnumerator BattleLoop()
    {
        while (player != null && enemy != null && player.IsAlive && enemy.IsAlive)
        {
            var order = units
                .Where(u => u != null && u.IsAlive)
                .OrderByDescending(u => u.spd)
                .ToList();

            foreach (var actor in order)
            {
                if (player == null || enemy == null) yield break;
                if (!player.IsAlive || !enemy.IsAlive) yield break;

                if (actor == player)
                    yield return StartCoroutine(PlayerActByTap()); // プレイヤーはタップ待ち
                else
                    yield return StartCoroutine(EnemyActAuto());   // 敵は自動

                yield return new WaitForSeconds(0.2f);
            }

            yield return null;
        }
    }
    private EnemyPartData GetPartData(BodyPart part)
    {
        if (enemy == null || enemy.data == null || enemy.data.parts == null) return null;
        return enemy.data.parts.FirstOrDefault(p => p != null && p.partId == part);
    }

    // プレイヤーターン：部位タップされるまで待つ
    IEnumerator PlayerActByTap()
    {
        isPlayerTurn = true;
        waitingTap = true;

        // ここで「敵の部位タップをON」にする（Enemy側で実装が必要）
        // enemy.EnablePartsTap(true);

        DialogTextManager.instance.SetScenarios(new string[]
        {
        "プレイヤーのターン。\n攻撃する部位をタップ！"
        });

        while (waitingTap) yield return null; // タップされるまで止まる

        // enemy.EnablePartsTap(false);
        isPlayerTurn = false;

        enemyUI.UpdateUI(enemy);

        if (!enemy.IsAlive)
            yield return StartCoroutine(EndBattle());
    }

    





// 敵ターン：自動で攻撃
    IEnumerator EnemyActAuto()
        {
            if (enemy == null || player == null) yield break;


            // EnemyActAuto() の攻撃直前あたりで
            float finalAcc = enemyBaseAccuracy - enemy.GetAccuracyPenalty();
            bool hit = DamageRule.RollHit(finalAcc, 0f); // evasionは今0でOK
            
            if (!hit)
            {
                DialogTextManager.instance.SetScenarios(new string[]
                {
                "敵の攻撃！\nしかし外れた！"
                });
                yield break;
            }

            // 追加：敵ターン開始時に火傷ダメージ（敵が燃えてたらここで減る）
            int burnDmg = enemy.TickBurnDamage();
            if (burnDmg > 0)
            {
                enemyUI.UpdateUI(enemy);
                DialogTextManager.instance.SetScenarios(new string[]
                {
            $"火傷ダメージ！\n敵は{burnDmg}ダメージ受けた"
                });

                if (enemy == null || !enemy.IsAlive) yield break;
            }

            yield return new WaitForSeconds(0.8f);

            SoundManager.instance.PlayButtonSE(1);
            playerDamagePanel.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);

            int damage = player.TakePhysical(enemy.at);
            playerUI.UpdateUI(player);

            DialogTextManager.instance.SetScenarios(new string[]
            {
            "敵の攻撃！\nプレイヤーは"+damage+"ダメージ受けた"
            });
        }

    // 部位タップされた時に部位側から呼ぶ（EnemyPart → BattleManager）
    // BattleManager.cs（部位タップ時）
    // BattleManager.cs の中：OnEnemyPartTapped をこれに丸ごと置き換え
    public void OnEnemyPartTapped(BodyPart part)
    {
        if (!isPlayerTurn || enemy == null) return;

        waitingTap = false;

        SoundManager.instance.PlayButtonSE(1);

        var result = SkillExecutor.Execute(
            player,             // 攻撃者
            enemy,              // 対象
            playerDefaultSkill, // 使うスキル（Inspectorで設定）
            part                // タップした部位
        );

        enemyUI.UpdateUI(enemy);
        playerUI.UpdateUI(player);

        DialogTextManager.instance.SetScenarios(new string[]
        {
        result.message
        });

        if (!enemy.IsAlive)
            StartCoroutine(EndBattle());
    }





    IEnumerator EndBattle()
    {        
        yield return new WaitForSeconds(2f); // 1秒待機
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "モンスターはやられた。"
        });
        enemyUI.gameObject.SetActive(false);
        //Destroy(enemy.gameObject);
        SoundManager.instance.PlayBGM("Quest"); // クエストBGM再生
        questManager.EndBattle();
        Debug.Log("戦闘終了");
    }
}
