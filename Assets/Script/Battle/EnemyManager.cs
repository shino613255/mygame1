using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyData;

public class EnemyManager : UnitBase
{
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

    // プレイヤーがこのターン何回行動できるか返す
    public int GetPlayerActionCount()
    {
        return isLegBroken ? 2 : 1;　
    }

    public void OnPartBroken(PartType part)
    {
        switch (part)
        {
            // 手の破壊:命中率 -10%
            case PartType.Hand:
                ApplyAccuracyDown(0.1f);
                break;


            // 脚の破壊:行動回数が1回増える
            case PartType.Leg:                
                isLegBroken = true;
                break;


            // 顔を壊すと防御力 -10
            case PartType.Face:
                def -= 20;
                mdef -= 20;
                def = Mathf.Max(0, def);
                mdef = Mathf.Max(0, mdef);
                break;
        }
    }

    public EnemyData data;

    // ダメージを受けたときの演出
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
        if (buff == null) return;

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
        if (!isMagicDefenseBuffed) return;

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

        Debug.Log($"魔法防御力アップ終了！ MDEF:{mdef}");
    }

    public void ApplyDefenseBuff(BuffData buff)
    {
        if (buff == null) return;

        if (!isDefenseBuffed)
        {
            defenseBuffAmount = buff.amount;
            def += defenseBuffAmount;
        }

        isDefenseBuffed = true;

        remainingDefenseBuffTurns = buff.durationTurns;

        Debug.Log(
            $"防御力アップ！ DEF:{def} 残り{remainingDefenseBuffTurns}ターン"
        );
    }

    public void TickDefenseBuff()
    {
        if (!isDefenseBuffed) return;

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

        Debug.Log($"防御力アップ終了！ DEF:{def}");
    }

    public bool ApplyBurn(StatusEffectData effect)
    {
        if (effect == null)
        {
            Debug.LogWarning(
                "StatusEffectData が null です。火傷状態を適用できません。"
            );

            return false;
        }

        // 別の状態異常が有効ならBurnは付与しない
        if (
            currentStatusEffect != StatusEffectType.None &&
            currentStatusEffect != StatusEffectType.Burn
        )
        {
            Debug.Log(
                $"{currentStatusEffect}が有効中のため、火傷は付与されませんでした。"
            );

            return false;
        }

        currentStatusEffect = StatusEffectType.Burn;

        // SkillData側に個別指定があればそちらを優先
        remainingBurnTurns = effect.durationTurns;

        Debug.Log(
            $"火傷状態になった！ 残りターン:{remainingBurnTurns}"
        );

        return true;
    }

    public bool ApplyFrozen(StatusEffectData effect)
    {
        if (effect == null)
        {
            Debug.LogWarning(
                "StatusEffectData が null です。凍結状態を適用できません。"
            );

            return false;
        }

        if (effect.type != StatusEffectType.Frozen)
        {
            return false;
        }

        // 他の状態異常がある場合は付与しない
        if (
            currentStatusEffect != StatusEffectType.None &&
            currentStatusEffect != StatusEffectType.Frozen
        )
        {
            Debug.Log(
                $"{currentStatusEffect}が有効中のため、凍結は付与されませんでした。"
            );

            return false;
        }

        // 初めて凍結したときだけ能力値を下げる
        if (!IsFrozen)
        {
            frozenAtDown =
                Mathf.RoundToInt(
                    at * effect.statDownRate
                );

            frozenMagDown =
                Mathf.RoundToInt(
                    mag * effect.statDownRate
                );

            at -= frozenAtDown;
            mag -= frozenMagDown;
        }

        currentStatusEffect =
            StatusEffectType.Frozen;

        remainingFrozenTurns =
            effect.durationTurns;

        Debug.Log(
            $"凍結状態になった！ AT:{at} MAG:{mag} " +
            $"残り{remainingFrozenTurns}ターン"
        );

        return true;
    }

    public int TickBurnDamage()
    {
        if (!IsBurning) return 0;

        if (data == null)
        {
            Debug.LogError("EnemyDataが設定されていません");                                                   
            return 0;
        }

        float burnRate;                                                                                         

        switch (data.enemyType)
        {
            case EnemyType.Boss:
                burnRate = 0.02f;                                                                                 
                break;

            case EnemyType.Reinforced:
                burnRate = 0.03f;
                break;

            case EnemyType.Normal:
            default:
                burnRate = 0.05f;
                break;
        }

        int damage = 
            Mathf.FloorToInt(maxHp * burnRate);                                                                 

        damage =Mathf.Max(1, damage);                                                                           

        TakeDamageRaw(damage);
        
        remainingBurnTurns--;

        Debug.Log(
            $"火傷ダメージ: {damage} 残りターン: {remainingBurnTurns}"
        );

        if (remainingBurnTurns <= 0)
        {
            RemoveBurn();
        }

        return damage;                                                                                         
    }

    public void TickFrozen()
    {
        if (!IsFrozen)
            return;

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

        Debug.Log("火傷状態が解除されました");
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
            currentStatusEffect =
                StatusEffectType.None;
        }

        Debug.Log(
            $"凍結状態が解除された！ AT:{at} MAG:{mag}"
        );
    }

    public void Setup(EnemyData enemyData)
    {
        data = enemyData;

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

    private void Start()
    {
        if (data == null)
        {
            Debug.LogError($"EnemyData が未設定ですわ！ object={gameObject.name}", this); 
            return;
        }

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
        BodyPart[] parts =
            GetComponentsInChildren<BodyPart>(true);

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

        transform.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);                                                      // 敵を0.3秒間、強さ0.5で揺らす。細かさは20、ランダムシードは0、スナップ0, フェードアウトあり。
        Debug.Log(name + "は" + damage + "のダメージを受けた" + (isMagic ? "(魔法)" : "(物理)"));
    }

    protected override void OnDied()
    {
        Debug.Log(name + "は倒れた");
        DOTween.Kill(transform);                                                                                       
    }

    // 現在の敵行動はBattleManager側で処理
    public override IEnumerator Act()                                                                                  
    {        
        yield break;                                                                                                    
    }
    public void TakeDamageRaw(int damage)
    {
        damage = Mathf.Max(1, damage);

        hp = Mathf.Clamp(
            hp - damage,
            0,
            maxHp
        );

        if (hp <= 0)
        {
            OnDied();
        }
    }
}