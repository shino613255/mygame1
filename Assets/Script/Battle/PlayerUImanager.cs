using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public Text hpText;
    public Text mpText;

    public Slider hpSlider;
    public Slider mpSlider;

    [Header("Effect Icon")]
    [SerializeField] private Transform effectIconContainer;
    [SerializeField] private Image effectIconPrefab;

    [SerializeField] private Sprite poisonIcon;

    public void SetupUI(PlayerManager player)
    {
        hpSlider.minValue = 0;
        hpSlider.maxValue = player.maxHp;
        hpSlider.value = player.hp;
        
        mpSlider.minValue = 0;
        mpSlider.maxValue = player.maxMp;
        mpSlider.value = player.mp;

        hpText.text = $"{player.hp} / {player.maxHp}";
        mpText.text = $"{player.mp} / {player.maxMp}";

        RefreshEffectIcons(player);
    }

    public void UpdateUI(PlayerManager player)
    {
        hpSlider.value = player.hp;
        mpSlider.value = player.mp;

        hpText.text = $"{player.hp} / {player.maxHp}";
        mpText.text = $"{player.mp} / {player.maxMp}";

        RefreshEffectIcons(player);
    }

    private void RefreshEffectIcons(PlayerManager player)
    {
        if (
            effectIconContainer == null ||
            effectIconPrefab == null
        )
        {
            return;
        }

        // 前回のアイコンを全部消す
        for (
            int i = effectIconContainer.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                effectIconContainer.GetChild(i).gameObject
            );
        }

        if (player == null)
            return;


        // 毒状態なら毒アイコンを表示
        if (player.IsPoisoned)
        {
            CreateEffectIcon(poisonIcon);
        }
    }
        private void CreateEffectIcon(Sprite sprite)
    {
        if (sprite == null)
            return;

        Image icon =
            Instantiate(
                effectIconPrefab,
                effectIconContainer
            );

        icon.sprite = sprite;
    }
}