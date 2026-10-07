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
    public Element attackElement = Element.None;
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
    public float critRate = 0.03f;
    public float critMultiplier = 1.5f;

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


    public void RecoverMp(int amount)
    {
        mp = Mathf.Min(maxMp, mp + Mathf.Max(0, amount));
    }

    public virtual int TakePhysical(int attackerAtk)
    {
        int damage = DamageRule.CalcNormalAttack(attackerAtk, def);
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        OnDamaged(damage, false);

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
        return damage;
    }    

    public virtual int TakeDamageRaw(
    int damage,
    bool isMagic = false
)
    {
        damage = Mathf.Max(1, damage);

        hp = Mathf.Clamp(
            hp - damage,
            0,
            maxHp
        );

        OnDamaged(damage, isMagic);

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }

        return damage;
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
