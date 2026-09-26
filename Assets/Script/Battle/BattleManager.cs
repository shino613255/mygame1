using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private EnemyPartsController enemyParts;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float playerBaseAccuracy = 1f;
    [SerializeField] private float enemyBaseAccuracy = 1f;
    [SerializeField] private float elementProcChance = 0.05f;           // 属性攻撃の追加効果が発動する確率                  
    [SerializeField] private SkillData selectedPlayerSkill;
    [SerializeField] private bool useSelectedSkill = false;
    [SerializeField] private List<SkillSlotUI> skillSlots;

    [Header("UI References")]
    [SerializeField] private GameObject skillSelectionPanel;
    [SerializeField] private GameObject playerStatusPanel;
    [SerializeField] private GameObject skillButton;

    public event System.Action BattleEnded;
    public Transform screenShakeTarget;                                 // プレイヤーがダメージを受けたときに揺れすようにするため
    public QuestManager questManager;
    public PlayerUIManager playerUI;
    public EnemyUIManager enemyUI;
    public PlayerManager player;
    public PlayerData playerData;
    private EnemyManager enemy;
    private EnemyAI enemyAI;
    private bool waitingTap;
    private bool isPlayerTurn;

    [SerializeField] private PlayerData defaultMageData;

    private bool isPartViewVisible = false;
    // 現在戦闘中か
    private bool isBattleRunning = false;

    // 戦闘終了処理中か
    private bool isEndingBattle = false;
    void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        Instance = this;
    }

    public void Setup(EnemyManager enemymanager)
    {
        skillButton.SetActive(true);
        playerStatusPanel.SetActive(true);
        // EnemyManagerが渡されていない
        if (enemymanager == null)
        {
            Debug.LogError(
                "BattleManager.Setup にnullのEnemyManagerが渡されました。"
            );

            return;
        }

        // すでに戦闘中なら二重開始しない
        if (isBattleRunning)
        {
            Debug.LogWarning(
                "すでに戦闘中なのでBattleManager.Setupを無視します。"
            );

            return;
        }


        isBattleRunning = true;

        isEndingBattle = false;

        enemyAI.ResetBattleState();

        SoundManager.instance.PlayBGM("Battle");

        enemyUI.gameObject.SetActive(true);

        enemy = enemymanager;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        enemyUI.SetupUI(enemy);

        playerUI.SetupUI(player);

        StartCoroutine(BattleLoop());
    }

    private void Start()
    {
        skillButton.SetActive(false);
        skillSelectionPanel.SetActive(false);
        playerStatusPanel.SetActive(false);
        enemyUI.gameObject.SetActive(false);
        playerData = PlayerSelectionManager.Instance.selectedPlayer;

        if (playerData == null)
        {
            playerData = defaultMageData;
        }

        if (playerData != null && player != null)
        {
            player.Setup(playerData);

            SetupSkillSlots();
        }
    }    

    public void ToggleSkillPanel()
    {
        // 戦闘中ではない
        if (!isBattleRunning)
            return;

        // プレイヤーターンではない
        if (!isPlayerTurn)
            return;

        // 戦闘終了処理中
        if (isEndingBattle)
            return;

        skillSelectionPanel.SetActive(
            !skillSelectionPanel.activeSelf
        );
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            isPartViewVisible =
                !isPartViewVisible;

            if (enemy != null)
            {
                enemy.SetPartViewVisible(
                    isPartViewVisible
                );
            }
        }

        if (!isPlayerTurn) return;

        if (skillSelectionPanel.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TryPickBodyPart(Input.mousePosition);
        }
    }

    private void SetupSkillSlots()
    {
        if (playerData == null)
            return;

        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (
                playerData.startSkills != null &&
                i < playerData.startSkills.Count
            )
            {
                skillSlots[i].SetSkill(
                    playerData.startSkills[i],
                    player
                );
            }
            else
            {
                skillSlots[i].SetSkill(
                    null,
                    player
                );
            }
        }
    }

    public void OnSkillSelected(SkillData selectedSkill)
    {
        // 戦闘中でなければスキルを使用しない
        if (!isBattleRunning)
            return;

        // 戦闘終了処理中
        if (isEndingBattle)
            return;

        // プレイヤーターン以外
        if (!isPlayerTurn)
            return;

        // 敵が存在しない
        if (enemy == null)
            return;

        // 敵がすでに死亡している
        if (!enemy.IsAlive)
            return;

        if (selectedSkill == null)
        {
            Debug.LogWarning
                ("選択されたスキルがnullですわ！");
            return;
        }

        if (
            (
                selectedSkill.skillType == SkillType.Heal ||
                selectedSkill.skillType == SkillType.RecoverMp
            ) &&
            selectedSkill.targetType == TargetType.Self
        )
        {
            var result = SkillExecutor.Execute(player, player, selectedSkill);          // スキルを実行して結果を取得            
            if (!result.executed)
            {
                DialogTextManager.instance.SetScenarios(new string[]                    // スキルが実行できなかった場合のメッセージを表示
                {
                    result.message
                });
                return;
            }
            PlaySkillEffect(selectedSkill, Vector3.zero);                                // プレイヤーの位置にスキルのエフェクトを再生

            playerUI.UpdateUI(player);

            skillSelectionPanel.SetActive(false);

            waitingTap = false;
            isPlayerTurn = false;

            DialogTextManager.instance.SetScenarios(new string[]                        // スキルの結果を表示
                {
                    result.message
                });

            return;
        }
        selectedPlayerSkill = selectedSkill;
        useSelectedSkill = true;
        waitingTap = true;

        skillSelectionPanel.SetActive(false);

        Debug.Log($"スキル「{selectedSkill.skillName}」を選択しましたわ！");
    }

    private void TryPickBodyPart(Vector2 screenPos)
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);                    // 画面上の座標をゲーム内の座標に変換
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);                   // クリックした一点にColliderがあるか確認する

        if (!hit.collider) return;

        var part = hit.collider.GetComponentInParent<BodyPart>();                       // Colliderの親にBodyPartがあるか確認する
        if (part == null) return;

        OnBodyPartTapped(part);
    }

    private int CalculateDamage(SkillData skill)
    {
        // 通常攻撃
        if (skill == null)
        {
            return DamageRule.CalcPhysical(
                player.at,
                enemy.def,
                1f,
                1
            );
        }

        // 魔法スキル
        if (skill.skillType == SkillType.Magic)
        {
            return DamageRule.CalcMagic(
                player.mag,
                enemy.mdef,
                skill.multiplier,
                1
            ) + skill.power;
        }

        // 物理スキル
        return DamageRule.CalcPhysical(
            player.at,
            enemy.def,
            skill.multiplier,
            1
        ) + skill.power;
    }

    private AttackContext CreateNormalAttackContext()
    {
        return new AttackContext
        {
            baseDamage = CalculateDamage(null),

            mainDamageRate = 1f,
            partDamageRate = 1f,
            canApplyStatus = false,
            sourceSkill = null
        };
    }
    private AttackContext CreateSkillAttackContext(SkillData skill)
    {
        return new AttackContext
        {
            baseDamage = CalculateDamage(skill),

            mainDamageRate = skill.mainDamageRate,
            partDamageRate = skill.partDamageRate,

            canApplyStatus = skill.statusEffect != null,
            sourceSkill = skill
        };
    }

    public void OnBodyPartTapped(BodyPart part)
    {
        // BodyPartが存在しない
        if (part == null)
            return;

        // 戦闘中ではない
        if (!isBattleRunning)
            return;

        // 戦闘終了処理中
        if (isEndingBattle)
            return;

        // プレイヤーターンではない
        if (!isPlayerTurn)
            return;

        // 敵が存在しない
        if (enemy == null)
            return;

        // 敵がすでに死亡している
        if (!enemy.IsAlive)
            return;

        enemyParts = part.GetComponentInParent<EnemyPartsController>();             // クリックされた部位の親にEnemyPartsControllerがあるか確認する

        if (enemyParts == null)
        {
            Debug.LogWarning(
                $"部位「{part.GetPartNameJP()}」の親にEnemyPartsControllerが見つかりませんでしたわ！");
            return;
        }

        enemyParts.SetSelectedPart(part);

        // 攻撃の情報を作る
        AttackContext ctx;

        if (useSelectedSkill && selectedPlayerSkill != null)
        {
            if (!player.TryUseMp(selectedPlayerSkill.mpCost))
            {
                Debug.Log("MPが足りませんわ！");
                return;
            }

            ctx = CreateSkillAttackContext(selectedPlayerSkill);
        }
        else
        {
            ctx = CreateNormalAttackContext();
        }

        waitingTap = false;
        isPlayerTurn = false;

        if (ctx.sourceSkill != null)
        {
            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            clickPos.z = 0f;

            PlaySkillEffect(ctx.sourceSkill, clickPos);
        }

        float accuracy =
            ctx.sourceSkill != null
                ? ctx.sourceSkill.accuracy
                : playerBaseAccuracy;

        bool hit = DamageRule.RollHit(
            accuracy,
            enemy.evasionRate
        );

        if (!hit)
        {
            if (useSelectedSkill)
            {
                useSelectedSkill = false;
                selectedPlayerSkill = null;
            }
            playerUI.UpdateUI(player);

            DialogTextManager.instance.SetScenarios(new string[]
            {
        $"{player.name}の攻撃！\nしかし{enemy.name}に当たらなかった！"
            });

            return;
        }

        // 攻撃前にすでに破壊されていたか記録
        bool wasBroken = part.IsBroken;

        // EnemyPartsControllerで本体・部位へのダメージを計算して適用する
        var result = enemyParts.ApplyAttack(ctx);

        // 今回の攻撃で壊れたか
        bool justBroken = !wasBroken && part.IsBroken;

        if (
            ctx.sourceSkill != null &&
            ctx.sourceSkill.statusEffect != null &&
            enemy != null &&
            enemy.IsAlive
)
        {
            StatusEffectData effect =
                ctx.sourceSkill.statusEffect;

            // SkillData側に個別設定があれば、StatusEffectDataより優先する
            float chance =
                ctx.sourceSkill.applyChance > 0f
                    ? ctx.sourceSkill.applyChance
                    : effect.applyChance;

            if (Random.value <= chance)
            {
                // スキルの状態異常効果ターンに関する設定
                int duration =
                    ctx.sourceSkill.overrideDurationTurns > 0
                        ? ctx.sourceSkill.overrideDurationTurns
                        : effect.durationTurns;

                if (effect.type == StatusEffectType.Burn)
                {
                    enemy.ApplyBurn(effect, duration);
                }
            }
        }

        if (useSelectedSkill)
        {
            useSelectedSkill = false;
            selectedPlayerSkill = null;
        }

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
            attackMessage +=
                $"\n部位に{result.partDamage}ダメージ！";
        }

        if (justBroken)
        {
            switch (part.partType)
            {
                case PartType.Face:
                    attackMessage +=
                        "\n頭を破壊した！" +
                        "\n敵の防御力が10低下した！";
                    break;

                case PartType.Hand:
                    attackMessage +=
                        "\n手を破壊した！" +
                        "\n敵の命中率が20%低下した！";
                    break;

                case PartType.Leg:
                    attackMessage +=
                        "\n脚を破壊した！" +
                        "\n2回行動できるようになった！";
                    break;
            }
        }
        else if (wasBroken)
        {
            attackMessage +=
                "\nその部位はもう破壊されていますわ！";
        }

        DialogTextManager.instance.SetScenarios(new[]
        {
            attackMessage
        });

        Debug.Log(attackMessage);
    }

    public static BattleManager Instance;


    public void PlaySkillEffect(SkillData skill, Vector3 worldPos)
    {
        if (skill == null) return;
        if (skill.effectPrefab == null) return;

        Vector3 pos = worldPos + (Vector3)skill.effectOffset;                           // エフェクトの表示位置をworldPosにeffectOffsetを加えた位置に設定
        pos.z = 0f;

        GameObject effect = Instantiate(
            skill.effectPrefab,
            pos,
            Quaternion.identity
        );

        Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);          // エフェクトのRendererを取得

        foreach (var r in renderers)
        {
            r.sortingLayerName = "Default";
            r.sortingOrder = 10;
        }

        Destroy(effect, skill.effectDuration);
    }

    IEnumerator BattleLoop()
    {
        EnemyManager battleEnemy = enemy;

        while (
            player != null &&
            enemy != null &&
            player.IsAlive &&
            enemy.IsAlive
        )
        {
            // 部位破壊状態から行動回数を取得
            int playerActionCount =
                enemy.GetPlayerActionCount();


            // 行動可能回数ぶんプレイヤーターン
            for (int i = 0; i < playerActionCount; i++)
            {
                yield return StartCoroutine(PlayerActByTap());

                if (enemy != battleEnemy)
                {
                    yield break;
                }

                if (
                    player == null ||
                    enemy == null ||
                    !player.IsAlive ||
                    !enemy.IsAlive
                )
                {
                    yield break;
                }

                // 1回目の攻撃で脚を破壊した場合にも、
                // そのターンから2回目を行えるよう再取得
                playerActionCount =
                    enemy.GetPlayerActionCount();
            }

            yield return new WaitForSeconds(0.5f);

            yield return StartCoroutine(EnemyActAuto());

            // Player死亡
            if (
                player == null ||
                !player.IsAlive
            )
            {
                questManager.QuestFailed();
                yield break;
            }

            // Enemy死亡
            if (
                enemy == null ||
                !enemy.IsAlive
            )
            {
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    IEnumerator PlayerActByTap()
    {
        isPlayerTurn = true;
        waitingTap = true;


        // 毒ダメージ
        int poisonDamage =
            player.TickPoisonDamage();

        if (poisonDamage > 0)
        {
            playerUI.UpdateUI(player);

            DialogTextManager.instance.SetScenarios(
                new string[]
                {
                $"毒ダメージ！\n" +
                $"{poisonDamage}ダメージ受けた！"
                }
            );

            yield return new WaitForSeconds(1.0f);


            // 毒で死亡
            if (!player.IsAlive)
            {
                waitingTap = false;
                isPlayerTurn = false;

                questManager.QuestFailed();

                yield break;
            }
        }


        DialogTextManager.instance.SetScenarios(
            new string[]
            {
            "プレイヤーのターン。\n敵をクリック！"
            }
        );


        // プレイヤーが攻撃するまで待つ
        while (waitingTap)
        {
            yield return null;
        }


        // 攻撃メッセージを見る時間
        yield return new WaitForSeconds(1.5f);


        isPlayerTurn = false;


        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);
        }


        if (
            enemy != null &&
            !enemy.IsAlive
        )
        {
            yield return StartCoroutine(
                EndBattle()
            );
        }
    }

    IEnumerator EnemyActAuto()
    {
        if (enemy == null || player == null)
            yield break;

        // Buffの残りターンを進める
        enemy.TickDefenseBuff();
        enemy.TickMagicDefenseBuff();

        // Cooldownは火傷の有無に関係なく毎ターン進める
        SkillCooldowns cooldowns =
            enemy.GetComponent<SkillCooldowns>();

        if (cooldowns != null)
        {
            cooldowns.Tick();
        }

        enemyUI.UpdateUI(enemy);

        // 火傷ダメージ
        int burnDmg = enemy.TickBurnDamage();

        if (burnDmg > 0)
        {
            // 火傷で倒れた場合
            if (enemy == null || !enemy.IsAlive)
            {
                yield return StartCoroutine(EndBattle());
                yield break;
            }

            enemyUI.UpdateUI(enemy);

            DialogTextManager.instance.SetScenarios(
                new string[]
                {
                $"火傷ダメージ！\n敵は{burnDmg}ダメージ受けた"
                }
            );

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.5f);

        // まず敵が使うスキルを決める
        SkillData skill = enemyAI.ChooseSkill(enemy);

        // nullなら通常攻撃
        if (skill == null)
        {
            float finalAcc =
                enemyBaseAccuracy -
                enemy.GetAccuracyPenalty();

            finalAcc = Mathf.Clamp01(finalAcc);

            bool hit =
                DamageRule.RollHit(
                    finalAcc,
                    0f
                );

            if (!hit)
            {
                DialogTextManager.instance.SetScenarios(
                    new string[]
                    {
                    "敵の攻撃！\nしかし外れた！"
                    }
                );

                yield break;
            }

            SoundManager.instance.PlayButtonSE(1);

            screenShakeTarget.DOShakePosition(
                0.3f,
                0.5f,
                20,
                0,
                false,
                true
            );

            int dmg =
                player.TakePhysical(enemy.at);

            playerUI.UpdateUI(player);

            DialogTextManager.instance.SetScenarios(
                new string[]
                {
                $"敵の攻撃！\nプレイヤーは{dmg}ダメージ受けた"
                }
            );

            yield return new WaitForSeconds(0.5f);

            yield break;
        }

        // スキルの対象を決める
        UnitBase target;

        if (skill.targetType == TargetType.Self)
        {
            target = enemy;
        }
        else
        {
            target = player;
        }

        // スキル側の命中判定・Buff・Healなどは
        // SkillExecutorに任せる
        var result =
            SkillExecutor.Execute(
                enemy,
                target,
                skill,
                0f
            );

        if (result.executed)
        {
            PlaySkillEffect(
                skill,
                enemy.transform.position
            );
        }

        enemyUI.UpdateUI(enemy);

        if (
            result.hit &&
            skill.targetType != TargetType.Self
        )
        {
            SoundManager.instance.PlayButtonSE(1);

            screenShakeTarget.DOShakePosition(
                0.3f,
                0.5f,
                20,
                0,
                false,
                true
            );
        }

        if (result.crit)
        {
            Debug.Log("クリティカル！");
        }

        playerUI.UpdateUI(player);

        DialogTextManager.instance.SetScenarios(
            new string[]
            {
            result.message
            }
        );

        yield return new WaitForSeconds(0.5f);
    }


    IEnumerator EndBattle()
    {
        bool defeatedBoss =
            enemy != null &&
            enemy.data != null &&
            enemy.data.enemyType == EnemyType.Boss;

        if (isEndingBattle)
            yield break;

        isEndingBattle = true;

        string deleteEnemyName =
            enemy != null ? enemy.name : "敵";

        // ここから戦闘中ではない
        isBattleRunning = false;
        isPlayerTurn = false;
        waitingTap = false;

        skillSelectionPanel.SetActive(false);
        playerStatusPanel.SetActive(false);

        DialogTextManager.instance.SetScenarios(new string[]
        {
            $"{deleteEnemyName}を倒した"
        });
        enemyUI.gameObject.SetActive(false);

        if (enemy != null)
        {
            Destroy(enemy.gameObject);
            enemy = null;
        }

        if (defeatedBoss)
        {
            player.RemovePoison();
        }

        // Destroyが反映されてから次の敵を生成する
        yield return null;

        SoundManager.instance.PlayBGM("Quest");

        Debug.Log("BattleManager：戦闘終了通知を送信");
        BattleEnded?.Invoke();

        Debug.Log("戦闘終了");
    }
}
