using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyData;

public class EnemyManager : UnitBase
{

    private float accuracyPenalty = 0f;                                                                         // 命中率ペナルティ

    public void ApplyAccuracyDown(float value)                                                                  // BattleManager などから参照する用
    {
        accuracyPenalty += value;                                                                               // 別のコードからのペナルティを加算
    }

    public float GetAccuracyPenalty()
    {
        return accuracyPenalty;                                                                                 // 現在の命中率ペナルティを返す
    }

    public void OnPartBroken(PartType part)
    {
        switch (part)                                                                                           // 破壊された部位ごとの処理分岐
        {
            case PartType.RightHand:
            case PartType.LeftHand:
                ApplyAccuracyDown(0.2f);                                                                        // 命中率ペナルティを加算
                break;

            case PartType.RightLeg:
            case PartType.LeftLeg:
                at -= 1;                                                                                        // 攻撃力を減少
                break;

            case PartType.Face:
                def -= 1;                                                                                       // 防御力を減少
                break;
        }
    }

    public EnemyData data;

    [Header("VFX")]
    public GameObject damageEffect;                                                                             // ダメージを受けたときの演出（Inspectorで設定）

    private bool isBurning;                                                                                     // 火傷状態かどうかのフラグ
    private int remainingBurnTurns;                                                                             // 火傷状態の残りターン数            
    private GameObject burnVfxInstance;                                                                         // 火傷VFXの保存                                          

    public void ApplyBurn(StatusEffectData effect, int duration)                                                // BattleManager などから参照する用
    {
        if (effect == null)
        {
            Debug.LogWarning("StatusEffectData が null です。火傷状態を適用できませんわ。");
            return;
        }

        isBurning = true;                                                                                       // 敵を火傷状態にする

        remainingBurnTurns = 
            duration > 0                                                                                        // duration が 0 より
                ? duration                                                                                      // 大きい場合は、指定された duration を使用し、
                : effect.durationTurns;                                                                         // それ以外の場合は effect.durationTurns を使用する

        if (burnVfxInstance == null && effect.vfxPrefab != null)                                                // まだ火傷VFXが生成されていない場合、かつ effect.vfxPrefab が null でない場合
        {
            burnVfxInstance = Instantiate(
                effect.vfxPrefab,                                                                               // StatusEffectDataに設定されている火傷エフェクトを生成
                transform                                                                                       // 敵の位置に VFX を生成
            );

            burnVfxInstance.transform.localPosition = Vector3.zero;                                             // VFXの位置を敵(0,0,0)に設定
            burnVfxInstance.transform.localRotation = Quaternion.identity;                                      // VFXの回転をリセット
            burnVfxInstance.transform.localScale = Vector3.one;                                                 // VFXのスケールを(1,1,1)に設定                
            Debug.Log($"火傷VFXを生成しましたわ:{burnVfxInstance.name}");
        }

        Debug.Log($"火傷状態になった！ 残りターン: {remainingBurnTurns}");
    }   

    public int TickBurnDamage()
    {
        if (!isBurning) return 0;                                                                               // 火傷状態でない場合はダメージを0にして返す

        if (data == null)
        {
            Debug.LogError("EnemyDataが設定されていません");                                                     // EnemyDataがInspectorに設定されていない場合のエラーログ
            return 0;
        }

        float burnRate;                                                                                         // 火傷ダメージの割合を決定する変数

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
            Mathf.FloorToInt(maxHp * burnRate);                                                                 // 最大HPに基づいて火傷ダメージを計算

        damage =Mathf.Max(1, damage);                                                                           // ダメージが1未満にならないように調整

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
        def = data.def;
        mag = data.mag;
        mdef = data.mdef;
        evasionRate = data.evasionRate;

        damageEffect = data.damageEffect;
    }

    public int Attack(PlayerManager player)
    {
        return player.TakePhysical(at);                                                                                 // プレイヤーに物理攻撃を行い、ダメージ量を返す
    }

    protected override void OnDamaged(int damage, bool isMagic)
    {
        if (damageEffect != null)
        {
            Instantiate(damageEffect, this.transform, false);                                                           // ダメージエフェクトを敵の位置を基準に生成
        }

        transform.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);                                                      // 敵を0.3秒間、強さ0.5で揺らす。細かさは20、ランダムシードは0、スナップ0, フェードアウトあり。
        Debug.Log(name + "は" + damage + "のダメージを受けた" + (isMagic ? "(魔法)" : "(物理)"));
    }

    protected override void OnDied()
    {
        Debug.Log(name + "は倒れた");
        DOTween.Kill(transform);                                                                                        // 敵の揺れアニメーションを停止
    }

    public override IEnumerator Act()                                                                                   // UnitBaseの実装。現在の敵行動はBattleManager側で処理
    {        
        yield break;                                                                                                    // 現在は何も行動しない
    }
    public void TakeDamageRaw(int damage)                                                                               // 敵に直接ダメージを与えるメソッド
    {
        damage = Mathf.Max(1, damage);                                                                                   // ダメージが0未満にならないように調整                                      
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
    }
}