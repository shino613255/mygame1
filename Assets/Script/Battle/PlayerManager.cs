using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerManager : UnitBase
{ 
    [Header("Skill Panel")]
    [SerializeField] private SkillSlotUI[] skillSlots;
    [SerializeField] private UnityEngine.UI.Image portraitView;
    [SerializeField] private SkillPanelPager skillPager;

    public PlayerData Data { get; private set; }
    public PlayerRole Role => Data != null ? Data.role : PlayerRole.Mage;
    public PlayerBattleState BattleEffects { get; } = new PlayerBattleState();
    public IReadOnlyList<SkillData> Skills => currentSkills;
    public bool HasSkill(SkillData skill) => skill != null && currentSkills.Contains(skill);

    private List<SkillData> currentSkills = new List<SkillData>();                                      
    private SkillData currentSkill;                                                                         
    private bool acted;                                                                                     
    private EnemyManager targetEnemy;                                                                       
    public bool useSkill = false;

    [Header("Poison")]
    private bool isPoisoned = false;
    private StatusEffectData poisonEffect;
    private int remainingPoisonTurns;
    public bool IsPoisoned => isPoisoned;

    [Header("Growth Rewards")]
    [SerializeField, Range(0f, 1f)] private float lifeDrainHealRate = 0.05f;
    [SerializeField, Min(1f)] private float focusedAttackSecondHitMultiplier = 1.2f;
    [SerializeField, Min(1f)] private float focusedAttackThirdHitMultiplier = 1.4f;
    [SerializeField, Range(0f, 1f)] private float precisionShotAccuracyBonus = 0.1f;
    private readonly HashSet<GrowthRewardId> acquiredGrowthRewards = new HashSet<GrowthRewardId>();
    private readonly HashSet<BodyPart> lifeDrainTriggeredParts = new HashSet<BodyPart>();
    private EnemyManager focusedAttackEnemy;
    private BodyPart focusedAttackPart;
    private int focusedAttackHitCount;

    public bool HasLifeDrainReward => HasGrowthReward(GrowthRewardId.LifeDrain);
    public bool HasFocusedAttackReward => HasGrowthReward(GrowthRewardId.FocusedAttack);
    public bool HasPrecisionShotReward => HasGrowthReward(GrowthRewardId.PrecisionShot);

    public void Setup(PlayerData data)
    {
        ResetGrowthRewards();
        ResetBattleState();
        var cooldowns = GetComponent<SkillCooldowns>();
        if (cooldowns == null) cooldowns = gameObject.AddComponent<SkillCooldowns>();
        cooldowns.Clear();

        if (data == null) return;                                                                          

        Data = data;
        RemovePoison();
        if (portraitView != null)
        {
            portraitView.sprite = data.portrait;
            portraitView.enabled = data.portrait != null;
        }

        name = data.playerName;                                                                            

        this.maxHp = data.startMaxHp;
        this.hp = data.startMaxHp;
        this.maxMp = data.startMaxMp;
        this.mp = data.startMaxMp;
        this.at = data.startAt;
        this.mag = data.startMag;
        this.def = data.startDef;
        this.mdef = data.startMdef;


        Debug.Log($"[完了] {data.playerName}のステータスを同期しましたわ！ (AT:{this.at})");

        currentSkills.Clear();                                                                              

        if (data.startSkills != null)                                                                         
        {
            currentSkills.AddRange(data.startSkills);                                               
        }

        UpdateSkillPanel();                                                                         
        Debug.Log("初期スキル数: " + currentSkills.Count);
    }

    public void RestoreHpAndMp()
    {
        hp = maxHp;
        mp = maxMp;
    }

    public bool ApplyPoison(
    StatusEffectData effect
)
    {
        if (effect == null)
        {
            Debug.LogWarning(
                "PoisonのStatusEffectDataがnullです"
            );

            return false;
        }

        if (effect.type != StatusEffectType.Poison)
        {
            return false;
        }

        if (isPoisoned)
            return false;

        isPoisoned = true;
        poisonEffect = effect;
        remainingPoisonTurns = Mathf.Max(1, effect.durationTurns);

        Debug.Log(
            $"プレイヤーが毒状態になった！ " +
            $"ダメージ率:{effect.tickHpRate * 100f}%"
        );

        return true;
    }


    public int TickPoisonDamage()
    {
        if (!isPoisoned)
            return 0;

        if (poisonEffect == null)
            return 0;

        int damage =
            poisonEffect.CalcTickDamage(maxHp);

        hp = Mathf.Clamp(
            hp - damage,
            0,
            maxHp
        );

        Debug.Log(
            $"毒ダメージ:{damage} HP:{hp}/{maxHp}"
        );

        remainingPoisonTurns--;
        if (remainingPoisonTurns <= 0)
            RemovePoison();

        return damage;
    }

    public void RemovePoison()
    {
        isPoisoned = false;
        poisonEffect = null;
        remainingPoisonTurns = 0;

        Debug.Log("毒状態が解除された");
    }

    public void SelectSkill(SkillData skill)
    {
        if (skill == null) return;                                                                                  

        currentSkill = skill;                                                                                  
        useSkill = true;

        Debug.Log("スキル「" + skill.skillName + "」を選択しました");
    }

    public void AddSkill(SkillData skill)
    {
        if (skill == null || currentSkills.Contains(skill) || !SkillUseRules.IsAllowedForRole(skill, Role)) return;

        currentSkills.Add(skill);                                                                   
        UpdateSkillPanel();                                                                                     

        Debug.Log("スキル「" + skill.skillName + "」を習得しました");
    }

    public void EnhanceMagicAttackRate(float rate)
    {
        mag = Mathf.RoundToInt(mag * rate);
    }

    public void EnhanceDefenseRate(float rate)
    {
        def = Mathf.RoundToInt(def * rate);
    }

    public void EnhanceAttackRate(float rate) => at = Mathf.RoundToInt(at * rate);

    public void EnhanceMaxMpRate(float rate)
    {
        int before = maxMp;
        maxMp = Mathf.RoundToInt(maxMp * rate);
        mp += maxMp - before;
    }

    public void ResetBattleState()
    {
        BattleEffects.Reset();
        ResetFocusedAttackChain();
    }

    public int GetSkillMpCost(SkillData skill) => BattleEffects.GetMpCost(skill != null ? skill.mpCost : 0);

    public int TakeDirectDamage(int damage, bool isMagic = false)
    {
        return base.TakeDamageRaw(BattleEffects.ReduceDirectDamage(damage), isMagic);
    }

    public override int TakePhysical(int attackerAtk)
    {
        return TakeDirectDamage(DamageRule.CalcNormalAttack(attackerAtk, def));
    }

    public void EnhanceMaxHpRate(float rate)
    {
        int oldMaxHp = maxHp;

        maxHp = Mathf.RoundToInt(maxHp * rate);

        int increase = maxHp - oldMaxHp;
        hp += increase;
    }

    public bool TryAcquireLifeDrainReward()
    {
        return TryAcquireGrowthReward(GrowthRewardId.LifeDrain);
    }

    public bool HasGrowthReward(GrowthRewardId rewardId)
    {
        return acquiredGrowthRewards.Contains(rewardId);
    }

    public bool TryAcquireGrowthReward(GrowthRewardId rewardId)
    {
        return acquiredGrowthRewards.Add(rewardId);
    }

    public float GetPrecisionShotAccuracyBonus()
    {
        return HasPrecisionShotReward
            ? precisionShotAccuracyBonus
            : 0f;
    }

    public float GetFocusedAttackPartDamageMultiplier(EnemyManager enemy, BodyPart part)
    {
        if (!HasFocusedAttackReward || enemy == null || part == null || part.IsBroken)
            return 1f;

        if (focusedAttackEnemy != enemy || focusedAttackPart != part || focusedAttackHitCount <= 0)
            return 1f;

        return focusedAttackHitCount == 1
            ? focusedAttackSecondHitMultiplier
            : focusedAttackThirdHitMultiplier;
    }

    public void RecordFocusedAttackResult(EnemyManager enemy, BodyPart part, int actualPartDamage)
    {
        if (!HasFocusedAttackReward || enemy == null || part == null ||
            actualPartDamage <= 0 || part.IsBroken)
        {
            ResetFocusedAttackChain();
            return;
        }

        if (focusedAttackEnemy == enemy && focusedAttackPart == part)
        {
            focusedAttackHitCount++;
            return;
        }

        focusedAttackEnemy = enemy;
        focusedAttackPart = part;
        focusedAttackHitCount = 1;
    }

    public void ResetFocusedAttackChain()
    {
        focusedAttackEnemy = null;
        focusedAttackPart = null;
        focusedAttackHitCount = 0;
    }

    public int TryRecoverHpFromPartBreak(BodyPart part)
    {
        if (!HasLifeDrainReward || part == null || !part.canBreak || !part.IsBroken)
            return 0;

        if (!lifeDrainTriggeredParts.Add(part))
            return 0;

        int healAmount = Mathf.Max(1, Mathf.RoundToInt(maxHp * lifeDrainHealRate));
        int hpBefore = hp;
        hp = Mathf.Min(maxHp, hp + healAmount);

        return hp - hpBefore;
    }

    private void ResetGrowthRewards()
    {
        acquiredGrowthRewards.Clear();
        lifeDrainTriggeredParts.Clear();
        ResetFocusedAttackChain();
    }

    private void UpdateSkillPanel()                                                                           
    {
        if (skillPager != null)
        {
            skillPager.Bind(this);
            return;
        }
        if (skillSlots == null) return;
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (i < currentSkills.Count)
            {
                skillSlots[i].SetSkill(currentSkills[i], this);                                    
            }
            else
            {
                skillSlots[i].SetSkill(null, this);
            }
        }
    }

    public override IEnumerator Act()                                                                           
    {  
        Debug.Log("プレイヤーの行動（入力待ち）");

        acted = false;

        // 現在は1対1戦闘前提のため、シーン内のEnemyManagerを1体取得
        targetEnemy = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();                                   
        if (targetEnemy == null)                                                                                                            
        {
            acted = true;
            yield break;
        }

        // プレイヤーがスキルや攻撃対象を選択して行動を確定するまで待機
        while (!acted)                                                                                         
        {
            yield return null;                                                                               
        }

        yield break;
    }
    void Update()
    {
           
    }
}
