using UnityEngine;

public class GrowthSelectionManager : MonoBehaviour
{
    [SerializeField] private GameObject growthPanel;

    [SerializeField] private PlayerManager player;
    [SerializeField] private PlayerUIManager playerUI;
    [SerializeField] private QuestManager questManager;

    private void Start()
    {
        growthPanel.SetActive(false);
    }

    public void Open()
    {
        growthPanel.SetActive(true);
    }

    public void SelectMaxHpUp()
    {
        player.EnhanceMaxHpRate(1.8f);      

        FinishSelection();
    }

    public void SelectMagicAttackUp()
    {
        player.EnhanceMagicAttackRate(1.2f);
        FinishSelection();
    }

    public void SelectDefenseUp()
    {
        player.EnhanceDefenseRate(1.5f);
        FinishSelection();
    }

    private void FinishSelection()
    {
        playerUI.UpdateUI(player);

        growthPanel.SetActive(false);

        questManager.ContinueAfterGrowthSelection();
    }
}