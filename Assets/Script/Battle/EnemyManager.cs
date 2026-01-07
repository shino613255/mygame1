using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


// 敵全体を管理(ステータス/クリック検出)
public class EnemyManager : UnitBase
{    

    Action OnTapAction;

    public EnemyData data;

    [Header("VFX")]
    public GameObject damageEffect;

    private bool isBurning;
    private bool isBroken;

    private const float BurnRate = 0.05f;
    private const float AccuracyDown = 0.15f;

    public void ApplyBurn() => isBurning = true;
    public void ApplyBreak() => isBroken = true;

    public float GetAccuracyPenalty() => isBroken ? AccuracyDown : 0f;

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

    // BattleManager用（とりあえず未使用なら空でもOK）
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
