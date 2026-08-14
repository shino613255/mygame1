using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public Text hpText;
    public Text mpText;

    public void SetupUI(PlayerManager player)
    {
        hpText.text = string.Format("HP:{0}", player.hp);
        mpText.text = string.Format("MP:{0}", player.mp);
    }

    public void UpdateUI(PlayerManager player)
    {
        hpText.text = $"HP:{player.hp}";
        mpText.text = $"MP:{player.mp}";
    }
}
