using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private UnityEngine.UI.Button button;

    private SkillData skillData;
    public SkillData Skill => skillData;

    public void SetSkill(SkillData data, PlayerManager manager)
    {
        skillData = data;

        if (skillData != null)
        {
            gameObject.SetActive(true);

            int remaining = manager.GetComponent<SkillCooldowns>()?.Remaining(data) ?? 0;
            skillNameText.text = $"{data.skillName}  MP {manager.GetSkillMpCost(data)}  CD {remaining}/{data.cooldown}\n{data.description}";
            skillNameText.raycastTarget = false;
            button.interactable = true;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnClick()
    {
        if (skillData == null) return;

        if (BattleManager.Instance != null) BattleManager.Instance.OnSkillSelected(skillData);
    }
}
