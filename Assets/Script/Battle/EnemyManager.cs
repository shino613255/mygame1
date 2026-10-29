using DG.Tweening;
using System.Collections;
using UnityEngine;

public class EnemyManager : UnitBase
{
    private const float HandBreakAccuracyPenalty = 0.1f;
    private const int PlayerActionsAfterLegBreak = 2;
    private const int FaceBreakDefenseReduction = 20;
    private const float CounterDamageMultiplier = 1.7f;
    private const float NormalBurnRate = 0.05f;
    private const float ReinforcedBurnRate = 0.03f;
    private const float BossBurnRate = 0.02f;

    [Header("Damage Feedback")]
    [SerializeField] private float damageShakeDuration = 0.3f;
    [SerializeField] private float damageShakeStrength = 0.5f;
    [SerializeField] private int damageShakeVibrato = 20;

    public EnemyBattleState BattleEffects { get; } = new EnemyBattleState();

    private float accuracyPenalty = 0f;
    private bool isLegBroken = false;

    public void ApplyAccuracyDown(float value)
    {
        accuracyPenalty += value;
    }

    public float GetAccuracyPenalty()
    {
        return accuracyPenalty;
    }

    public int GetPlayerActionCount()
    {
        return isLegBroken ? PlayerActionsAfterLegBreak : 1;
    }

    public void OnPartBroken(PartType part)
    {
        switch (part)
        {
            case PartType.Hand:
                ApplyAccuracyDown(HandBreakAccuracyPenalty);
                break;
            case PartType.Leg:
                isLegBroken = true;
                break;
            case PartType.Face:
                def -= FaceBreakDefenseReduction;
                mdef -= FaceBreakDefenseReduction;
                def = Mathf.Max(0, def);
                mdef = Mathf.Max(0, mdef);
                break;
        }
    }

    public EnemyData data;
    private int staggerGauge;
    private bool isStaggered;
    private int remainingStaggerRecoveryTurns;

    public bool UsesStagger => data != null && data.enableStagger && data.enemyType == EnemyType.Normal;

    public int StaggerGauge => staggerGauge;

    public int StaggerGaugeMax => data != null ? Mathf.Max(1, data.staggerGaugeMax) : 0;

    public bool IsStaggered => isStaggered;

    public bool TryAddStagger(BodyPart part, int actualHpDamage, int actualPartDamage)
    {
        if (!UsesStagger || !IsAlive || part == null || part.GetComponentInParent<EnemyManager>() != this ||
            part.partType != data.staggerWeakPoint || (actualHpDamage <= 0 && actualPartDamage <= 0) ||
            isStaggered || remainingStaggerRecoveryTurns > 0)
        {
            return false;
        }

        staggerGauge += Mathf.Min(Mathf.Max(1, data.staggerGainPerHit), StaggerGaugeMax - staggerGauge);
        isStaggered = staggerGauge >= StaggerGaugeMax;
        return true;
    }

    public bool ConsumeStaggerAtEnemyTurn()
    {
        if (!UsesStagger || !IsAlive)
        {
            return false;
        }

        if (!isStaggered)
        {
            if (remainingStaggerRecoveryTurns > 0)
            {
                remainingStaggerRecoveryTurns--;
            }

            return false;
        }

        isStaggered = false;
        staggerGauge = 0;
        remainingStaggerRecoveryTurns = Mathf.Max(1, data.staggerRecoveryTurns);
        return true;
    }

    public void ResetStagger()
    {
        staggerGauge = 0;
        isStaggered = false;
        remainingStaggerRecoveryTurns = 0;
    }

    private bool isCounterActive;

    public bool IsCounterActive => isCounterActive;

    public bool CanActivateCounter => IsAlive && !isCounterActive && data != null && data.role == EnemyRole.Tank &&
        (data.enemyType == EnemyType.Normal || data.enemyType == EnemyType.Boss);

    public void ActivateCounter()
    {
        if (CanActivateCounter)
        {
            isCounterActive = true;
        }
    }

