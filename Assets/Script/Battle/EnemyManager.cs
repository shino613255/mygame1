using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : UnitBase
{
    // 命中率低下の累積値
    private float accuracyPenalty = 0f;

    // 命中率低下を加算
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
    private const float BurnRate = 0.05f;
    public void ApplyBurn() => isBurning = true;
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
                spd -= 1;
                break;

            case PartType.Face:
                def -= 1;
                break;
        }
    }

    public int TickBurnDamage()
    {
        if (!isBurning) return 0;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(maxHp * BurnRate));
        TakePhysical(dmg);
        return dmg;
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