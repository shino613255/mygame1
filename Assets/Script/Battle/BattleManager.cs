 using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// PlayerとEnemyの戦闘を管理するクラス
public class BattleManager : MonoBehaviour
{
    [SerializeField] private EnemyPartsController enemyParts;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float playerBaseAccuracy = 0.9f;
    [SerializeField] private float enemyBaseAccuracy = 0.9f;
    [SerializeField] private float elementProcChance = 0.05f; // 5%
    [SerializeField] private SkillData playerDefaultSkill;
    [SerializeField] private bool useDefaultSkill = false;

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
        SoundManager.instance.PlayBGM("Battle");
        enemyUI.gameObject.SetActive(true);

        enemy = enemymanager;

        if (mainCamera == null) mainCamera = Camera.main;
        
        enemyUI.SetupUI(enemy);
        playerUI.SetupUI(player);

        units = new List<UnitBase> { player, enemy };
        StartCoroutine(BattleLoop());
    }

    private void Update()
    {
        if (!isPlayerTurn) return;
        if (!waitingTap) return;

        if (Input.GetMouseButtonDown(0))        // タップ（クリック）されたとき
        {
            TryPickBodyPart(Input.mousePosition);
        }

        if (Input.GetKeyDown(KeyCode.Q))        // Qキーでスキル使用切り替え
        {
            useDefaultSkill = !useDefaultSkill;
            Debug.Log("スキル使用切り替え: " + useDefaultSkill);
        }

    }

    private void TryPickBodyPart(Vector2 screenPos)     // 画面上の座標から敵の部位をピックする処理
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (!hit.collider) return;

        var part = hit.collider.GetComponentInParent<BodyPart>();
        if (part == null) return;

        OnBodyPartTapped(part);
    }
    public void OnBodyPartTapped(BodyPart part)     // プレイヤーが敵の部位をタップしたときの処理
    {
        Debug.Log($"OnBodyPartTapped called. isPlayerTurn={isPlayerTurn} waitingTap={waitingTap}");

        if (enemyParts == null)
            enemyParts = part.GetComponentInParent<EnemyPartsController>();

        if (enemyParts == null) return;

        
        if (!isPlayerTurn) return;

        waitingTap = false;

        // タップした部位を選択
        enemyParts.SetSelectedPart(part);
        // 攻撃処理
        AttackContext ctx;

        if (useDefaultSkill && playerDefaultSkill != null)
        {
            if (!player.TrySpendMp(playerDefaultSkill.mpCost))
            {
                Debug.Log("MPが足りませんわ！");
                return;
            }

            ctx = CreateSkillAttackContext(playerDefaultSkill);
        }
        else
        {
            ctx = CreateNormalAttackContext();
        }

        if (ctx.sourceSkill != null)
        {
            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            clickPos.z = 0f;

            PlaySkillEffect(ctx.sourceSkill, clickPos);
        }
        // ここで、攻撃の命中判定やダメージ計算を行う
        var result = enemyParts.ApplyAttack(ctx);

        // UI更新
        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);
        }
        playerUI.UpdateUI(player);

        string attackMessage;

        if (ctx.sourceSkill != null)
        {
            attackMessage = $"{ctx.sourceSkill.skillName}！\n{part.GetPartNameJP()}に攻撃！";
        }
        else
        {
            attackMessage = $"{part.GetPartNameJP()}を攻撃！";
        }

        if (result.mainDamage > 0)
        {
            attackMessage += $"\n本体に{result.mainDamage}ダメージ！";
        }

        if (result.partDamage > 0)
        {
            attackMessage += $"\n部位に{result.partDamage}ダメージ！";
        }
        else if (part.IsBroken)
        {
            attackMessage += "\nその部位はもう破壊されていますわ！";
        }

        DialogTextManager.instance.SetScenarios(new[]
        {
            attackMessage
        });

        Debug.Log(attackMessage);
    }
    private AttackContext CreateNormalAttackContext()       // 通常攻撃のAttackContextを作る用
    {
        return new AttackContext
        {
            baseDamage = DamageRule.CalcPhysical(player.at, enemy.def, 1f, 1),
            mainDamageRate = 1f,
            partDamageRate = 1f,
            canApplyStatus = false,
            sourceSkill = null
        };
    }
    private AttackContext CreateSkillAttackContext(SkillData skill)     // スキル攻撃のAttackContextを作る用
    {
        return new AttackContext
        {
            baseDamage = DamageRule.CalcPhysical(player.at, enemy.def, skill.multiplier, 1) + skill.power,
            mainDamageRate = skill.mainDamageRate,
            partDamageRate = skill.partDamageRate,
            canApplyStatus = skill.statusEffect != null,
            sourceSkill = skill
        };
    }

    public static BattleManager Instance;

    void Awake()
    {
        Instance = this;
    }
    public void PlaySkillEffect(SkillData skill, Vector3 worldPos)
    {        
        if (skill == null) return;
        if (skill.effectPrefab == null) return;

        Vector3 pos = worldPos + (Vector3)skill.effectOffset;
        pos.z = 0f; // Zは0固定

        GameObject effect = Instantiate(
            skill.effectPrefab,
            pos,
            Quaternion.identity     // 回転は無し
        );

        Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.sortingLayerName = "Default";
            r.sortingOrder = 10;
        }

        Destroy(effect, skill.effectDuration);
    }

    IEnumerator PlayerActByTap()        // プレイヤーターン：タップ待ちで攻撃
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
        // 攻撃のテキストメッセージを読める時間を作る
        yield return new WaitForSeconds(1.5f);

        isPlayerTurn = false;

        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);
        }

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
    private SkillData PickEnemySkillOrNormal()      // 敵のスキル（EnemyData.attackSkill と skillList）からランダムで1つ選ぶ。なければ null を返す（呼び出し側で通常攻撃にフォールバック）
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
        SoundManager.instance.PlayBGM("Quest"); // クエストBGM再生
        questManager.EndBattle();
        Debug.Log("戦闘終了");
    }
}
