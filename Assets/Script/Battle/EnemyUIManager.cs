using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUIManager : MonoBehaviour
{
    public Text hpText;
    public Text nameText;

    public void SetupUI(EnemyManager enemy)
    {
        hpText.text = $"HP:{enemy.data.maxHp}";
        nameText.text = enemy.data.enemyName;
    }

    public void UpdateUI(EnemyManager enemy)
    {
        hpText.text = $"HP:{enemy.hp}";
        nameText.text = enemy.data.enemyName;
    }
}