    public int GetCounterDamage(int actualHpDamage)
    {
        return isCounterActive && IsAlive && actualHpDamage > 0
            ? Mathf.Max(0, Mathf.RoundToInt(actualHpDamage * CounterDamageMultiplier))
            : 0;
    }

    public void ClearCounter()
    {
        isCounterActive = false;
    }

    [Header("VFX")]
    public GameObject damageEffect;

    [Header("状態異常")]
    private StatusEffectType currentStatusEffect = StatusEffectType.None;

    public StatusEffectType CurrentStatusEffect => currentStatusEffect;

    public bool IsBurning => currentStatusEffect == StatusEffectType.Burn;

    public bool IsFrozen => currentStatusEffect == StatusEffectType.Frozen;

    private int remainingBurnTurns;
    private int remainingFrozenTurns;
    private int frozenAtDown;
    private int frozenMagDown;

    [Header("防御バフ")]
    private bool isDefenseBuffed;
    private int remainingDefenseBuffTurns;
    private int defenseBuffAmount;

    public bool IsDefenseBuffed => isDefenseBuffed;

    [Header("魔法防御バフ")]
    private bool isMagicDefenseBuffed;
    private int remainingMagicDefenseBuffTurns;
    private int magicDefenseBuffAmount;

    public bool IsMagicDefenseBuffed => isMagicDefenseBuffed;

    public void ApplyMagicDefenseBuff(BuffData buff)
    {
        if (buff == null)
        {
            return;
        }

        if (!isMagicDefenseBuffed)
        {
            magicDefenseBuffAmount = buff.amount;
            mdef += magicDefenseBuffAmount;
        }

        isMagicDefenseBuffed = true;
        remainingMagicDefenseBuffTurns = buff.durationTurns;
    }

    public void TickMagicDefenseBuff()
    {
        if (!isMagicDefenseBuffed)
        {
            return;
        }

        remainingMagicDefenseBuffTurns--;
        if (remainingMagicDefenseBuffTurns <= 0)
        {
            RemoveMagicDefenseBuff();
        }
    }

    private void RemoveMagicDefenseBuff()
    {
        mdef -= magicDefenseBuffAmount;
        magicDefenseBuffAmount = 0;
        remainingMagicDefenseBuffTurns = 0;
        isMagicDefenseBuffed = false;
    }

    public void ApplyDefenseBuff(BuffData buff)
    {
        if (buff == null)
        {
            return;
        }

        if (!isDefenseBuffed)
        {
            defenseBuffAmount = buff.amount;
            def += defenseBuffAmount;
        }

        isDefenseBuffed = true;
        remainingDefenseBuffTurns = buff.durationTurns;
    }

    public void TickDefenseBuff()
    {
        if (!isDefenseBuffed)
        {
            return;
        }

        remainingDefenseBuffTurns--;
        if (remainingDefenseBuffTurns <= 0)
        {
            RemoveDefenseBuff();
        }
    }

    private void RemoveDefenseBuff()
    {
        def -= defenseBuffAmount;
        defenseBuffAmount = 0;
        remainingDefenseBuffTurns = 0;
        isDefenseBuffed = false;
    }

    public bool ApplyBurn(StatusEffectData effect)
    {
        if (effect == null)
        {
            Debug.LogWarning("StatusEffectData が null です。火傷状態を適用できません。");
            return false;
        }

        // 状態異常は同時に1種類だけ保持する
        if (currentStatusEffect != StatusEffectType.None && currentStatusEffect != StatusEffectType.Burn)
        {
            return false;
        }

        currentStatusEffect = StatusEffectType.Burn;
        remainingBurnTurns = effect.durationTurns;
        return true;
    }

    public bool ApplyFrozen(StatusEffectData effect)
    {
        if (effect == null)
        {
            Debug.LogWarning("StatusEffectData が null です。凍結状態を適用できません。");
            return false;
        }

        if (effect.type != StatusEffectType.Frozen)
        {
            return false;
        }

        // 状態異常は同時に1種類だけ保持する
        if (currentStatusEffect != StatusEffectType.None && currentStatusEffect != StatusEffectType.Frozen)
        {
            return false;
        }

        // 再付与で能力低下が重複しないよう、初回だけ減少させる
        if (!IsFrozen)
        {
            frozenAtDown = Mathf.RoundToInt(at * effect.statDownRate);
            frozenMagDown = Mathf.RoundToInt(mag * effect.statDownRate);
            at -= frozenAtDown;
            mag -= frozenMagDown;
        }

        currentStatusEffect = StatusEffectType.Frozen;
        remainingFrozenTurns = effect.durationTurns;
        return true;
    }

