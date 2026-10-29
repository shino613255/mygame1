using UnityEngine;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private UnityEngine.UI.Button button;
    private SkillData skillData;

    public SkillData Skill => skillData;

    public TMP_Text Label => skillNameText;

    public TMP_Text CostLabel => costText;

    public void SetSkill(SkillData data, PlayerManager manager)
    {
        skillData = data;
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        skillNameText.text = data.skillName;
        skillDescriptionText.text = data.description;
        int remaining = manager.GetComponent<SkillCooldowns>()?.Remaining(data) ?? 0;
        costText.text = $"MP {manager.GetSkillMpCost(data)}   CD {remaining}/{data.cooldown}";
        button.interactable = true;
    }

    public void OnClick()
    {
        if (skillData == null)
        {
            return;
        }

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnSkillSelected(skillData);
        }
    }
}
