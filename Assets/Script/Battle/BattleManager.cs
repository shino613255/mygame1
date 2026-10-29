using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static EnemyPartsController;

public class BattleManager : MonoBehaviour
{
    private const int BossHeavyChargeTurns = 2;
    private const int SingleTurnCharge = 1;

    [Header("Battle Presentation")]
    [SerializeField] private float battlePauseDuration = 0.5f;
    [SerializeField] private float dialogPauseDuration = 0.5f;
    [SerializeField] private float screenShakeDuration = 0.3f;
    [SerializeField] private float screenShakeStrength = 0.5f;
    [SerializeField] private int screenShakeVibrato = 20;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float playerBaseAccuracy = 1f;
    [SerializeField] private float enemyBaseAccuracy = 1f;
    [SerializeField] private SkillData selectedPlayerSkill;
    [SerializeField] private bool useSelectedSkill = false;

    [Header("UI References")]
    [SerializeField] private GameObject skillSelectionPanel;
    [SerializeField] private GameObject playerStatusPanel;
    [SerializeField] private GameObject skillButton;
    public event System.Action BattleEnded;
    public Transform screenShakeTarget;
    public QuestManager questManager;
    public PlayerUIManager playerUI;
    public EnemyUIManager enemyUI;
    public PlayerManager player;
    public PlayerData playerData;
    private EnemyManager enemy;
    private EnemyAI enemyAI;
    private SkillVfxController skillVfx;

    public SkillVfxController SkillVfx
    {
        get
        {
            if (skillVfx == null)
            {
                skillVfx = GetComponent<SkillVfxController>() ?? gameObject.AddComponent<SkillVfxController>();
            }

            return skillVfx;
        }
    }

    private bool waitingTap;
    private bool isPlayerTurn;
    [SerializeField] private PlayerData defaultMageData;
    private bool isPartViewVisible = false;
    private bool isBattleRunning = false;
    private bool isEndingBattle = false;

    private struct EnemyActionPlan
    {
        public EnemyManager enemy;
        public SkillData skill;
        public bool isReady;
        public bool isInterrupted;
        public bool isTelegraphed;
        public int remainingChargeTurns;
    }

    private EnemyActionPlan enemyActionPlan;
    private bool isShowingEnemyTelegraph;
    private string[] playerActionMessages;
    private int playerInputBlockedThroughFrame = -1;
    public static BattleManager Instance;

