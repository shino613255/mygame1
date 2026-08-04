using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyData;

public class EnemyManager : UnitBase
{

    private float accuracyPenalty = 0f;

    public void ApplyAccuracyDown(float value)
    {
        accuracyPenalty += value;
    }

    // BattleManager などから参照する用
    public float GetAccuracyPenalty()
    {
        return accuracyPenalty;
    }

    public EnemyData data;

    [Header("VFX")]
    public GameObject damageEffect;

    private bool isBurning;
    private int remainingBurnTurns;
    private GameObject burnVfxInstance;

    public void ApplyBurn(StatusEffectData effect, int duration)
    {
        Debug.Log(
        $"ApplyBurn called / " +
        $"effect={(effect != null ? effect.name : "null")} / " +
        $"vfx={(effect != null && effect.vfxPrefab != null ? effect.vfxPrefab.name : "null")} / " +
        $"currentInstance={(burnVfxInstance != null ? burnVfxInstance.name : "null")}"
    );

        if (effect == null)
        {
            Debug.LogWarning("StatusEffectData が null です。火傷状態を適用できませんわ。");
            return;
        }

        isBurning = true;

        remainingBurnTurns = 
            duration > 0
                ? duration
                : effect.durationTurns;

        if (burnVfxInstance == null && effect.vfxPrefab != null)
        {
            burnVfxInstance = Instantiate(
                effect.vfxPrefab,
                transform
            );

            burnVfxInstance.transform.localPosition = Vector3.zero;
            burnVfxInstance.transform.localRotation = Quaternion.identity;
            burnVfxInstance.transform.localScale = Vector3.one;
            Debug.Log($"火傷VFXを生成しましたわ:{burnVfxInstance.name}");
        }

        Debug.Log($"火傷状態になった！ 残りターン: {remainingBurnTurns}");
    }   
    public void OnPartBroken(PartType part)
    {
        switch (part)
        {
            case PartType.RightHand:
            case PartType.LeftHand:
                ApplyAccuracyDown(0.2f);
                break;

            case PartType.RightLeg:
            case PartType.LeftLeg:
                break;

            case PartType.Face:
                def -= 1;
                break;
        }
    }

    public int TickBurnDamage()
    {
        if (!isBurning) return 0;

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
    private void RemoveBurn()
    {
        isBurning = false;
        remainingBurnTurns = 0;

        if (burnVfxInstance != null)
        {
            Destroy(burnVfxInstance);
            burnVfxInstance = null;
        }

        Debug.Log("火傷状態が解除されました");
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
        at = data.at;
        damageEffect = data.damageEffect;
        maxMp = data.maxMp;
        mp = data.maxMp;
        def = data.def;
        mag = data.mag;
        mdef = data.mdef;
    }

    // プレイヤーを攻撃（物理）
    public int Attack(PlayerManager player)
    {
        return player.TakePhysical(at);
    }

    // ダメージ演出だけ敵用に上書き
    protected override void OnDamaged(int damage, bool isMagic)
    {
        if (damageEffect != null)
        {
            Instantiate(damageEffect, this.transform, false);
        }

        transform.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);
        Debug.Log(name + "は" + damage + "のダメージを受けた" + (isMagic ? "(魔法)" : "(物理)"));
    }

    // 死亡演出だけ敵用に上書き
    protected override void OnDied()
    {
        Debug.Log(name + "は倒れた");
        DOTween.Kill(transform);
    }

    // BattleManager用
    public override IEnumerator Act()
    {        
        yield break;
    }

    private Dictionary<StatusEffectType, GameObject> activeVfx = new();

    public void ShowStatusVfx(StatusEffectData effect)
    {
        if (effect == null || effect.vfxPrefab == null) return;

        if (activeVfx.ContainsKey(effect.type)) return;

        var vfx = Instantiate(effect.vfxPrefab, transform);
        activeVfx[effect.type] = vfx;
    }

    public void TakeDamageRaw(int damage)
    {
        damage = Mathf.Max(0, damage);
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
    }

}