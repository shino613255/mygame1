using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum Team
{
    Ally,
    Enemy
}

public abstract class UnitBase : MonoBehaviour
{
    [Header("Team")]
    public Team team;                                           // チームの種類（味方 or 敵）
    public enum Element { None, Fire, Wind, Ice }               // 属性の種類の定義
    [Header("Element")]
    public Element attackElement = Element.None;                // 攻撃するときの属性
    public Element resistElement = Element.None;                // 耐性を持つ属性

    [Header("Stats")]
    public int maxHp = 100;
    public int hp = 100;
    public int maxMp = 30;
    public int mp = 30;
    public int at = 10;
    public int def = 5;
    public int mag = 10;
    public int mdef = 3;

    [Header("Evasion")]
    [Range(0f, 1f)]
    public float evasionRate = 0f;                              // 回避率（0%～100%）

    [Header("Critical")]
    [Range(0f, 1f)]
    public float critRate = 0.02f;                              // クリティカル率（2%）
    public float critMultiplier = 2f;                           // クリティカルの攻撃（2倍）

    public float skillMultiplier = 2f;                          // スキル倍率（2倍）    

    public bool IsDead => hp <= 0;                              // 死亡判定
    public bool IsAlive => hp > 0;                              // 生存判定

    public bool HasMp(int cost)                                  // MPが足りるかの判定
    {
        return mp >= cost;
    }
    
    public bool TrySpendMp(int cost)                            // MP消費（足りなければ消費しない）
    {
        if (cost <= 0) return true;                             // 消費コストが0以下なら消費せずにtrueを返す
        if (mp < cost) return false;                            // mpが足りない場合使用できない
        mp -= cost;
        return true;
    }
        
    public void RecoverMp(int amount)                           // MP回復
    {
        mp = Mathf.Min(maxMp, mp + Mathf.Max(0, amount));       // maxMpを超えず、0未満にならないように回復
    }

    public int MakePhysicalDamage()
    {
        int damage = at;

        bool isCrit = Random.value < critRate;                  // クリティカルが発生したかどうかの判定
        if (isCrit)                                             // isCritがtrueの場合
        {
            damage = Mathf.RoundToInt(damage * critMultiplier); // damage*クリティカル倍率
            Debug.Log($"{name} のクリティカル！ x{critMultiplier}");
        }
        return damage;
    }

    public int MakeMagicDamage()
    {
        int damage = mag;

        damage = Mathf.RoundToInt(damage * skillMultiplier);    // damage*スキル倍率

        bool isCrit = Random.value < critRate;                  // クリティカルが発生したかどうかの判定

        if (isCrit)                                             // isCritがtrueの場合
        {
            damage = Mathf.RoundToInt(damage * critMultiplier); // damage*クリティカル倍率
            Debug.Log($"{name} の魔法クリティカル！");
        }

        return damage;
    }    
    
    public virtual int TakePhysical(int attackerAtk)
    {
        int damage = DamageRule.CalcPhysical(attackerAtk, def); // ダメージ計算(AT − DEF)
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        OnDamaged(damage, false);                               // ダメージ演出の呼び出し（物理攻撃の場合はisMagicをfalseに設定）

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
        return damage;
    }

    public virtual int TakeMagic(int attackerMag)
    {
        int damage = DamageRule.CalcMagic(attackerMag, mdef);   // ダメージ計算(MAG − MDEF)
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        OnDamaged(damage, true);                                // ダメージ演出の呼び出し（魔法攻撃の場合はisMagicをtrueに設定）

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
        return damage;
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;                                
        hp = Mathf.Min(maxHp, hp + Mathf.Max(0, amount));       // maxHpを超えず、0未満にならないように回復
    }

    protected virtual void OnDamaged(int damage, bool isMagic)
    {
        Debug.Log(name + "は" + damage + "のダメージを受けた");
    }

    protected virtual void OnDied()
    {
        Debug.Log(name + "は倒れた");
    }
    public abstract IEnumerator Act();
}
