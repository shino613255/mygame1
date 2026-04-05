using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUIManager : MonoBehaviour
{
    public Text hpText;
    public Text nameText;
    // 敵のステータスUIを初期化するときに呼び出す
    public void SetupUI(EnemyManager enemy)
    {
        hpText.text = string.Format("HP:{0}", enemy.hp);
        nameText.text = string.Format("{0}", enemy.data.enemyName);
    }
    // 敵のステータスが変化したときに呼び出す
    public void UpdateUI(EnemyManager enemy)
    {
        hpText.text = $"HP:{enemy.hp}";
        nameText.text = $"{enemy.data.enemyName}";
    }
}
