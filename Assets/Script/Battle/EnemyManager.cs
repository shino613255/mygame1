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

    // 追加：部位タップ管理
    [Header("Parts (部位タップ用)")]
    public List<EnemyPart> parts = new();

    // 追加：部位ごとの状態
    private HashSet<BodyPart> burnParts = new HashSet<BodyPart>();
    private HashSet<BodyPart> brokenParts = new HashSet<BodyPart>();

    // 追加：1部位につき毎ターン5%（例）
    private const float BurnRatePerPart = 0.05f;
    // 追加：1部位につき命中-15%（例）
    private const float AccuracyDownPerBrokenPart = 0.15f;

    // 追加：火傷付与（部位ごと）
    public void ApplyBurn(BodyPart part)
    {
        burnParts.Add(part);
    }

    // 追加：欠損付与（部位ごと）
    public void ApplyBreak(BodyPart part)
    {
        brokenParts.Add(part);
    }

    // 追加：欠損による命中ペナルティ（この敵の攻撃命中に使う）
    public float GetAccuracyPenalty()
    {
        return brokenParts.Count * AccuracyDownPerBrokenPart;
    }

    // 追加：ターン開始時の火傷ダメージ（この敵が火傷してるなら減る）
    public int TickBurnDamage()
    {
        if (burnParts.Count <= 0) return 0;

        // 1部位ごとに maxHp の5%（最低1）
        int dmg = Mathf.Max(1, Mathf.RoundToInt(maxHp * BurnRatePerPart * burnParts.Count));
        TakePhysical(dmg); // 物理扱いでHPを減らす（演出/死亡処理も流れる）
        return dmg;
    }
    public void EnablePartsTap(bool enable)
    {
        foreach (var p in parts)
        {
            if (p == null) continue;
            p.SetTapEnabled(enable);
        }
    }


    private void Start()
    {
        // EnemyData から初期化
        name = data.enemyName;
        hp = data.maxHp;
        at = data.at;
        damageEffect = data.damageEffect;

        // ※EnemyDataにまだdef/mat/mdef/spdが無いなら、とりあえずInspectorの値を使う
        // （将来 EnemyData に追加したらここで代入すればOK）
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

    // タップイベント
    public void AddEventListenerOnTap(Action action)
    {
        OnTapAction += action;
    }

    public void OnTap()
    {
        OnTapAction?.Invoke();
    }
        private void OnDestroy()
    {
        OnTapAction = null;
    }

}
