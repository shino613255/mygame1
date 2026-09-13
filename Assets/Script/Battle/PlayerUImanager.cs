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
    }

    public void UpdateUI(PlayerManager player)
    {
        hpSlider.value = player.hp;
        mpSlider.value = player.mp;

        hpText.text = $"{player.hp} / {player.maxHp}";
        mpText.text = $"{player.mp} / {player.maxMp}";
    }
}