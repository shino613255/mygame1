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
    private const float CardWidth = 930f;
    private const float CardHeight = 450f;

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
        bool narrowFooter = area.width < 1100f;
        float side = narrowFooter ? 184.5f : 320f;
        SetFooter(previousButton, new Vector2(-side, 83.5f));
        SetFooter(nextButton, new Vector2(side, 83.5f));
        if (pageLabel != null)
        {
            SetRect(pageLabel.rectTransform, narrowFooter ? Vector2.one : new Vector2(.5f, 0),
                narrowFooter ? new Vector2(-112, -50) : new Vector2(0, 83.5f), new Vector2(200, 70));
            SetControlFont(pageLabel);
        }
        var close = transform.Find("CloseSkills");
        if (close != null)
        {
            SetRect((RectTransform)close, new Vector2(.5f, 1), new Vector2(0, -50), new Vector2(240, 75));
            foreach (TMP_Text label in close.GetComponentsInChildren<TMP_Text>(true)) SetControlFont(label);
        }
        if (scroll == null) return;
        float margin = area.width >= 1900 ? 6 : 24;
        ((RectTransform)scroll.transform).offsetMin = new Vector2(margin, 170);
        ((RectTransform)scroll.transform).offsetMax = new Vector2(-margin, -110);
        int columns = scroll.viewport.rect.width >= CardWidth * 2 + 12 ? 2 : 1;
        grid.constraintCount = columns;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, CardWidth * columns + 12 * (columns - 1));
        bool horizontalChanged = scroll.horizontal != (scroll.viewport.rect.width < CardWidth);
        scroll.horizontal = scroll.viewport.rect.width < CardWidth;
        scroll.viewport.offsetMin = new Vector2(0, scroll.horizontal ? 36 : 0);
        if (horizontalChanged)
        {
            // ScrollRect binds scrollbar input only while its axis is enabled.
            scroll.horizontalScrollbar = scroll.horizontalScrollbar;
            scroll.horizontalNormalizedPosition = 0;
        }
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
        grid.cellSize = new Vector2(CardWidth, CardHeight); grid.spacing = new Vector2(12, 20);
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
        var horizontalTrack = CreateRect("HorizontalScrollbar", root);
        horizontalTrack.anchorMin = Vector2.zero; horizontalTrack.anchorMax = new Vector2(1, 0);
        horizontalTrack.offsetMin = Vector2.zero; horizontalTrack.offsetMax = new Vector2(-36, 24);
        var horizontalBackground = horizontalTrack.gameObject.AddComponent<UnityEngine.UI.Image>();
        horizontalBackground.color = background.color;
        var horizontalBar = horizontalTrack.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
        var horizontalHandle = CreateRect("Handle", horizontalTrack);
        horizontalHandle.anchorMin = Vector2.zero; horizontalHandle.anchorMax = Vector2.one;
        horizontalHandle.offsetMin = horizontalHandle.offsetMax = Vector2.zero;
        var horizontalImage = horizontalHandle.gameObject.AddComponent<UnityEngine.UI.Image>();
        horizontalImage.color = image.color;
        horizontalBar.handleRect = horizontalHandle; horizontalBar.targetGraphic = horizontalImage;
        horizontalBar.direction = UnityEngine.UI.Scrollbar.Direction.LeftToRight;
        scroll.horizontalScrollbar = horizontalBar;
        scroll.horizontalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
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
        SetRect((RectTransform)button.transform, new Vector2(.5f, 0), position, new Vector2(345, 135));
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true)) SetControlFont(label, 45);
    }

    private static void SetControlFont(TMP_Text label, float size = 30)
    {
        label.enableAutoSizing = false; label.fontSize = size; label.fontSizeMin = size; label.fontSizeMax = size;
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
        if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; scroll.horizontalNormalizedPosition = 0; }
    }
}
