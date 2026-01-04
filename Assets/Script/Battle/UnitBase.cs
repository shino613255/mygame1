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
    public enum Element { None, Fire, Wind, Thunder }

    [Header("Element")]
    public Element attackElement = Element.None;
    public Element resistElement = Element.None;

    [Header("Team")]
    public Team team;       //味方チーム・敵チームの区別

    [Header("Stats")]
    public int maxHp = 100;
    public int hp = 100;
    public int mp = 50;
    public int maxMp = 50;
    public int at = 10;
    public int def = 5;
    public int mag = 10;
    public int mdef = 3;
    public int spd = 5;
    public bool isDeficiency;   //欠損状態
    public bool isburn;         //火傷状態

    [Header("Critical")]

    [Header("Evasion")]
    [Range(0f, 1f)]
    public float evasionRate = 0f;  //回避率(今は0%にしてある)

    // MPが足りるかの判定
    public bool HasMp(int cost) 
    {
        return mp >= cost;
    }

    // MP消費（足りなければ消費しない）
    public bool TrySpendMp(int cost) 
    {
        if (cost <= 0) return true;
        if (mp < cost) return false;    // mpが足りない場合使用できない
        mp -= cost;
        return true;
    }

    // MP回復
    public void RecoverMp(int amount) 
    {
        mp = Mathf.Min(maxMp, mp + Mathf.Max(0, amount));   // maxMpを超えず、0未満にならないように回復
    }

    


    [Header("Level")]
    public int level = 1;                 
    public int maxLevel = 20;             
    public int exp = 0;

    [Tooltip("次のレベルに必要なEXP（Lv1->2 が index0）")]
    public int[] expToNext = new int[19]; //次のレベルに必要な経験値の表（1→2〜19→20まで)
                                          //上がる回数は19回なので19個

    //EXP(amount)加算
    public void AddExp(int amount)
    {
        if (level >= maxLevel) return;  // カンストしてたら無視

        exp += Mathf.Max(0, amount);    // マイナスの値は無視

        // レベルアップできるだけする
        while (level < maxLevel)
        {
            int need = GetNeedExpToNext(level);
            if (need <= 0) break;

            if (exp >= need)
            {
                exp -= need;
                level++;
                OnLevelUp(); // レベルアップ時の処理（下にメソッドがある）
            }
            else
            {
                break;
            }
        }

        // カンストしたらexpを0に
        if (level >= maxLevel) exp = 0;
    }

    //次Lvに必要EXPを取得
    int GetNeedExpToNext(int currentLevel)
    {
        // Lv1→2 は expToNext[0]
        int index = currentLevel - 1;
        if (expToNext == null || expToNext.Length <= index) return 0;   // 配列外アクセス防止
        return expToNext[index];
    }

    [Range(0f, 1f)]
    public float critRate = 0.02f;        //　クリティカル率（2%）
    public float critMultiplier = 2f;     //　クリティカルの攻撃（2倍）
    public float skillMultiplier = 2f;  　// スキル倍率（2倍）

    //死亡・生存判定
    public bool IsDead => hp <= 0;
    public bool IsAlive => hp > 0;

    // 魔法ダメージ（今はシンプルに mag を返す）
    public int MakeMagicDamage()
    {
        int damage = mag;

        // スキル倍率
        damage = Mathf.RoundToInt(damage * skillMultiplier);

        // 魔法クリティカル
        bool isCrit = Random.value < critRate;
        if (isCrit)
        {
            damage = Mathf.RoundToInt(damage * critMultiplier);
            Debug.Log($"{name} の魔法クリティカル！");
        }

        return damage;
    }

    //攻撃ダメージ（クリティカル込み）を作る共通関数
    public int MakePhysicalDamage()
    {
        int damage = at;

        // クリティカルのダメージ計算
        bool isCrit = Random.value < critRate;
        if (isCrit)
        {
            damage = Mathf.RoundToInt(damage * critMultiplier);
            Debug.Log($"{name} のクリティカル！ x{critMultiplier}");
        }
        return damage;
    }
    
    // 物理被ダメ（DEF軽減）
    public virtual int TakePhysical(int attackerAtk)
    {
        int damage = DamageRule.CalcPhysical(attackerAtk, def);
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        OnDamaged(damage, false);

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
        return damage;
    }

    // 魔法被ダメ（MDEF軽減）
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

    // 回復量(amount)
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Min(maxHp, hp + amount); // maxHpを超えない
    }

    // レベルアップ時の処理（ステータス上昇など）
    protected virtual void OnLevelUp()
    {
        maxHp += 10;  // 上限が上がる
        at += 2;

        hp = maxHp;   // レベルアップで全回復

        Debug.Log($"{name} は Lv{level} になった！");
    }

    // ダメージ演出（必要なキャラだけ上書きする）
    protected virtual void OnDamaged(int damage, bool isMagic)
    {
        Debug.Log(name + "は" + damage + "のダメージを受けた");
    }

    // 死亡演出（必要なキャラだけ上書きする）
    protected virtual void OnDied()
    {
        Debug.Log(name + "は倒れた");
    }
    public abstract IEnumerator Act();
}
