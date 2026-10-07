using UnityEngine;
using TMPro;

public class PlayerStatusDetailUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI atText;
    [SerializeField] private TextMeshProUGUI magText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI mdefText;

    [SerializeField] private GameObject statusPanel;
    [SerializeField] private PlayerManager player;

    private void Start()
    {
        statusPanel.SetActive(false);
    }

    public void ToggleStatusPanel()
    {
        bool isOpen = !statusPanel.activeSelf;

        statusPanel.SetActive(isOpen);

        if (isOpen)
        {
            UpdateUI(player);
        }
    }

    public void UpdateUI(PlayerManager player)
    {
        if (player == null)
            return;

        nameText.text = $"ñºëOÅF{player.name}";
        hpText.text = $"HP : {player.hp} / {player.maxHp}";
        mpText.text = $"MP : {player.mp} / {player.maxMp}";
        atText.text = $"AT : {player.at}";
        magText.text = $"MAG : {player.mag}";
        defText.text = $"DEF : {player.def}";
        mdefText.text = $"MDEF : {player.mdef}";
    }
}