    public int TickBurnDamage()
    {
        if (!IsBurning)
        {
            return 0;
        }

        if (data == null)
        {
            Debug.LogError("EnemyDataが設定されていません");
            return 0;
        }

        float burnRate;
        switch (data.enemyType)
        {
            case EnemyType.Boss:
                burnRate = BossBurnRate;
                break;
            case EnemyType.Reinforced:
                burnRate = ReinforcedBurnRate;
                break;
            case EnemyType.Normal:
            default:
                burnRate = NormalBurnRate;
                break;
        }

        int damage = Mathf.FloorToInt(maxHp * burnRate);
        damage = Mathf.Max(1, damage);
        TakeDamageRaw(damage);
        remainingBurnTurns--;
        if (remainingBurnTurns <= 0)
        {
            RemoveBurn();
        }

        return damage;
    }

    public void TickFrozen()
    {
        if (!IsFrozen)
        {
            return;
        }

        remainingFrozenTurns--;
        if (remainingFrozenTurns <= 0)
        {
            RemoveFrozen();
        }
    }

    private void RemoveBurn()
    {
        if (currentStatusEffect == StatusEffectType.Burn)
        {
            currentStatusEffect = StatusEffectType.None;
        }

        remainingBurnTurns = 0;
    }

    private void RemoveFrozen()
    {
        at += frozenAtDown;
        mag += frozenMagDown;
        frozenAtDown = 0;
        frozenMagDown = 0;
        remainingFrozenTurns = 0;
        if (currentStatusEffect == StatusEffectType.Frozen)
        {
            currentStatusEffect = StatusEffectType.None;
        }
    }

    public void Setup(EnemyData enemyData)
    {
        BattleEffects.Reset();
        ClearCounter();
        ResetStagger();
        data = enemyData;
        InitializeStatsFromData();
    }

    private void Start()
    {
        if (data == null)
        {
            Debug.LogError($"EnemyData が未設定ですわ！ object={gameObject.name}", this);
            return;
        }

        InitializeStatsFromData();
    }

    private void InitializeStatsFromData()
    {
        name = data.enemyName;
        maxHp = data.maxHp;
        hp = maxHp;
        maxMp = data.maxMp;
        mp = maxMp;
        at = data.at;
        def = data.def;
        mag = data.mag;
        mdef = data.mdef;
        evasionRate = data.evasionRate;
        damageEffect = data.damageEffect;
    }

    public int Attack(PlayerManager player)
    {
        return player.TakePhysical(at);
    }

    public void SetPartViewVisible(bool visible)
    {
        BodyPart[] parts = GetComponentsInChildren<BodyPart>(true);
        foreach (BodyPart part in parts)
        {
            part.SetPartViewVisible(visible);
        }
    }

    protected override void OnDamaged(int damage, bool isMagic)
    {
        if (damageEffect != null)
        {
            Instantiate(damageEffect, this.transform, false);
        }

        transform.DOShakePosition(damageShakeDuration, damageShakeStrength, damageShakeVibrato, 0, false, true);
    }

    protected override void OnDied()
    {
        ClearCounter();
        ResetStagger();
        DOTween.Kill(transform);
    }

    public override IEnumerator Act()
    {
        yield break;
    }

    public void TakeDamageRaw(int damage)
    {
        damage = Mathf.Max(1, damage);
        hp = Mathf.Clamp(hp - damage, 0, maxHp);
        if (hp <= 0)
        {
            OnDied();
        }
    }

    public int TakeDirectDamage(int damage)
    {
        int before = hp;
        TakeDamageRaw(BattleEffects.ReduceMainDirectDamage(damage));
        return before - hp;
    }
}
