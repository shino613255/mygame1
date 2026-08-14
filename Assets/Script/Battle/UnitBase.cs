using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
public enum Team
{
    Ally,
    Enemy
}

public abstract class UnitBase : MonoBehaviour
{
    [Header("Team")]
    public Team team;
    public enum Element { None, Fire, Wind, Ice }  
    [Header("Element")]
    public Element attackElement = Element.None;                        // 攻撃するときの属性
    public Element resistElement = Element.None;                        // 耐性を持つ属性

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
    public float evasionRate = 0f;                           

    [Header("Critical")]
    [Range(0f, 1f)]
    public float critRate = 0.02f;                           
    public float critMultiplier = 2f;                         

    public float skillMultiplier = 2f;                          

    public bool IsDead => hp <= 0;                              
    public bool IsAlive => hp > 0;                              

    public bool HasMp(int cost)                               
    {
        return mp >= cost;
    }
    
    public bool TryUseMp(int cost)                            
    {
        if (cost <= 0) return true;                             
        if (mp < cost) return false;                            
        mp -= cost;
        return true;
    }

   
    public void RecoverMp(int amount)                                        // MP回復    
    {
        mp = Mathf.Min(maxMp, mp + Mathf.Max(0, amount));      
    }

    public int MakePhysicalDamage()
    {
        int damage = at;

        bool isCrit = Random.value < critRate;                            

        if (isCrit)                                             
        {
            damage = Mathf.RoundToInt(damage * critMultiplier);             // damage*クリティカル倍率
            Debug.Log($"{name} のクリティカル！ x{critMultiplier}");
        }
        return damage;
    }

    public int MakeMagicDamage()
    {
        int damage = mag;

        damage = Mathf.RoundToInt(damage * skillMultiplier);                // damage*スキル倍率

        bool isCrit = Random.value < critRate;                              

        if (isCrit)                                                        
        {
            damage = Mathf.RoundToInt(damage * critMultiplier);             
            Debug.Log($"{name} の魔法クリティカル！");
        }

        return damage;
    }    
    
    public virtual int TakePhysical(int attackerAtk)
    {
        int damage = DamageRule.CalcPhysical(attackerAtk, def);             // ダメージ計算(AT − DEF)
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        OnDamaged(damage, false);                                           // ダメージ演出の呼び出し（物理攻撃の場合はisMagicをfalseに設定）

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
        return damage;
    }

    public virtual int TakeMagic(int attackerMag)
    {
        int damage = DamageRule.CalcMagic(attackerMag, mdef);               
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        OnDamaged(damage, true);                                            

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
        hp = Mathf.Min(maxHp, hp + Mathf.Max(0, amount));                   // maxHpを超えず、0未満にならないように回復
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
