using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private Button button;

    private SkillData skillData;

    public void SetSkill(SkillData data, PlayerManager manager)
    {
        skillData = data;

        if (skillData != null)
        {
            gameObject.SetActive(true);

            skillNameText.text = skillData.skillName;
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

        BattleManager.Instance.OnSkillSelected(skillData);
    }
}