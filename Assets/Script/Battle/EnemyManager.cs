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

    public float GetAccuracyPenalty()
    {
        return accuracyPenalty;                                                                                
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
                at -= 1;                                                                                        
                break;

            case PartType.Face:
                def -= 1;                                                                                       
                break;
        }
    }

    public EnemyData data;

    // ダメージを受けたときの演出
    [Header("VFX")]
    public GameObject damageEffect;                                                                             

    private bool isBurning;
    private int remainingBurnTurns;

    private bool isDefenseBuffed;
    private int remainingDefenseBuffTurns;
    private int defenseBuffAmount;
    private GameObject burnVfxInstance;

    public bool isMagicDefenseBuffed;
    private int remainingMagicDefenseBuffTurns;
    private int magicDefenseBuffAmount;
    private GameObject magicDefenseVfxInstance;


    public void ApplyMagicDefenseBuff(StatusEffectData effect, int duration)
    {
        if (effect == null) return;

        if (!isMagicDefenseBuffed)
        {
            magicDefenseBuffAmount = effect.mdefUpAmount;
            mdef += magicDefenseBuffAmount;
        }

        isMagicDefenseBuffed = true;

        remainingMagicDefenseBuffTurns =
            duration > 0
                ? duration
                : effect.durationTurns;

        if (magicDefenseVfxInstance == null && 
            effect.vfxPrefab != null)
        {
            // 敵本体のRendererを先に取得
            Renderer enemyRenderer =
                GetComponentInChildren<Renderer>();

            if (magicDefenseVfxInstance == null && effect.vfxPrefab != null)
            {
                magicDefenseVfxInstance = Instantiate(
                    effect.vfxPrefab,
                    transform.position,
                    effect.vfxPrefab.transform.rotation
                );

                Renderer[] vfxRenderers =
                    magicDefenseVfxInstance.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in vfxRenderers)
                {
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder = 20;
                }
            }
            
        }

        Debug.Log(
            $"魔法防御力アップ！ MDEF:{mdef} 残り{remainingMagicDefenseBuffTurns}ターン"
        );
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

        if (magicDefenseVfxInstance != null)
        {
            Destroy(magicDefenseVfxInstance);
            magicDefenseVfxInstance = null;
        }

        Debug.Log($"魔法防御力アップ終了！ MDEF:{mdef}");
    }

    public void ApplyDefenseBuff(StatusEffectData effect, int duration)
    {
        if (effect == null) return;

        if (!isDefenseBuffed)
        {
            defenseBuffAmount = effect.defUpAmount;
            def += defenseBuffAmount;
        }

        isDefenseBuffed = true;

        remainingDefenseBuffTurns =
            duration > 0
                ? duration
                : effect.durationTurns;

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

    public void ApplyBurn(StatusEffectData effect, int duration)                                                
    {
        if (effect == null)
        {
            Debug.LogWarning("StatusEffectData が null です。火傷状態を適用できませんわ。");
            return;
        }

        isBurning = true;                                                                                       

        // 個別指定があれば、StatusEffectDataのターン数より優先する
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
        hp = Mathf.Clamp(hp - damage, 0, maxHp);

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }
    }
}