    void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        Instance = this;
    }

    public void Setup(EnemyManager enemymanager)
    {
        if (enemymanager == null)
        {
            Debug.LogError("BattleManager.Setup にnullのEnemyManagerが渡されました。");
            return;
        }

        if (isBattleRunning)
        {
            Debug.LogWarning("すでに戦闘中なのでBattleManager.Setupを無視します。");
            return;
        }

        skillButton.SetActive(true);
        playerStatusPanel.SetActive(true);
        isBattleRunning = true;
        isEndingBattle = false;
        SkillVfx.Clear();
        ClearEnemyActionPlan();
        ClearPlayerSkillSelection();
        if (player != null)
        {
            player.RemovePoison();
            player.ResetBattleState();
        }

        isShowingEnemyTelegraph = false;
        isPlayerTurn = false;
        waitingTap = false;
        playerActionMessages = null;
        playerInputBlockedThroughFrame = Time.frameCount;
        enemyAI.ResetBattleState();
        SoundManager.instance.PlayBGM("Battle");
        enemyUI.gameObject.SetActive(true);
        enemy = enemymanager;
        enemy.ClearCounter();
        enemy.ResetStagger();
        enemy.SetPartViewVisible(isPartViewVisible);
        if (enemy.data != null && enemy.data.role == EnemyRole.Tank &&
            (enemy.data.enemyType == EnemyType.Normal || enemy.data.enemyType == EnemyType.Boss) &&
            enemy.GetComponent<SkillCooldowns>() == null)
        {
            enemy.gameObject.AddComponent<SkillCooldowns>();
        }

        if (enemy.data != null && enemy.data.enemyType == EnemyType.Boss && player != null)
        {
            player.RestoreHpAndMp();
        }

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
        if (PlayerSelectionManager.Instance != null)
        {
            playerData = PlayerSelectionManager.Instance.selectedPlayer;
        }

        if (playerData == null)
        {
            playerData = defaultMageData;
        }

        if (playerData != null && player != null)
        {
            player.Setup(playerData);
            SkillPanelPager pager = skillSelectionPanel.GetComponentInChildren<SkillPanelPager>(true);
            if (pager != null)
            {
                pager.Bind(player);
            }
            else
            {
                Debug.LogError("SkillPanelPagerが見つかりません。");
            }
        }
    }

    public void ToggleSkillPanel()
    {
        if (!CanReceivePlayerInput())
        {
            return;
        }

        playerInputBlockedThroughFrame = Time.frameCount;
        skillSelectionPanel.SetActive(!skillSelectionPanel.activeSelf);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            isPartViewVisible = !isPartViewVisible;
            if (enemy != null)
            {
                enemy.SetPartViewVisible(isPartViewVisible);
            }
        }

        if (!CanReceivePlayerInput())
        {
            return;
        }

        if (skillSelectionPanel.activeSelf)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryPickBodyPart(Input.mousePosition);
        }
    }

    public void OnSkillSelected(SkillData selectedSkill)
    {
        if (!CanReceivePlayerInput())
        {
            return;
        }

        if (enemy == null)
        {
            return;
        }

        if (!enemy.IsAlive)
        {
            return;
        }

        if (selectedSkill == null)
        {
            Debug.LogWarning("選択されたスキルがnullですわ！");
            return;
        }

        UnitBase skillTarget = selectedSkill.targetType == TargetType.Self ? (UnitBase)player : enemy;
        if (!SkillUseRules.CanUse(player, skillTarget, selectedSkill, out string useError))
        {
            ShowPlayerChoiceMessage(useError);
            return;
        }

        if (selectedSkill.targetType != TargetType.Self &&
            (!SkillUseRules.IsDirectAttack(selectedSkill) || !selectedSkill.canTargetPart))
        {
            ShowPlayerChoiceMessage("このスキルでは部位を攻撃できない。");
            return;
        }

        playerInputBlockedThroughFrame = Time.frameCount;
        SkillCooldowns cooldowns = player.GetComponent<SkillCooldowns>();
        if (cooldowns != null && !cooldowns.IsReady(selectedSkill))
        {
            ShowPlayerChoiceMessage($"{selectedSkill.skillName}はクールダウン中！");
            return;
        }

        if (selectedSkill.targetType == TargetType.Self)
        {
            var result = SkillExecutor.Execute(player, player, selectedSkill);
            if (!result.executed)
            {
                ShowPlayerChoiceMessage(result.message);
                return;
            }

            player.ResetFocusedAttackChain();
            ClearPlayerSkillSelection();
            PlaySkillEffect(selectedSkill, playerUI.GetSkillEffectPosition(mainCamera));
            playerUI.UpdateUI(player);
            skillSelectionPanel.SetActive(false);
            waitingTap = false;
            isPlayerTurn = false;
            playerActionMessages = new[]
            {
                result.message
            };
            return;
        }

        selectedPlayerSkill = selectedSkill;
        useSelectedSkill = true;
        waitingTap = true;
        skillSelectionPanel.SetActive(false);
        ShowPlayerChoiceMessage($"{selectedSkill.skillName}：部位を選択！");
    }

    private void TryPickBodyPart(Vector2 screenPos)
    {
        if (EventSystem.current != null)
        {
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Exists(result => result.module is UnityEngine.UI.GraphicRaycaster))
            {
                return;
            }
        }

        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (!hit.collider)
        {
            return;
        }

        var part = hit.collider.GetComponentInParent<BodyPart>();
        if (part == null)
        {
            return;
        }

        OnBodyPartTapped(part);
    }

    private int CalculateDamage(SkillData skill)
    {
        if (skill == null)
        {
            return DamageRule.CalcNormalAttack(player.at, enemy.def);
        }

        if (skill.skillType == SkillType.Magic)
        {
            return DamageRule.CalcMagic(player.mag, enemy.mdef, skill.power);
        }

        return DamageRule.CalcPhysical(player.at, enemy.def, skill.power);
    }

    private AttackContext CreateNormalAttackContext()
    {
        int normalDamage = CalculateDamage(null);
        int finalDamage = DamageRule.RollCrit(normalDamage, player.critRate, player.critMultiplier);
        bool isCritical = finalDamage > normalDamage;
        return new AttackContext
        {
            baseDamage = finalDamage,
            mainDamageRate = 1f,
            partDamageRate = 1f,
            focusedPartDamageMultiplier = 1f,
            canApplyStatus = false,
            sourceSkill = null,
            isCritical = isCritical
        };
    }

    private AttackContext CreateSkillAttackContext(SkillData skill)
    {
        int skillDamage = CalculateDamage(skill);
        int finalDamage = skillDamage;
        bool isCritical = false;
        if (skill.canCrit)
        {
            finalDamage = DamageRule.RollCrit(skillDamage, Mathf.Clamp01(player.critRate + skill.critBonus), player.critMultiplier);
            isCritical = finalDamage > skillDamage;
        }

        return new AttackContext
        {
            baseDamage = finalDamage,
            mainDamageRate = skill.mainDamageRate,
            partDamageRate = skill.partDamageRate,
            focusedPartDamageMultiplier = 1f,
            canApplyStatus = skill.statusEffect != null,
            sourceSkill = skill,
            isCritical = isCritical
        };
    }

    public void OnBodyPartTapped(BodyPart part)
    {
        if (!CanAttackPart(part))
        {
            return;
        }

        EnemyPartsController enemyParts = part.GetComponentInParent<EnemyPartsController>();
        if (enemyParts == null || part.GetComponentInParent<EnemyManager>() != enemy)
        {
            Debug.LogWarning($"部位「{part.GetPartNameJP()}」の親にEnemyPartsControllerが見つかりませんでしたわ！");
            return;
        }

        enemyParts.SetSelectedPart(part);
        if (!TryCreateAttackContext(out AttackContext ctx))
        {
            return;
        }

        SkillExecutor.PreparePlayerAttack(player, ref ctx);
        waitingTap = false;
        isPlayerTurn = false;
        if (!RollPlayerAttackHit(ctx))
        {
            HandlePlayerAttackMiss();
            return;
        }

        PlayAttackEffect(ctx, part);
        bool wasBroken = part.IsBroken;
        int hpBefore = enemy.hp;
        int defenseBefore = enemy.def;
        int magicDefenseBefore = enemy.mdef;
        float accuracyPenaltyBefore = enemy.GetAccuracyPenalty();
        var sealedBossSkillsBefore = TankBossRoleAction.GetSealedSkills(enemy);
        ctx.focusedPartDamageMultiplier = player.GetFocusedAttackPartDamageMultiplier(enemy, part);
        var result = enemyParts.ApplyAttack(ctx);
        player.RecordFocusedAttackResult(enemy, part, result.partDamage);
        bool justBroken = !wasBroken && part.IsBroken;
        int lifeDrainRecovery = justBroken ? player.TryRecoverHpFromPartBreak(part) : 0;
        bool techniqueLearned = justBroken && player.BattleEffects.LearnTechnique(player.Role, part);
        bool interrupted = TryInterruptEnemyAction(part, justBroken);
        TryApplyStatusEffect(ctx);
        int reflectedDamage = ApplyCounterDamage(hpBefore);
        bool staggerHit = enemy.TryAddStagger(part, Mathf.Max(0, hpBefore - enemy.hp), result.partDamage);
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
        var messages = new List<string>();
        if (interrupted)
        {
            messages.Add(UsesBossHeavyAttack(enemy)
                ? $"{enemyActionPlan.skill.skillName}を中断した！\n敵はこのターン行動しない。"
                : $"妨害成功！{enemyActionPlan.skill.skillName}を中断！\n敵はこのターン行動しない。");
        }

        messages.Add(CreateAttackMessage(part, ctx, result));
        string breakMessage = CreatePartBreakMessage(
            part,
            wasBroken,
            justBroken,
            Mathf.Max(0, defenseBefore - enemy.def),
            Mathf.Max(0, magicDefenseBefore - enemy.mdef),
            enemy.GetAccuracyPenalty() - accuracyPenaltyBefore);
        if (!string.IsNullOrEmpty(breakMessage))
        {
            messages.Add(breakMessage);
        }

        if (lifeDrainRecovery > 0)
        {
            messages.Add($"\u751F\u547D\u5438\u53CE\uFF01\nHP\u3092{lifeDrainRecovery}\u56DE\u5FA9\u3057\u305F\uFF01");
        }

        if (techniqueLearned)
        {
            messages.Add($"戦技習得！ {part.GetPartNameJP()}の戦技を得た！");
        }

        if (justBroken && enemy.IsAlive)
        {
            foreach (SkillData skill in TankBossRoleAction.GetSealedSkills(enemy))
            {
                if (!sealedBossSkillsBefore.Contains(skill))
                {
                    messages.Add($"{skill.skillName}を封印！\n敵は以後このスキルを使えない。");
                }
            }
        }

        if (staggerHit)
        {
            messages.Add($"弱点ヒット！ 体勢崩し {enemy.StaggerGauge}/{enemy.StaggerGaugeMax}");
            if (enemy.IsStaggered)
            {
                messages.Add("体勢崩し！ 敵の次の行動を封じた！");
            }
        }

        if (reflectedDamage > 0)
        {
            messages.Add($"カウンター！\nプレイヤーは{reflectedDamage}ダメージ受けた！");
        }

        playerActionMessages = messages.ToArray();
    }

    private void HandlePlayerAttackMiss()
    {
        player.ResetFocusedAttackChain();
        if (useSelectedSkill)
        {
            useSelectedSkill = false;
            selectedPlayerSkill = null;
        }

        playerUI.UpdateUI(player);
        playerActionMessages = new[]
        {
            $"{player.name}の攻撃！\nしかし{enemy.name}に当たらなかった！"
        };
    }

    private int ApplyCounterDamage(int hpBefore)
    {
        int reflectedDamage = enemy.GetCounterDamage(Mathf.Max(0, hpBefore - enemy.hp));
        if (reflectedDamage > 0)
        {
            player.TakeDamageRaw(reflectedDamage);
        }

        return reflectedDamage;
    }

    private bool CanAttackPart(BodyPart part)
    {
        if (part == null)
        {
            return false;
        }

        if (!CanReceivePlayerInput())
        {
            return false;
        }

        if (enemy == null)
        {
            return false;
        }

        if (!enemy.IsAlive)
        {
            return false;
        }

        return true;
    }

    private bool TryCreateAttackContext(out AttackContext ctx)
    {
        if (useSelectedSkill && selectedPlayerSkill != null)
        {
            if (!SkillUseRules.TryPay(player, enemy, selectedPlayerSkill, out string useError))
            {
                ShowPlayerChoiceMessage(useError);
                ctx = default;
                return false;
            }

            ctx = CreateSkillAttackContext(selectedPlayerSkill);
            player.GetComponent<SkillCooldowns>()?.StartCooldown(selectedPlayerSkill);
            return true;
        }

        ctx = CreateNormalAttackContext();
        return true;
    }

    private void PlayAttackEffect(AttackContext ctx, BodyPart part)
    {
        if (ctx.sourceSkill == null)
        {
            return;
        }

        Collider2D collider = part.GetComponent<Collider2D>();
        PlaySkillEffect(ctx.sourceSkill, collider != null ? collider.bounds.center : part.transform.position);
    }

    private bool RollPlayerAttackHit(AttackContext ctx)
    {
        float accuracy = ctx.sourceSkill != null ? ctx.sourceSkill.accuracy : playerBaseAccuracy;
        return DamageRule.RollHit(accuracy, enemy.evasionRate, ctx.accuracyBonus);
    }

    private void TryApplyStatusEffect(AttackContext ctx)
    {
        if (ctx.sourceSkill == null)
        {
            return;
        }

        StatusEffectData effect = ctx.sourceSkill.statusEffect;
        if (effect == null)
        {
            return;
        }

        if (enemy == null || !enemy.IsAlive)
        {
            return;
        }

        float chance = ctx.sourceSkill.applyChance;
        if (Random.value > chance)
        {
            return;
        }

        switch (effect.type)
        {
            case StatusEffectType.Burn:
                enemy.ApplyBurn(effect);
                break;
            case StatusEffectType.Frozen:
                enemy.ApplyFrozen(effect);
                break;
        }
    }

    private string CreateAttackMessage(BodyPart part, AttackContext ctx, AttackResult result)
    {
        string message;
        if (ctx.isCritical)
        {
            message = "クリティカル！\n";
        }
        else
        {
            message = string.Empty;
        }

        if (ctx.sourceSkill != null)
        {
            message += $"{ctx.sourceSkill.skillName}で{part.GetPartNameJP()}に攻撃！";
        }
        else
        {
            message += $"{part.GetPartNameJP()}を攻撃！";
        }

        if (result.damageMultiplier > 1f)
        {
            message += "\n弱点！";
        }
        else if (result.damageMultiplier > 0f && result.damageMultiplier < 1f)
        {
            message += "\n耐性あり";
        }

        message += $"\nダメージ：本体{result.mainDamage}／部位{result.partDamage}";
        return message;
    }

    private string CreatePartBreakMessage(
        BodyPart part,
        bool wasBroken,
        bool justBroken,
        int defenseDown,
        int magicDefenseDown,
        float accuracyDown)
    {
        if (justBroken)
        {
            switch (part.partType)
            {
                case PartType.Face:
                    return $"顔を破壊！\n防御-{defenseDown}／魔法防御-{magicDefenseDown}";
                case PartType.Hand:
                    return $"手を破壊！\n通常攻撃の命中率-{Mathf.RoundToInt(accuracyDown * 100f)}ポイント";
                case PartType.Leg:
                    return "脚を破壊！\nこのターンからプレイヤーは2回行動！";
            }
        }

        if (wasBroken && enemy.UsesStagger)
        {
            return "この部位は破壊済み。\n部位破壊効果は再発生しない。";
        }

        return wasBroken ? "この部位は破壊済み。\n再攻撃では妨害できない。" : string.Empty;
    }

    public void PlaySkillEffect(SkillData skill, Vector3 worldPos)
    {
        SkillVfx.Play(skill, worldPos);
    }

    private bool CanReceivePlayerInput()
    {
        var dialog = DialogTextManager.instance;
        return isBattleRunning && !isEndingBattle && isPlayerTurn && !isShowingEnemyTelegraph &&
            Time.frameCount > playerInputBlockedThroughFrame &&
            (dialog == null || (dialog.IsCompleteDisplayText && !dialog.HasPendingLines && !dialog.ConsumedClickThisFrame));
    }

    private string GetEnemyTelegraphMessage()
    {
        if (!HasPlannedTelegraph())
        {
            return string.Empty;
        }

        string skillName = enemyActionPlan.skill.skillName;
        string announcementName = UsesBossHeavyAttack(enemy) ? skillName + "！" : skillName;
        if (enemyActionPlan.isInterrupted)
        {
            return $"{skillName}は中断済み！\n敵はこのターン行動しない。";
        }

        string announcement = UsesBossHeavyAttack(enemy) && enemyActionPlan.remainingChargeTurns > 1
            ? $"{enemy.name}は{skillName}を構えている！\nあと{enemyActionPlan.remainingChargeTurns - 1}回構えてから発動！"
            : $"次の敵行動：{announcementName}";
        bool hasHands = false;
        foreach (BodyPart part in enemy.GetComponentsInChildren<BodyPart>(true))
        {
            if (part.partType != PartType.Hand || !part.canBreak)
            {
                continue;
            }

            hasHands = true;
            if (!part.IsBroken)
            {
                return $"{announcement}\n壊れていない手を破壊すると中断！";
            }
        }

        return $"{announcement}\n" + (hasHands ? "両手は破壊済み。今回は妨害不可。" : "破壊できる手がなく、今回は妨害不可。");
    }

    private void ShowPlayerChoiceMessage(string message)
    {
        DialogTextManager.instance.SetScenarios(new[] { message });
        playerInputBlockedThroughFrame = Time.frameCount;
    }

    private bool UsesEnemyActionPlan(EnemyManager target)
    {
        return target != null && target.data != null && target.data.enemyType == EnemyType.Normal &&
            target.data.role == EnemyRole.Attacker && target.data.telegraphedSkill != null;
    }

    private void PrepareEnemyAction()
    {
        if (enemyActionPlan.isReady && enemyActionPlan.enemy == enemy && enemyActionPlan.remainingChargeTurns > 0)
        {
            enemyActionPlan.isTelegraphed = false;
            return;
        }

        ClearEnemyActionPlan();
        if (UsesBossHeavyAttack(enemy))
        {
            SkillData heavyAttack = enemyAI.TakePendingBossHeavyAttack(enemy);
            if (heavyAttack != null)
            {
                StartEnemyCharge(heavyAttack, BossHeavyChargeTurns);
            }

            return;
        }

        if (!UsesEnemyActionPlan(enemy))
        {
            return;
        }

        enemyActionPlan = new EnemyActionPlan
        {
            enemy = enemy,
            skill = enemyAI.ChooseSkill(enemy, 1),
            isReady = true
        };
    }

    private bool HasPlannedTelegraph()
    {
        return enemyActionPlan.isReady && enemyActionPlan.enemy == enemy && enemy != null && enemy.data != null &&
            enemyActionPlan.skill != null && enemyActionPlan.skill == enemy.data.telegraphedSkill;
    }

    private bool TryInterruptEnemyAction(BodyPart part, bool justBroken)
    {
        if (!justBroken || part == null || part.partType != PartType.Hand ||
            !(UsesEnemyActionPlan(enemy) || (UsesChargedAttack(enemy) && enemyActionPlan.isTelegraphed)) ||
            !HasPlannedTelegraph() || !enemy.IsAlive || enemyActionPlan.isInterrupted ||
            part.GetComponentInParent<EnemyManager>() != enemy)
        {
            return false;
        }

        enemyActionPlan.isInterrupted = true;
        return true;
    }

    private bool TryConsumeEnemyAction(out SkillData skill, out bool isInterrupted)
    {
        skill = null;
        isInterrupted = false;
        if (!enemyActionPlan.isReady || enemyActionPlan.enemy != enemy)
        {
            return false;
        }

        skill = enemyActionPlan.skill;
        isInterrupted = enemyActionPlan.isInterrupted;
        ClearEnemyActionPlan();
        return true;
    }

    private void ClearEnemyActionPlan()
    {
        enemyActionPlan = default;
    }

    private bool UsesBossHeavyAttack(EnemyManager target)
    {
        return target != null && target.data != null && target.data.enemyType == EnemyType.Boss &&
            target.data.role == EnemyRole.Tank && target.data.telegraphedSkill != null &&
            target.data.telegraphedSkill.skillType == SkillType.Physical;
    }

    private bool UsesEliteCharge(EnemyManager target)
    {
        return target != null && target.data != null && target.data.enemyType == EnemyType.Reinforced &&
            target.data.role == EnemyRole.Attacker && target.data.telegraphedSkill != null;
    }

    private bool UsesChargedAttack(EnemyManager target)
    {
        return UsesBossHeavyAttack(target) || UsesEliteCharge(target) || ExpansionEnemyAction.UsesCharge(target);
    }

    private void StartEnemyCharge(SkillData skill, int playerTurns)
    {
        enemyActionPlan = new EnemyActionPlan
        {
            enemy = enemy,
            skill = skill,
            isReady = true,
            remainingChargeTurns = playerTurns
        };
    }

    private void ClearPlayerSkillSelection()
    {
        selectedPlayerSkill = null;
        useSelectedSkill = false;
    }

    private void OnDisable()
    {
        if (skillVfx != null)
        {
            skillVfx.Clear();
        }

        if (enemy != null)
        {
            enemy.ClearCounter();
            enemy.ResetStagger();
        }

        ClearEnemyActionPlan();
        ClearPlayerSkillSelection();
        enemyAI?.ResetBattleState();
        if (player != null)
        {
            player.RemovePoison();
            player.ResetBattleState();
        }

        isShowingEnemyTelegraph = false;
        isPlayerTurn = false;
        waitingTap = false;
        isBattleRunning = false;
        playerActionMessages = null;
        StopAllCoroutines();
    }

    IEnumerator BattleLoop()
    {
        EnemyManager battleEnemy = enemy;
        while (player != null && enemy != null && player.IsAlive && enemy.IsAlive)
        {
            player.BattleEffects.BeginPlayerTurn();
            int playerActionCount = enemy.GetPlayerActionCount();
            SkillCooldowns playerCooldowns = player.GetComponent<SkillCooldowns>();
            if (playerCooldowns != null)
            {
                playerCooldowns.Tick();
            }

            PrepareEnemyAction();
            for (int i = 0; i < playerActionCount; i++)
            {
                yield return StartCoroutine(PlayerActByTap(i == 0));
                if (enemy != battleEnemy)
                {
                    yield break;
                }

                if (player == null || enemy == null || !player.IsAlive || !enemy.IsAlive)
                {
                    ClearEnemyActionPlan();
                    yield break;
                }

                // 最初の行動で脚を破壊した場合は、このターンから追加行動を許可する。
                playerActionCount = enemy.GetPlayerActionCount();
            }

            player.BattleEffects.EndPlayerTurn();
            enemy.BattleEffects.EndPlayerTurn();
            enemy.ClearCounter();
            yield return new WaitForSeconds(battlePauseDuration);
            yield return StartCoroutine(EnemyActAuto());
            if (enemy != battleEnemy)
            {
                yield break;
            }

            if (player == null || !player.IsAlive)
            {
                ClearEnemyActionPlan();
                ClearPlayerSkillSelection();
                enemyAI?.ResetBattleState();
                if (player != null)
                {
                    player.RemovePoison();
                }

                if (enemy != null)
                {
                    enemy.ClearCounter();
                    enemy.ResetStagger();
                }

                questManager.QuestFailed();
                yield break;
            }

            if (enemy == null || !enemy.IsAlive)
            {
                ClearEnemyActionPlan();
                yield break;
            }

            yield return new WaitForSeconds(battlePauseDuration);
        }

        ClearEnemyActionPlan();
    }

    IEnumerator PlayerActByTap(bool isFirstActionOfTurn = false)
    {
        EnemyManager battleEnemy = enemy;
        isPlayerTurn = false;
        waitingTap = true;
        playerActionMessages = null;
        int poisonDamage = isFirstActionOfTurn ? player.TickPoisonDamage() : 0;
        if (poisonDamage > 0)
        {
            playerUI.UpdateUI(player);
            var poisonMessages = new List<string>
            {
                $"毒ダメージ！\n{poisonDamage}ダメージ受けた！"
            };
            if (player.IsAlive && !player.IsPoisoned)
            {
                poisonMessages.Add("毒が自然に解除された！");
            }

            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait(poisonMessages.ToArray(), dialogPauseDuration));
            if (player == null || !player.IsAlive)
            {
                waitingTap = false;
                isPlayerTurn = false;
                ClearEnemyActionPlan();
                ClearPlayerSkillSelection();
                enemyAI?.ResetBattleState();
                if (player != null)
                {
                    player.RemovePoison();
                }

                if (enemy != null)
                {
                    enemy.ClearCounter();
                    enemy.ResetStagger();
                }

                questManager.QuestFailed();
                yield break;
            }
        }

        if (!isBattleRunning || isEndingBattle || enemy != battleEnemy)
        {
            yield break;
        }

        if (isFirstActionOfTurn && HasPlannedTelegraph())
        {
            isShowingEnemyTelegraph = true;
            skillSelectionPanel.SetActive(false);
            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait(GetEnemyTelegraphMessage(), dialogPauseDuration));
            yield return null;
            if (enemy != battleEnemy)
            {
                yield break;
            }

            isShowingEnemyTelegraph = false;
            if (!isBattleRunning || isEndingBattle || player == null || !player.IsAlive || enemy == null || !enemy.IsAlive)
            {
                ClearEnemyActionPlan();
                yield break;
            }

            enemyActionPlan.isTelegraphed = true;
        }
        else
        {
            ShowPlayerChoiceMessage("攻撃する部位を選択！");
        }

        isPlayerTurn = true;
        while (waitingTap)
        {
            yield return null;
        }

        isPlayerTurn = false;
        if (!isBattleRunning || isEndingBattle || enemy != battleEnemy)
        {
            yield break;
        }

        string[] messages = playerActionMessages;
        playerActionMessages = null;
        if (messages != null)
        {
            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait(messages, dialogPauseDuration));
        }

        if (!isBattleRunning || isEndingBattle || enemy != battleEnemy)
        {
            yield break;
        }

        if (player == null || !player.IsAlive)
        {
            ClearEnemyActionPlan();
            ClearPlayerSkillSelection();
            enemyAI?.ResetBattleState();
            if (enemy != null)
            {
                enemy.ClearCounter();
                enemy.ResetStagger();
            }

            if (player != null)
            {
                player.RemovePoison();
            }

            questManager.QuestFailed();
            yield break;
        }

        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);
        }

        if (enemy != null && !enemy.IsAlive)
        {
            yield return StartCoroutine(EndBattle());
        }
    }

    IEnumerator EnemyActAuto()
    {
        yield return StartCoroutine(EnemyActCore());
        if (player != null)
        {
            player.BattleEffects.EndEnemyAction();
        }
    }

    IEnumerator EnemyActCore()
    {
        if (enemy == null || player == null)
        {
            ClearEnemyActionPlan();
            yield break;
        }

        EnemyManager actingEnemy = enemy;
        if (!enemy.IsAlive)
        {
            yield return StartCoroutine(EndBattle());
            yield break;
        }

        enemy.TickDefenseBuff();
        enemy.TickMagicDefenseBuff();
        enemy.TickFrozen();
        SkillCooldowns cooldowns = enemy.GetComponent<SkillCooldowns>();
        if (cooldowns != null)
        {
            cooldowns.Tick();
        }

        enemyUI.UpdateUI(enemy);
        int burnDmg = enemy.TickBurnDamage();
        if (burnDmg > 0)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                yield return StartCoroutine(EndBattle());
                yield break;
            }

            enemyUI.UpdateUI(enemy);
            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait($"火傷ダメージ！\n敵は{burnDmg}ダメージ受けた", dialogPauseDuration));
        }

        yield return new WaitForSeconds(battlePauseDuration);
        if (enemy == null || enemy != actingEnemy || player == null || !player.IsAlive || !isBattleRunning || isEndingBattle)
        {
            yield break;
        }

        if (!enemy.IsAlive)
        {
            yield return StartCoroutine(EndBattle());
            yield break;
        }

        if (enemy.ConsumeStaggerAtEnemyTurn())
        {
            ClearEnemyActionPlan();
            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait(
                $"{enemy.name}は体勢を崩して動けない！\n次のプレイヤーターンは体勢崩しの蓄積を休止する。",
                dialogPauseDuration));
            yield break;
        }

        bool isChargedAction = UsesChargedAttack(enemy) && enemyActionPlan.isReady && enemyActionPlan.enemy == enemy &&
            enemyActionPlan.remainingChargeTurns > 0 && enemyActionPlan.skill == enemy.data.telegraphedSkill;
        bool isBossHeavyAction = isChargedAction && UsesBossHeavyAttack(enemy);
        if (isChargedAction && !enemyActionPlan.isTelegraphed)
        {
            ClearEnemyActionPlan();
            Debug.LogWarning("予告されていない構え攻撃は実行しません。", this);
            yield break;
        }

        if (isChargedAction && !enemyActionPlan.isInterrupted &&
            !(isBossHeavyAction && TankBossRoleAction.GetSealedSkills(enemy).Contains(enemyActionPlan.skill)) &&
            enemyActionPlan.remainingChargeTurns > 1)
        {
            enemyActionPlan.remainingChargeTurns--;
            enemyActionPlan.isTelegraphed = false;
            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait($"{enemy.name}は強力な一撃の構えを続けている！", dialogPauseDuration));
            yield break;
        }

        SkillData skill;
        if (!TryConsumeEnemyAction(out skill, out bool isInterrupted))
        {
            if (UsesEnemyActionPlan(enemy))
            {
                Debug.LogWarning("確定済みの敵行動がないため、敵の行動を終了します。", this);
                yield break;
            }

            skill = enemyAI.ChooseSkill(enemy, 0, player, UsesBossHeavyAttack(enemy));
            if ((UsesEliteCharge(enemy) || ExpansionEnemyAction.UsesCharge(enemy)) && skill != null &&
                skill == enemy.data.telegraphedSkill)
            {
                StartEnemyCharge(skill, SingleTurnCharge);
            }

            if ((UsesBossHeavyAttack(enemy) && enemyAI.PendingBossHeavyAttack != null) ||
                ((UsesEliteCharge(enemy) || ExpansionEnemyAction.UsesCharge(enemy)) && enemyActionPlan.isReady))
            {
                yield return StartCoroutine(DialogTextManager.instance.ShowAndWait($"{enemy.name}は強力な一撃を構えた！", dialogPauseDuration));
                yield break;
            }
        }

        if (isBossHeavyAction && TankBossRoleAction.GetSealedSkills(enemy).Contains(skill))
        {
            isInterrupted = true;
        }

        if (ExpansionEnemyAction.IsSealed(enemy, skill))
        {
            isInterrupted = true;
        }

        if (isInterrupted)
        {
            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait(
                $"{skill.skillName}は中断された！\n{enemy.name}は行動できない！",
                dialogPauseDuration));
            yield break;
        }

        if (skill == null)
        {
            float physicalMultiplier = enemy.BattleEffects.ConsumePhysicalMultiplier();
            float finalAcc = enemyBaseAccuracy - enemy.GetAccuracyPenalty();
            finalAcc = Mathf.Clamp01(finalAcc);
            bool hit = DamageRule.RollHit(finalAcc, 0f);
            if (!hit)
            {
                yield return StartCoroutine(DialogTextManager.instance.ShowAndWait("敵の攻撃！\nしかし外れた！", dialogPauseDuration));
                yield break;
            }

            SoundManager.instance.PlayButtonSE(1);
            ShakeScreen();
            int normalDamage = DamageRule.CalcNormalAttack(enemy.at, player.def);
            normalDamage = Mathf.RoundToInt(normalDamage * physicalMultiplier);
            int dmg = player.TakeDirectDamage(normalDamage);
            playerUI.UpdateUI(player);
            yield return StartCoroutine(DialogTextManager.instance.ShowAndWait($"敵の攻撃！\nプレイヤーは{dmg}ダメージ受けた", dialogPauseDuration));
            yield break;
        }

        UnitBase target;
        if (skill.targetType == TargetType.Self)
        {
            target = enemy;
        }
        else
        {
            target = player;
        }

        var result = SkillExecutor.Execute(enemy, target, skill, 0f);
        if (result.executed && result.hit)
        {
            PlaySkillEffect(
                skill,
                target == enemy ? enemy.transform.position : playerUI.GetSkillEffectPosition(mainCamera));
        }

        enemyUI.UpdateUI(enemy);
        if (result.hit && skill.targetType != TargetType.Self)
        {
            SoundManager.instance.PlayButtonSE(1);
            ShakeScreen();
        }

        playerUI.UpdateUI(player);
        yield return StartCoroutine(DialogTextManager.instance.ShowAndWait(result.message, dialogPauseDuration));
    }

    private void ShakeScreen()
    {
        screenShakeTarget.DOShakePosition(
            screenShakeDuration,
            screenShakeStrength,
            screenShakeVibrato,
            0,
            false,
            true);
    }

    IEnumerator EndBattle()
    {
        if (isEndingBattle)
        {
            yield break;
        }

        isEndingBattle = true;
        SkillVfx.Clear();
        ClearEnemyActionPlan();
        ClearPlayerSkillSelection();
        enemyAI?.ResetBattleState();
        isShowingEnemyTelegraph = false;
        playerActionMessages = null;
        string deleteEnemyName = enemy != null ? enemy.name : "敵";
        isBattleRunning = false;
        isPlayerTurn = false;
        waitingTap = false;
        skillSelectionPanel.SetActive(false);
        playerStatusPanel.SetActive(false);
        DialogTextManager.instance.SetScenarios(new string[] { $"{deleteEnemyName}を倒した" });
        enemyUI.gameObject.SetActive(false);
        if (enemy != null)
        {
            enemy.ClearCounter();
            enemy.ResetStagger();
            Destroy(enemy.gameObject);
            enemy = null;
        }

        if (player != null)
        {
            player.RemovePoison();
            player.ResetBattleState();
            playerUI.UpdateUI(player);
        }

        // Destroyが反映されてから次の敵を生成する
        yield return null;
        SoundManager.instance.PlayBGM("Quest");
        BattleEnded?.Invoke();
        skillButton.SetActive(false);
    }

    public void DebugKillEnemy()
    {
        if (enemy == null)
        {
            return;
        }

        if (!isBattleRunning)
        {
            return;
        }

        enemy.TakeDamageRaw(enemy.hp);
        waitingTap = false;
        isPlayerTurn = false;
        StartCoroutine(EndBattle());
    }
}
