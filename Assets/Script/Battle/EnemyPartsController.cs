using System.Collections;
// EnemyPartsController.cs
using System.Collections.Generic;
using UnityEngine;

public class EnemyPartsController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private BattleManager battleManager;

    [Header("部位タップ用（敵の各部位に付いてる EnemyPart を登録）")]
    [SerializeField] private List<EnemyPart> parts = new();

    private void Awake()
    {
        // battleManager未設定ならシーン内から探す（1つ想定）
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        // parts未設定なら子から自動取得（ボタン/コライダーが子にある想定）
        if (parts == null || parts.Count == 0)
            parts = new List<EnemyPart>(GetComponentsInChildren<EnemyPart>(true));

        // EnemyPart 側に battle を渡す（タップ時に呼べるように）
        foreach (var p in parts)
        {
            if (p == null) continue;
            p.battle = battleManager;
        }
    }

    // プレイヤーターンだけタップ可能にする
    public void EnablePartsTap(bool enable)
    {
        foreach (var p in parts)
        {
            if (p == null) continue;
            p.SetTapEnabled(enable);
        }
    }

    // 必要なら外からBattleManagerを差し替える用
    public void SetBattleManager(BattleManager bm)
    {
        battleManager = bm;
        foreach (var p in parts)
        {
            if (p == null) continue;
            p.battle = battleManager;
        }
    }
}
