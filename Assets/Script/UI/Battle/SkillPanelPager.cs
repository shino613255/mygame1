using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillPanelPager : MonoBehaviour
{
    [SerializeField] private SkillSlotUI[] slots;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageLabel;

    private PlayerManager player;
    private int page;

    public int Page => page;

    public int PageCount =>
        slots == null || slots.Length == 0 || player == null
            ? 1
            : Mathf.Max(1, Mathf.CeilToInt(
                player.Skills.Count / (float)slots.Length));

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPage);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Bind(PlayerManager owner)
    {
        player = owner;
        page = 0;
        Refresh();
    }

    public void PreviousPage()
    {
        page = Mathf.Max(0, page - 1);
        Refresh();
    }

    public void NextPage()
    {
        page = Mathf.Min(PageCount - 1, page + 1);
        Refresh();
    }

    public void Refresh()
    {
        if (player == null || slots == null || slots.Length == 0)
            return;

        page = Mathf.Clamp(page, 0, PageCount - 1);

        for (int i = 0; i < slots.Length; i++)
        {
            int index = page * slots.Length + i;

            slots[i].SetSkill(
                index < player.Skills.Count
                    ? player.Skills[index]
                    : null,
                player
            );
        }

        if (pageLabel != null)
            pageLabel.text = $"スキル {page + 1} / {PageCount}";

        if (previousButton != null)
            previousButton.interactable = page > 0;

        if (nextButton != null)
            nextButton.interactable = page < PageCount - 1;
    }
}