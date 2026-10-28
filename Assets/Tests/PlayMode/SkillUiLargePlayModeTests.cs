#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public partial class ClassExpansionPlayModeTests
{
    private static IEnumerator CaptureLargeSkillPage(string name, int width, int height)
    {
        using (var view = new PresentationView(width, height))
        {
            Component pager = Find("SkillPanelPager");
            Call(pager, "Refresh");

            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(pager, Is.Not.Null);
            Assert.That(FindAll("SkillSlotUI").Any(slot => slot.gameObject.activeInHierarchy), Is.True);

            Capture(name, width, height);
        }
    }

    private static void ClickLargeButton(Button button, bool enlargedArea = false)
    {
        Assert.That(button, Is.Not.Null);
        Assert.That(button.IsActive(), Is.True, button.name);
        Assert.That(button.IsInteractable(), Is.True, button.name);
        button.onClick.Invoke();
    }

    [UnityTest]
    public IEnumerator SkillPagesNavigateAndReopenWithoutDuplicatingButtons()
    {
        yield return LoadClass("Warrior");

        var panel = (GameObject)Get(Find("BattleManager"), "skillSelectionPanel");
        panel.SetActive(true);

        Component pager = Find("SkillPanelPager");
        Call(pager, "Bind", Find("PlayerManager"));

        var slots = ((IEnumerable)Get(pager, "slots")).Cast<Component>().ToArray();
        int[] originalSlotIds = slots.Select(slot => slot.GetInstanceID()).ToArray();

        Assert.That(slots.Length, Is.GreaterThan(0));
        Assert.That((int)Get(pager, "Page"), Is.Zero);
        Assert.That((int)Get(pager, "PageCount"), Is.GreaterThan(1));

        var nextButton = (Button)Get(pager, "nextButton");
        var previousButton = (Button)Get(pager, "previousButton");

        ClickLargeButton(nextButton);
        yield return null;
        Assert.That((int)Get(pager, "Page"), Is.EqualTo(1));

        ClickLargeButton(previousButton);
        yield return null;
        Assert.That((int)Get(pager, "Page"), Is.Zero);

        int initialButtonCount = panel.GetComponentsInChildren<Button>(true).Length;

        for (int i = 0; i < 3; i++)
        {
            panel.SetActive(false);
            panel.SetActive(true);
            yield return null;
        }

        int finalButtonCount = panel.GetComponentsInChildren<Button>(true).Length;
        Assert.That(finalButtonCount, Is.EqualTo(initialButtonCount));

        int[] finalSlotIds = ((IEnumerable)Get(pager, "slots"))
            .Cast<Component>()
            .Select(slot => slot.GetInstanceID())
            .ToArray();

        CollectionAssert.AreEqual(originalSlotIds, finalSlotIds);

        ClickLargeButton(nextButton);
        yield return null;
        Assert.That((int)Get(pager, "Page"), Is.EqualTo(1));
    }
}
#endif
