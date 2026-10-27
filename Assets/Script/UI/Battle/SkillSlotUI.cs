using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private UnityEngine.UI.Button button;
    private TMP_Text costText;

    private SkillData skillData;
    public SkillData Skill => skillData;
    public TMP_Text Label => skillNameText;
    public TMP_Text CostLabel => costText;

    public void SetSkill(SkillData data, PlayerManager manager)
    {
        skillData = data;

        if (skillData != null)
        {
            gameObject.SetActive(true);

            int remaining = manager.GetComponent<SkillCooldowns>()?.Remaining(data) ?? 0;
            ConfigureText();
            skillNameText.text = $"{data.skillName}\n{data.description}";
            costText.text = $"MP {manager.GetSkillMpCost(data)}   CD {remaining}/{data.cooldown}";
            button.interactable = true;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void ConfigureText()
    {
        if (costText == null)
        {
            Transform existing = transform.Find("SkillCostText");
            costText = existing != null ? existing.GetComponent<TMP_Text>() :
                Instantiate(skillNameText, transform);
            costText.name = "SkillCostText";
        }
        var rect = skillNameText.rectTransform;
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20, -160); rect.offsetMax = new Vector2(-20, -20);
        skillNameText.alignment = TextAlignmentOptions.TopLeft;
        skillNameText.enableAutoSizing = false;
        skillNameText.fontSize = 30;
        skillNameText.fontSizeMin = 30; skillNameText.fontSizeMax = 30;
        skillNameText.overflowMode = TextOverflowModes.Overflow;
        skillNameText.maxVisibleLines = int.MaxValue;
        skillNameText.raycastTarget = false;
        costText.enableAutoSizing = false; costText.fontSize = 30;
        costText.fontSizeMin = 30; costText.fontSizeMax = 30; costText.raycastTarget = false;
        costText.alignment = TextAlignmentOptions.BottomLeft;
        var costRect = costText.rectTransform;
        costRect.anchorMin = new Vector2(0, 1); costRect.anchorMax = Vector2.one;
        costRect.pivot = Vector2.zero;
        costRect.offsetMin = new Vector2(20, -212); costRect.offsetMax = new Vector2(-20, -166);
    }

    public void OnClick()
    {
        if (skillData == null) return;

        if (BattleManager.Instance != null) BattleManager.Instance.OnSkillSelected(skillData);
    }
}
