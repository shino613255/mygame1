using System.Collections;
// DropTable.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Drop/DropTable", fileName = "DropTable_")]
public class DropTable : ScriptableObject
{
    [System.Serializable]
    public class DropEntry
    {
        [Header("アイテムID（とりあえずint。後でItemData参照に拡張OK）")]
        public int itemId;

        [Header("ドロップ確率(0〜1)")]
        [Range(0f, 1f)]
        public float probability = 0.1f;

        [Header("個数範囲")]
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;

        public DropEntry(int itemId, float probability, int minAmount, int maxAmount)
        {
            this.itemId = itemId;
            this.probability = probability;
            this.minAmount = minAmount;
            this.maxAmount = maxAmount;
        }
    }

    public List<DropEntry> drops = new();

    // 抽選して結果を返す（itemId -> amount）
    public Dictionary<int, int> RollDrops()
    {
        var result = new Dictionary<int, int>();

        foreach (var d in drops)
        {
            if (d == null) continue;
            if (d.itemId < 0) continue;
            if (d.maxAmount < d.minAmount) d.maxAmount = d.minAmount;

            if (Random.value <= d.probability)
            {
                int amount = Random.Range(d.minAmount, d.maxAmount + 1);
                if (result.ContainsKey(d.itemId)) result[d.itemId] += amount;
                else result.Add(d.itemId, amount);
            }
        }

        return result;
    }
}
