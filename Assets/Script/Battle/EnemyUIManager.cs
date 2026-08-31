using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUIManager : MonoBehaviour
{
    [SerializeField] private Transform effectIconContainer;
    [SerializeField] private Image effectIconPrefab;

    [SerializeField] private Sprite burnIcon;
    [SerializeField] private Sprite defenseUpIcon;
    [SerializeField] private Sprite magicDefenseUpIcon;

    public Text hpText;
    public Text nameText;

    public void SetupUI(EnemyManager enemy)
    {
        hpText.text = $"HP:{enemy.hp}";
        nameText.text = enemy.data.enemyName;

        RefreshEffectIcons(enemy);
    }

    public void UpdateUI(EnemyManager enemy)
    {
        hpText.text = $"HP:{enemy.hp}";
        nameText.text = enemy.data.enemyName;

        RefreshEffectIcons(enemy);
    }

    private void RefreshEffectIcons(EnemyManager enemy)
    {
        if (effectIconContainer == null || effectIconPrefab == null)
            return;

        // 前回表示したアイコンを消す
        for (int i = effectIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(effectIconContainer.GetChild(i).gameObject);
        }

        if (enemy == null)
            return;

        if (enemy.IsBurning)
        {
            CreateEffectIcon(burnIcon);
        }

        if (enemy.isDefenseBuffed)
        {
            CreateEffectIcon(defenseUpIcon);
        }

        if (enemy.isMagicDefenseBuffed)
        {
            CreateEffectIcon(magicDefenseUpIcon);
        }
    }

    private void CreateEffectIcon(Sprite sprite)
    {
        if (sprite == null)
            return;

        Image icon =
            Instantiate(effectIconPrefab, effectIconContainer);

        icon.sprite = sprite;
    }
}
