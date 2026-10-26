using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillPanelPager : MonoBehaviour
{
    [SerializeField] private SkillSlotUI[] slots;
    [SerializeField] private UnityEngine.UI.Button previousButton;
    [SerializeField] private UnityEngine.UI.Button nextButton;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private PlayerManager player;
    private int page;

    public int Page => page;
    public int PageCount => slots == null || slots.Length == 0 || player == null ? 1 :
        Mathf.Max(1, Mathf.CeilToInt(player.Skills.Count / (float)slots.Length));

    private void Awake()
    {
        if (previousButton != null) previousButton.onClick.AddListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
    }

    private void OnEnable() { RefreshLayout(); Refresh(); }
    private void OnRectTransformDimensionsChange() => RefreshLayout();

    public void RefreshLayout()
    {
        if (slots == null || slots.Length == 0) return;
        Rect area = ((RectTransform)transform).rect;
        float width = Mathf.Min(400f, Mathf.Max(140f, (area.width - 70f) * .5f));
        float height = Mathf.Min(195f, Mathf.Max(95f, (area.height - 170f) * .5f));
        for (int i = 0; i < slots.Length; i++)
        {
            var rect = (RectTransform)slots[i].transform;
            rect.anchoredPosition = new Vector2((i % 2 == 0 ? -1 : 1) * (width * .5f + 10f),
                (i / 2 == 0 ? 1 : -1) * (height * .5f + 10f));
            rect.sizeDelta = new Vector2(width, height);
        }
        float footer = -area.height * .5f + 40f;
        if (previousButton != null) ((RectTransform)previousButton.transform).anchoredPosition = new Vector2(-180, footer);
        if (nextButton != null) ((RectTransform)nextButton.transform).anchoredPosition = new Vector2(180, footer);
        if (pageLabel != null) ((RectTransform)pageLabel.transform).anchoredPosition = new Vector2(0, footer);
    }

    public void Bind(PlayerManager owner)
    {
        player = owner;
        page = 0;
        Refresh();
    }

    public void PreviousPage() { page = Mathf.Max(0, page - 1); Refresh(); }
    public void NextPage() { page = Mathf.Min(PageCount - 1, page + 1); Refresh(); }

    public void Refresh()
    {
        if (player == null || slots == null || slots.Length == 0) return;
        page = Mathf.Clamp(page, 0, PageCount - 1);
        for (int i = 0; i < slots.Length; i++)
        {
            int index = page * slots.Length + i;
            slots[i].SetSkill(index < player.Skills.Count ? player.Skills[index] : null, player);
        }
        if (pageLabel != null) pageLabel.text = $"スキル {page + 1} / {PageCount}";
        if (previousButton != null) previousButton.interactable = page > 0;
        if (nextButton != null) nextButton.interactable = page < PageCount - 1;
    }
}
