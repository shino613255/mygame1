#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public partial class ClassExpansionPlayModeTests
{
    private static IEnumerator CaptureLargeSkillPage(string name, int width, int height)
    {
        using (var view = new PresentationView(width, height))
        {
            Component pager = Find("SkillPanelPager"); Call(pager, "RefreshLayout"); Call(pager, "Refresh");
            yield return null; Canvas.ForceUpdateCanvases();
            var scroll = (UnityEngine.UI.ScrollRect)Get(pager, "scroll");
            Component[] slots = FindAll("SkillSlotUI").Where(s => s.gameObject.activeInHierarchy)
                .OrderBy(s => s.transform.GetSiblingIndex()).ToArray();
            bool scrolling = scroll.content.rect.height > scroll.viewport.rect.height + 1;
            for (int i = 0; i < slots.Length; i++)
            {
                ScrollToCard(pager, slots[i]); yield return null; Canvas.ForceUpdateCanvases();
                ValidateSkillLayout(name, Camera.main, width, height);
                Rect bounds = ScreenRect(Camera.main, (RectTransform)slots[i].transform);
                AssertInside(bounds, ScreenRect(Camera.main, scroll.viewport), slots[i].name + " must be fully reachable");
                ClickRaycast(slots[i].GetComponent<UnityEngine.UI.Button>(), true);
                if (scrolling || i == 0) Capture(name + (scrolling ? "-card-" + (i + 1) : ""), width, height);
            }
        }
    }

    private static void ScrollToCard(Component pager, Component slot)
    {
        var scroll = (UnityEngine.UI.ScrollRect)Get(pager, "scroll");
        Canvas.ForceUpdateCanvases(); UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        var rect = (RectTransform)slot.transform;
        float fromTop = -scroll.content.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
        float maximum = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
        scroll.StopMovement(); scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(fromTop - scroll.viewport.rect.height * .5f, 0, maximum));
    }

    private static PointerEventData ClickRaycast(UnityEngine.UI.Button button, bool enlargedArea)
    {
        var rect = (RectTransform)button.transform;
        Vector3 local = enlargedArea ? new Vector3(rect.rect.xMax - 24, rect.rect.center.y) : (Vector3)rect.rect.center;
        var canvas = button.GetComponentInParent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(local));
        var data = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
        Assert.That(hits, Is.Not.Empty, button.name);
        Assert.That(hits[0].gameObject.transform.IsChildOf(button.transform), Is.True,
            button.name + " must receive the click; hit " + hits[0].gameObject.name + " at " + position);
        if (enlargedArea) Assert.That(local.x, Is.GreaterThan(400 * .5f), "Click must reach beyond the old card width");
        return data;
    }

    private static void ClickLargeButton(UnityEngine.UI.Button button, bool enlargedArea = false)
    {
        var data = ClickRaycast(button, enlargedArea);
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
    }

    private static Rect ScreenRect(Camera camera, RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2 min = camera.WorldToScreenPoint(corners[0]), max = camera.WorldToScreenPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void AssertInside(Rect inner, Rect outer, string context)
    {
        Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 1), context);
        Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 1), context);
        Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 1), context);
        Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 1), context);
    }

    [UnityTest]
    public IEnumerator LargeControlsScrollNavigateAndReopenWithoutDuplicatingButtons()
    {
        yield return LoadClass("Warrior");
        var panel = (GameObject)Get(Find("BattleManager"), "skillSelectionPanel"); panel.SetActive(true);
        Component pager = Find("SkillPanelPager"); Call(pager, "Bind", Find("PlayerManager"));
        var slots = ((IEnumerable)Get(pager, "slots")).Cast<Component>().ToArray();
        int[] ids = slots.Select(s => s.GetInstanceID()).ToArray();
        using (var view = new PresentationView(800, 600))
        {
            Call(pager, "RefreshLayout"); yield return null; Canvas.ForceUpdateCanvases();
            var scroll = (UnityEngine.UI.ScrollRect)Get(pager, "scroll");
            Assert.That(((UnityEngine.UI.GridLayoutGroup)Get(pager, "grid")).constraintCount, Is.EqualTo(1));
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0).Within(1));
            var wheel = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -3) };
            ExecuteEvents.Execute(scroll.gameObject, wheel, ExecuteEvents.scrollHandler); yield return null;
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(0));
            float y = scroll.content.anchoredPosition.y;
            scroll.verticalScrollbar.value = 0; yield return null;
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(y));
            ClickLargeButton((UnityEngine.UI.Button)Get(pager, "nextButton")); yield return null;
            Assert.That((int)Get(pager, "Page"), Is.EqualTo(1));
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0).Within(1));
            Assert.That(slots.Count(s => s.gameObject.activeSelf), Is.EqualTo(2));
            ClickLargeButton((UnityEngine.UI.Button)Get(pager, "previousButton")); yield return null;
            Assert.That((int)Get(pager, "Page"), Is.Zero);
            for (int i = 0; i < 3; i++) { panel.SetActive(false); panel.SetActive(true); yield return null; }
            Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true).Length, Is.EqualTo(1));
            CollectionAssert.AreEqual(ids, ((IEnumerable)Get(pager, "slots")).Cast<Component>().Select(s => s.GetInstanceID()));
            Assert.That(panel.GetComponent<UnityEngine.UI.CanvasScaler>().scaleFactor, Is.EqualTo(1));
            Assert.That(panel.GetComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode,
                Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize));
            Assert.That(((UnityEngine.UI.Button)Get(pager, "nextButton")).onClick.GetPersistentEventCount(), Is.Zero);
        }
    }
}
#endif
