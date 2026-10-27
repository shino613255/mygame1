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
    private UnityEngine.UI.ScrollRect scroll;
    private UnityEngine.UI.GridLayoutGroup grid;
    private RectTransform content;
    private const float CardWidth = 620f;
    private const float CardHeight = 300f;

    public int Page => page;
    public int PageCount => slots == null || slots.Length == 0 || player == null ? 1 :
        Mathf.Max(1, Mathf.CeilToInt(player.Skills.Count / (float)slots.Length));

    private void Awake()
    {
        CreateScrollArea();
        if (previousButton != null) previousButton.onClick.AddListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
    }

    private void OnEnable() { RefreshLayout(); Refresh(); }
    private void OnRectTransformDimensionsChange() => RefreshLayout();

    public void RefreshLayout()
    {
        if (slots == null || slots.Length == 0) return;
        Rect area = ((RectTransform)transform).rect;
        for (int i = 0; i < slots.Length; i++)
        {
            var rect = (RectTransform)slots[i].transform;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
        }
        bool narrowFooter = area.width < 760f;
        float side = narrowFooter ? 130f : 250f;
        SetFooter(previousButton, new Vector2(-side, 59));
        SetFooter(nextButton, new Vector2(side, 59));
        if (pageLabel != null)
        {
            SetRect(pageLabel.rectTransform, new Vector2(.5f, 0), new Vector2(0, narrowFooter ? 150 : 59), new Vector2(200, 70));
            SetControlFont(pageLabel);
        }
        var close = transform.Find("CloseSkills");
        if (close != null)
        {
            SetRect((RectTransform)close, new Vector2(.5f, 1), new Vector2(0, -50), new Vector2(240, 75));
            foreach (TMP_Text label in close.GetComponentsInChildren<TMP_Text>(true)) SetControlFont(label);
        }
        if (scroll == null) return;
        ((RectTransform)scroll.transform).offsetMin = new Vector2(24, narrowFooter ? 205 : 128);
        ((RectTransform)scroll.transform).offsetMax = new Vector2(-24, -110);
        int columns = scroll.viewport.rect.width >= CardWidth * 2 + 24 ? 2 : 1;
        grid.constraintCount = columns;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, CardWidth * columns + 24 * (columns - 1));
        scroll.horizontal = scroll.viewport.rect.width < CardWidth;
        UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(content);
    }

    private void CreateScrollArea()
    {
        if (scroll != null || slots == null || slots.Length == 0) return;
        var root = CreateRect("SkillScroll", transform);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        var viewport = CreateRect("Viewport", root);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero; viewport.offsetMax = new Vector2(-36, 0);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        content = CreateRect("Content", viewport);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = Vector2.zero;
        grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        grid.cellSize = new Vector2(CardWidth, CardHeight); grid.spacing = new Vector2(24, 20);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        // Reparent only at runtime so the existing prefab instance and its button references stay intact.
        foreach (SkillSlotUI slot in slots) slot.transform.SetParent(content, false);
        scroll.viewport = viewport; scroll.content = content; scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.inertia = false; scroll.scrollSensitivity = 60;
        var track = CreateRect("Scrollbar", root);
        track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one;
        track.offsetMin = new Vector2(-24, 0); track.offsetMax = Vector2.zero;
        var background = track.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(.12f, .18f, .27f, 1);
        var bar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
        var handle = CreateRect("Handle", track);
        handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one;
        handle.offsetMin = handle.offsetMax = Vector2.zero;
        var image = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(.7f, .62f, .48f, 1);
        bar.handleRect = handle; bar.targetGraphic = image;
        bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false); return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }

    private static void SetFooter(UnityEngine.UI.Button button, Vector2 position)
    {
        if (button == null) return;
        SetRect((RectTransform)button.transform, new Vector2(.5f, 0), position, new Vector2(230, 90));
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true)) SetControlFont(label);
    }

    private static void SetControlFont(TMP_Text label)
    {
        label.enableAutoSizing = false; label.fontSize = 30; label.fontSizeMin = 30; label.fontSizeMax = 30;
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
        if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; scroll.horizontalNormalizedPosition = .5f; }
    }
}
