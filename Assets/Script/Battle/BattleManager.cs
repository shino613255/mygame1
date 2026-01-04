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
    private bool waitingTap;        // タップ待ち中か
    private bool isPlayerTurn;      // 今プレイヤーのターンか

    private List<UnitBase> units = new();   // SPD順の並び替え用


    private void Start()
    {
        enemyUI.gameObject.SetActive(false);
    }

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
    public void OnEnemyTapped()
    {
        if (!isPlayerTurn) return;

        waitingTap = false;

        var result = SkillExecutor.Execute(
            player,
            enemy,
            playerDefaultSkill,
            0f
        );

        enemyUI.UpdateUI(enemy);
        playerUI.UpdateUI(player);

        DialogTextManager.instance.SetScenarios(new[]
        {
        result.message
    });
    }

    IEnumerator PlayerActByTap()
    {
        isPlayerTurn = true;
        waitingTap = true;

        DialogTextManager.instance.SetScenarios(new string[]
        {
        "プレイヤーのターン。\n敵をタップ！"
        });

        // ★敵タップされるまで待つ
        while (waitingTap)
            yield return null;

        isPlayerTurn = false;

        enemyUI.UpdateUI(enemy);

        if (!enemy.IsAlive)
            yield return StartCoroutine(EndBattle());
    }


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




    // BattleManager.cs に追加（クラス内のどこでもOK）
    private SkillData PickEnemySkillOrNormal()
    {
        if (enemy == null || enemy.data == null) return null;

        // 1) 通常攻撃（EnemyData.attackSkill）が入ってたら、それは常に候補に入れる
        List<SkillData> pool = new();

        if (enemy.data.attackSkill != null)
            pool.Add(enemy.data.attackSkill);

        // 追加スキル（EnemyData.skillSlots or skillList）も候補に入れる（最大4想定でもOK）
        if (enemy.data.skillList != null && enemy.data.skillList.Count > 0)
        {
            foreach (var s in enemy.data.skillList)
            {
                if (s == null) continue;
                pool.Add(s);
            }
        }

        // 候補が無いなら null（呼び出し側で通常攻撃にフォールバック）
        if (pool.Count == 0) return null;

        // 候補からランダムで1つ
        return pool[Random.Range(0, pool.Count)];
    }

    // 敵ターン：自動で攻撃
    // BattleManager.cs の EnemyActAuto をこれに丸ごと置き換え
    IEnumerator EnemyActAuto()
    {
        if (enemy == null || player == null) yield break;

        // ① 敵ターン開始時：火傷ダメージ（敵が燃えてるなら減る）
        int burnDmg = enemy.TickBurnDamage();
        if (burnDmg > 0)
        {
            enemyUI.UpdateUI(enemy);
            DialogTextManager.instance.SetScenarios(new string[]
            {
            $"火傷ダメージ！\n敵は{burnDmg}ダメージ受けた"
            });

            if (enemy == null || !enemy.IsAlive) yield break;
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.8f);

        // ② 敵の命中判定（欠損の命中低下を反映）
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

        // ③ スキルがあるなら「スキル + 通常攻撃(attackSkill)」を同じプールでランダム
        SkillData skill = PickEnemySkillOrNormal();

        // ④ スキルが無いなら従来の通常攻撃にフォールバック
        if (skill == null)
        {
            SoundManager.instance.PlayButtonSE(1);
            playerDamagePanel.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);

            int dmg = player.TakePhysical(enemy.at);
            playerUI.UpdateUI(player);

            DialogTextManager.instance.SetScenarios(new string[]
            {
            $"敵の攻撃！\nプレイヤーは{dmg}ダメージ受けた"
            });
            yield break;
        }

        // ⑤ スキル実行（敵→プレイヤーなので部位は固定でOK）
        var result = SkillExecutor.Execute(
            enemy,
            player,
            skill,
            0f
        );

        playerUI.UpdateUI(player);

        DialogTextManager.instance.SetScenarios(new string[]
        {
        result.message
        });
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
