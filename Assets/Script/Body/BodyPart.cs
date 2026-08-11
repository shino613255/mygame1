using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PartType
{
    Face, Belly, RightHand, LeftHand, RightLeg, LeftLeg
}

public class BodyPart : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlight;                      // 選択時のハイライト表示用のSpriteRenderer

    [Header("Part Settings")]
    public PartType partType;                                               // 部位の種類
    public bool canBreak = true;                                            // 部位が破壊可能かどうか

    [Header("Part HP")]
    [Min(1)] public int maxPartHp = 30;                                     // 部位の最大HP
    private int partHp;                                                     // 部位の現在HP      

    public bool IsBroken => canBreak && partHp <= 0;

    private void Awake()
    {
        partHp = maxPartHp;

        if (highlight == null)
            highlight = GetComponent<SpriteRenderer>();

        if (highlight != null)
            highlight.enabled = false;                                      // 初期状態ではハイライトを非表示にする
    }

    public string GetPartNameJP()                                           // それぞれの部位の日本語化
    {
        switch (partType)
        {
            case PartType.Face: return "顔";
            case PartType.Belly: return "腹";
            case PartType.RightHand: return "右手";
            case PartType.LeftHand: return "左手";
            case PartType.RightLeg: return "右脚";
            case PartType.LeftLeg: return "左脚";
            default: return partType.ToString();                            // どのケースにも当てはまらない場合デフォルトの名前を返す
        }
    }

    public void SetSelectedVisual(bool selected)                            // 選択状態のビジュアルを切り替える
    {
        if (highlight == null) return;
        highlight.enabled = selected;                                       // ハイライトの表示状態を切り替える
    }
    public int TakePartDamage(int damage)                                   // 部位にダメージを与えるメソッド
    {
        if (damage <= 0) return 0;
        if (IsBroken) return 0;                                             // すでに破壊されている場合はダメージを与えない

        int before = partHp;                                                // ダメージを与える前の部位HPを保存する
        partHp -= damage;                                                   // ダメージを部位HPから引く

        if (!canBreak)
        {
            if (partHp <= 0) partHp = 1;
            Debug.Log($"[BodyPart] {GetPartNameJP()} の部位HP: {before} → {partHp}");
            return before - partHp;
        }

        if (partHp <= 0)
        {
            partHp = 0;
            OnBroken();
        }

        Debug.Log($"[BodyPart] {GetPartNameJP()} の部位HP: {before} → {partHp}");
        return before - partHp;
    }

    private void OnBroken()                                                 // 部位が破壊されたときの処理
    {
        var enemy = GetComponentInParent<EnemyManager>();                   // EnemyManagerコンポーネントを取得し処理させる
        if (enemy == null) return;

        enemy.OnPartBroken(partType);
    }
    private void OnValidate()
    {
        if (maxPartHp < 1) maxPartHp = 1;                                   // 最大HPが1未満の場合は1に設定する
        if (partType == PartType.Belly) canBreak = false;                   // 腹部の場合は破壊不可能に設定する
    }
}
