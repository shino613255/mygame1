#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public partial class ClassExpansionPlayModeTests
{
    private static void ValidateSkillLayout(string name, Camera camera, int width, int height)
    {
        var rectangles = new List<Rect>();
        foreach (Component slot in FindAll("SkillSlotUI").Where(s => s.gameObject.activeInHierarchy))
        {
            var label = (TMP_Text)Get(slot, "Label"); var cost = (TMP_Text)Get(slot, "CostLabel");
            label.ForceMeshUpdate(); cost.ForceMeshUpdate();
            string id = ((UnityEngine.Object)Get(slot, "Skill")).name;
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(30), id);
            Assert.That(label.fontSizeMin, Is.GreaterThanOrEqualTo(30), id);
            Assert.That(cost.fontSize, Is.GreaterThanOrEqualTo(30), id);
            foreach (var character in label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c => c.isVisible))
                Assert.That(character.pointSize, Is.GreaterThanOrEqualTo(30), id + " actual character size");
            Assert.That(label.textInfo.lineCount, Is.LessThanOrEqualTo(3), id + " " + name);
            Assert.That(label.isTextOverflowing, Is.False, id);
            Assert.That(label.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));
            Assert.That(label.maxVisibleLines, Is.EqualTo(int.MaxValue));
            var rect = (RectTransform)slot.transform;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = camera.WorldToScreenPoint(corners[0]), max = camera.WorldToScreenPoint(corners[2]);
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(620 * 1.5f));
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(300 * 1.5f));
            Assert.That(rect.localScale, Is.EqualTo(Vector3.one));
            Rect screen = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            Assert.That(rectangles.Any(r => r.Overlaps(screen)), Is.False, "Skill buttons overlap"); rectangles.Add(screen);
            AssertInside(ScreenRect(camera, label.rectTransform), screen, id + " name/description");
            AssertInside(ScreenRect(camera, cost.rectTransform), screen, id + " cost");
            File.AppendAllText(ArtifactFolder + "/ui-metrics.tsv", $"{name}\t{id}\tfont={label.fontSize}\tlines={label.textInfo.lineCount}\tbutton={rect.rect.size}\tcostfont={cost.fontSize}\n");
        }
        var pager = Find("SkillPanelPager");
        Rect viewport = ScreenRect(camera, ((UnityEngine.UI.ScrollRect)Get(pager, "scroll")).viewport);
        var controls = new List<Rect>();
        foreach (string control in new[] { "previousButton", "nextButton" })
        {
            var button = (Button)Get(Find("SkillPanelPager"), control);
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            Vector2 min = camera.WorldToScreenPoint(corners[0]), max = camera.WorldToScreenPoint(corners[2]);
            Rect screen = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            AssertInside(screen, new Rect(0, 0, width, height), control);
            Assert.That(screen.Overlaps(viewport), Is.False, control + " overlaps scroll area");
            Assert.That(controls.Any(r => r.Overlaps(screen)), Is.False); controls.Add(screen);
            var rect = (RectTransform)button.transform;
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(230 * 1.5f));
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(90 * 1.5f));
            Assert.That(button.GetComponentInChildren<TMP_Text>().fontSize, Is.GreaterThanOrEqualTo(45));
            File.AppendAllText(ArtifactFolder + "/navigation-metrics.tsv", $"{name}\t{control}\tsize={rect.rect.size}\tfont={button.GetComponentInChildren<TMP_Text>().fontSize}\n");
        }
        Rect page = ScreenRect(camera, ((TMP_Text)Get(pager, "pageLabel")).rectTransform);
        Assert.That(controls.Any(r => r.Overlaps(page)), Is.False, "Page number overlaps navigation");
        AssertInside(page, new Rect(0, 0, width, height), "Page number");
        Rect close = ScreenRect(camera, (RectTransform)pager.transform.Find("CloseSkills"));
        AssertInside(close, new Rect(0, 0, width, height), "Close button");
        Assert.That(close.Overlaps(page) || close.Overlaps(viewport), Is.False, "Header overlaps");
        AssertInside(viewport, new Rect(0, 0, width, height), "Viewport");
    }

    [UnityTest]
    public IEnumerator EveryRetainedSkillUsesOneVfxAndCleansUpWithoutChangingCombatCosts()
    {
        foreach (string role in new[] { "Warrior", "Mage", "Berserker" })
        {
            yield return LoadClass(role);
            using (var view = new PresentationView(1920, 1080))
            {
            Component player = Find("PlayerManager"), battle = Find("BattleManager");
            var skills = ((IEnumerable)Get(player, "Skills")).Cast<UnityEngine.Object>().ToArray();
            foreach (UnityEngine.Object skill in skills)
            {
                var data = AssetDatabase.LoadMainAssetAtPath("Assets/Script/Enemy/EnemyAI/Enemy Action/01_Tank.asset");
                GameObject root = UnityEngine.Object.Instantiate((GameObject)Get(data, "prefab"));
                Component enemy = root.GetComponent(Resolve("EnemyManager")); Call(enemy, "Setup", data);
                Call(battle, "Setup", enemy); yield return Ready(battle);
                Call(player.GetComponent(Resolve("SkillCooldowns")), "Clear");
                Set(player, "hp", (int)Get(player, "maxHp") / 2); Set(player, "mp", (int)Get(player, "maxMp") - 40);
                if (Get(skill, "id").ToString() == "Common_Antidote")
                {
                    var poison = AssetDatabase.LoadMainAssetAtPath("Assets/Skill Data/Skill_Poison.asset");
                    Call(player, "ApplyPoison", Get(poison, "statusEffect"));
                }
                var panel = (GameObject)Get(battle, "skillSelectionPanel"); panel.SetActive(true);
                Component pager = Find("SkillPanelPager"); Call(pager, "Bind", player);
                Component chosen = null;
                for (int page = 0; page < (int)Get(pager, "PageCount") && chosen == null; page++)
                {
                    chosen = FindAll("SkillSlotUI").FirstOrDefault(s => (UnityEngine.Object)Get(s, "Skill") == skill);
                    if (chosen == null) ((Button)Get(pager, "nextButton")).onClick.Invoke();
                }
                Assert.That(chosen, Is.Not.Null); panel.SetActive(false);
                int hp = (int)Get(player, "hp"), mp = (int)Get(player, "mp");
                int width = Camera.main.pixelWidth, height = Camera.main.pixelHeight;
                Color32[] before = Capture("vfx-" + role + "-" + skill.name + "-before", width, height);
                int cost = (int)Call(player, "GetSkillMpCost", skill);
                UnityEngine.Random.InitState(42);
                panel.SetActive(true); Canvas.ForceUpdateCanvases();
                ScrollToCard(pager, chosen); yield return null;
                ClickLargeButton(chosen.GetComponent<Button>(), true);
                bool attack = Get(skill, "targetType").ToString() != "Self";
                Component part = root.GetComponentsInChildren(Resolve("BodyPart")).First(p => Get(p, "partType").ToString() == "Face");
                if (attack)
                {
                    Assert.That((int)Get(player, "mp"), Is.EqualTo(mp));
                    Assert.That((int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount"), Is.Zero);
                    yield return Ready(battle); Call(battle, "OnBodyPartTapped", part);
                }
                Component controller = (Component)Get(battle, "SkillVfx");
                Assert.That((int)Get(controller, "ActiveEffectCount"), Is.EqualTo(1), skill.name);
                Transform effect = controller.transform.GetChild(controller.transform.childCount - 1);
                Vector3 expected = attack ? part.GetComponent<Collider2D>().bounds.center :
                    (Vector3)Call(Get(battle, "playerUI"), "GetSkillEffectPosition", Camera.main);
                Assert.That(Vector2.Distance(effect.position, expected + (Vector3)(Vector2)Get(skill, "effectOffset")), Is.LessThan(.01f));
                float sampleTime = Get(skill, "skillType").ToString() == "Physical" ? .12f : .3f;
                foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true)) ps.Simulate(sampleTime, false, true, true);
                Color32[] after = Capture("vfx-" + role + "-" + skill.name, width, height);
                Vector3 screen = Camera.main.WorldToViewportPoint(effect.position);
                int changes = 0;
                int cx = Mathf.RoundToInt(screen.x * width), cy = Mathf.RoundToInt(screen.y * height);
                for (int y = Mathf.Max(0, cy - 100); y < Mathf.Min(height, cy + 100); y++)
                for (int x = Mathf.Max(0, cx - 100); x < Mathf.Min(width, cx + 100); x++)
                {
                    Color32 a = before[y * width + x], b = after[y * width + x];
                    if (Mathf.Abs(a.r-b.r) + Mathf.Abs(a.g-b.g) + Mathf.Abs(a.b-b.b) > 30) changes++;
                }
                File.AppendAllText(ArtifactFolder + "/vfx-metrics.tsv", $"{role}\t{skill.name}\tposition={effect.position}\tchangedPixels={changes}\n");
                Assert.That(changes, Is.GreaterThan(20), skill.name + " must be visible near its target");
                int expectedMp = mp - cost + (Get(skill, "skillType").ToString() == "RecoverMp" ? (int)Get(skill, "power") : 0);
                Assert.That((int)Get(player, "mp"), Is.EqualTo(expectedMp));
                int expectedHp = hp;
                string skillId = Get(skill, "id").ToString();
                if (skillId == "Berserker_BloodPrice") expectedHp -= 150;
                if (skillId == "Berserker_WarCry") expectedHp += Mathf.RoundToInt((int)Get(player, "maxHp") * .15f);
                if (skillId == "Common_FirstAid") expectedHp += Mathf.RoundToInt((int)Get(player, "maxHp") * .12f);
                if (skillId == "None" && Get(skill, "skillType").ToString() == "Heal")
                    expectedHp += (int)Get(skill, "power") + Mathf.RoundToInt((int)Get(player, "mag") * (float)Get(skill, "multiplier"));
                Assert.That((int)Get(player, "hp"), Is.EqualTo(expectedHp));
                Assert.That((int)Call(player.GetComponent(Resolve("SkillCooldowns")), "Remaining", skill), Is.EqualTo((int)Get(skill, "cooldown")));
                int afterHp = (int)Get(player, "hp"), afterMp = (int)Get(player, "mp");
                ((MonoBehaviour)battle).StopAllCoroutines(); Set(battle, "isBattleRunning", false);
                yield return new WaitForSeconds((float)Get(skill, "effectDuration") + .15f);
                Assert.That((int)Get(controller, "ActiveEffectCount"), Is.Zero);
                Assert.That((int)Get(player, "hp"), Is.EqualTo(afterHp)); Assert.That((int)Get(player, "mp"), Is.EqualTo(afterMp));
                UnityEngine.Object.Destroy(root); yield return null;
            }
            }
        }
    }

    [UnityTest]
    public IEnumerator FailedSelectionMissAndUiClicksDoNotCreateImpactEffects()
    {
        yield return LoadClass("Berserker");
        Component player = Find("PlayerManager"), battle = Find("BattleManager");
        var data = AssetDatabase.LoadMainAssetAtPath("Assets/Script/Enemy/EnemyAI/Enemy Action/01_Tank.asset");
        GameObject root = UnityEngine.Object.Instantiate((GameObject)Get(data, "prefab"));
        Component enemy = root.GetComponent(Resolve("EnemyManager")); Call(enemy, "Setup", data); Call(battle, "Setup", enemy); yield return Ready(battle);
        var skills = ((IEnumerable)Get(player, "Skills")).Cast<UnityEngine.Object>().ToArray();
        var blood = skills.Single(s => Get(s, "id").ToString() == "Berserker_BloodPrice");
        var rend = skills.Single(s => Get(s, "id").ToString() == "Berserker_Rend");
        Set(player, "hp", 150); Call(battle, "OnSkillSelected", blood);
        Assert.That((int)Get(player, "hp"), Is.EqualTo(150)); yield return Ready(battle);
        Set(player, "mp", 0); Call(battle, "OnSkillSelected", rend); yield return Ready(battle);
        Assert.That((int)Get(player, "mp"), Is.Zero);
        Call(player, "RestoreHpAndMp"); Component cooldowns = player.GetComponent(Resolve("SkillCooldowns"));
        Call(cooldowns, "StartCooldown", rend); Call(battle, "OnSkillSelected", rend); yield return Ready(battle);
        Assert.That((int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount"), Is.Zero);
        Call(cooldowns, "Clear");
        var panel = (GameObject)Get(battle, "skillSelectionPanel"); panel.SetActive(true);
        int beforeHp = (int)Get(enemy, "hp");
        var slot = FindAll("SkillSlotUI").First(s => s.gameObject.activeInHierarchy);
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, slot.transform.position);
        Call(battle, "TryPickBodyPart", point);
        Assert.That((int)Get(enemy, "hp"), Is.EqualTo(beforeHp)); panel.SetActive(false);
        int mp = (int)Get(player, "mp"); Call(battle, "OnSkillSelected", rend); yield return Ready(battle);
        Assert.That((int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount"), Is.Zero);
        Set(enemy, "evasionRate", 1f);
        Component part = root.GetComponentsInChildren(Resolve("BodyPart")).First(); Call(battle, "OnBodyPartTapped", part);
        Assert.That((int)Get(player, "mp"), Is.EqualTo(mp - 4));
        Assert.That((int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount"), Is.Zero);
        ((MonoBehaviour)battle).StopAllCoroutines();
        Call(battle, "PlaySkillEffect", rend, Vector3.zero);
        Assert.That((int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount"), Is.EqualTo(1));
        Call(battle, "DebugKillEnemy"); yield return null;
        Assert.That((int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount"), Is.Zero);
    }

    [UnityTest]
    public IEnumerator SharedHealUsesTheEnemyPositionAndBattleEndRemovesEffects()
    {
        yield return LoadClass("Mage");
        Component battle = Find("BattleManager"), player = Find("PlayerManager");
        var data = AssetDatabase.LoadMainAssetAtPath("Assets/Script/Enemy/EnemyAI/Enemy Action/01_Tank.asset");
        GameObject root = UnityEngine.Object.Instantiate((GameObject)Get(data, "prefab"));
        Component enemy = root.GetComponent(Resolve("EnemyManager")); Call(enemy, "Setup", data); Call(battle, "Setup", enemy); yield return Ready(battle);
        ((MonoBehaviour)battle).StopAllCoroutines(); Set(enemy, "hp", 100);
        var heal = AssetDatabase.LoadMainAssetAtPath("Assets/Skill Data/Skill_Heal.asset");
        var cooldowns = enemy.GetComponent(Resolve("SkillCooldowns"));
        var dictionary = (IDictionary)Get(cooldowns, "cd");
        foreach (UnityEngine.Object skill in ((IEnumerable)Get(data, "skillList"))) if (skill != heal) dictionary[skill] = 99;
        Component ai = (Component)Get(battle, "enemyAI"); int seed = -1;
        for (int i = 0; i < 100 && seed < 0; i++)
        {
            UnityEngine.Random.InitState(i);
            if ((UnityEngine.Object)Call(ai, "ChooseSkill", enemy, 0, player, false) == heal) seed = i;
        }
        Assert.That(seed, Is.GreaterThanOrEqualTo(0)); UnityEngine.Random.InitState(seed);
        ((MonoBehaviour)battle).StartCoroutine((IEnumerator)Call(battle, "EnemyActAuto"));
        yield return Until(() => (int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount") > 0);
        ((MonoBehaviour)battle).StopAllCoroutines();
        Assert.That((int)Get(enemy, "hp"), Is.EqualTo(180));
        Component controller = (Component)Get(battle, "SkillVfx"); Transform effect = controller.transform.GetChild(controller.transform.childCount - 1);
        Assert.That(Vector2.Distance(effect.position, root.transform.position), Is.LessThan(.01f));
        foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true)) ps.Simulate(.3f, false, true, true);
        Capture("vfx-shared-enemy-heal", Camera.main.pixelWidth, Camera.main.pixelHeight);
        Call(battle, "DebugKillEnemy"); yield return null;
        Assert.That((int)Get(controller, "ActiveEffectCount"), Is.Zero);
    }

    private sealed class PresentationView : IDisposable
    {
        private readonly Camera camera;
        private readonly RenderTexture target;
        private readonly RenderTexture previous;
        private readonly (Canvas canvas, RenderMode mode, Camera camera, float distance)[] states;

        public PresentationView(int width, int height)
        {
            camera = Camera.main; previous = camera.targetTexture;
            target = new RenderTexture(width, height, 24); camera.targetTexture = target;
            states = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.isRootCanvas)
                .Select(c => (c, c.renderMode, c.worldCamera, c.planeDistance)).ToArray();
            foreach (var state in states)
            {
                state.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                state.canvas.worldCamera = camera; state.canvas.planeDistance = 5;
            }
            Canvas.ForceUpdateCanvases();
        }

        public void Dispose()
        {
            foreach (var state in states)
            {
                if (state.canvas == null) continue;
                state.canvas.renderMode = state.mode; state.canvas.worldCamera = state.camera; state.canvas.planeDistance = state.distance;
            }
            camera.targetTexture = previous; target.Release(); UnityEngine.Object.Destroy(target);
        }
    }
}
#endif
