#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public partial class ClassExpansionPlayModeTests
{
    private static string ArtifactFolder => Environment.GetEnvironmentVariable("BREAKTACTICS_TEST_OUTPUT") ?? "Logs/SkillUiLarge";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        foreach (string name in new[] { "PlayerSelectionManager", "DialogTextManager", "SoundManager", "FadeIOManager" })
            foreach (Component c in FindAll(name)) UnityEngine.Object.Destroy(c.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TownSelectionAndEverySkillPageWorkForAllClasses()
    {
        foreach (string role in new[] { "Warrior", "Mage", "Berserker" })
        {
            yield return LoadClass(role);
            Component player = Find("PlayerManager");
            Assert.That(Get(player, "Role").ToString(), Is.EqualTo(role));
            var skills = ((IEnumerable)Get(player, "Skills")).Cast<UnityEngine.Object>().ToArray();
            ((GameObject)Get(Find("BattleManager"), "skillSelectionPanel")).SetActive(true);
            Component pager = Find("SkillPanelPager"); Call(pager, "Bind", player);
            var reached = new HashSet<UnityEngine.Object>();
            int count = (int)Get(pager, "PageCount");
            for (int page = 0; page < count; page++)
            {
                yield return null;
                foreach (Component slot in FindAll("SkillSlotUI").Where(s => s.gameObject.activeInHierarchy))
                {
                    var skill = (UnityEngine.Object)Get(slot, "Skill");
                    if (skill == null) continue;
                    reached.Add(skill);
                    string text = slot.GetComponentInChildren<TMP_Text>().text;
                    StringAssert.Contains((string)Get(skill, "skillName"), text);
                    string cost = ((TMP_Text)Get(slot, "CostLabel")).text;
                    StringAssert.Contains("MP", cost); StringAssert.Contains("CD", cost);
                    StringAssert.Contains((string)Get(skill, "description"), text);
                }
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720),
                    new Vector2Int(960, 600), new Vector2Int(800, 600), new Vector2Int(900, 1200) })
                    yield return CaptureLargeSkillPage($"ui-{role}-{size.x}x{size.y}-page-{page + 1}", size.x, size.y);
                if (page + 1 < count) ((Button)Get(pager, "nextButton")).onClick.Invoke();
            }
            CollectionAssert.AreEquivalent(skills, reached);
            Assert.That(reached.Count, Is.EqualTo(6));
        }
    }

    [UnityTest]
    public IEnumerator ThreeClassesAttackAllEightEnemiesWithoutDoubleSpending()
    {
        string[] paths = {
            "Assets/Script/Enemy/EnemyAI/Enemy Action/00_Attacker.asset", "Assets/Script/Enemy/EnemyAI/Enemy Action/01_Tank.asset",
            "Assets/Script/Enemy/EnemyAI/Enemy Action/02_Ninja.asset", "Assets/Script/Enemy/EnemyAI/Enemy Action/03_Elite Attacker.asset",
            "Assets/Script/Enemy/EnemyAI/Enemy Action/04_Tank Boss.asset", "Assets/Script/Enemy/Expansion/Auditor.asset",
            "Assets/Script/Enemy/Expansion/Raider.asset", "Assets/Script/Enemy/Expansion/Pilgrim.asset" };
        foreach (string role in new[] { "Warrior", "Mage", "Berserker" })
        {
            yield return LoadClass(role);
            Component player = Find("PlayerManager"), battle = Find("BattleManager");
            foreach (string path in paths)
            {
                var data = AssetDatabase.LoadMainAssetAtPath(path);
                GameObject root = UnityEngine.Object.Instantiate((GameObject)Get(data, "prefab"));
                Component enemy = root.GetComponent(Resolve("EnemyManager")); Call(enemy, "Setup", data);
                Call(player, "RestoreHpAndMp"); Call(player.GetComponent(Resolve("SkillCooldowns")), "Clear");
                Call(battle, "Setup", enemy); yield return Ready(battle);
                var skill = ((IEnumerable)Get(player, "Skills")).Cast<UnityEngine.Object>().First(s => Get(s, "id").ToString() ==
                    (role == "Warrior" ? "Warrior_BreakSlash" : role == "Mage" ? "Mage_FlamePiercer" : "Berserker_Rend"));
                ((GameObject)Get(battle, "skillSelectionPanel")).SetActive(true);
                Component pager = Find("SkillPanelPager"); Call(pager, "Bind", player);
                Component chosen = null;
                for (int page = 0; page < (int)Get(pager, "PageCount") && chosen == null; page++)
                {
                    chosen = FindAll("SkillSlotUI").FirstOrDefault(s => (UnityEngine.Object)Get(s, "Skill") == skill);
                    if (chosen == null) ((Button)Get(pager, "nextButton")).onClick.Invoke();
                }
                Assert.That(chosen, Is.Not.Null); chosen.GetComponent<Button>().onClick.Invoke(); yield return Ready(battle);
                Component part = root.GetComponentsInChildren(Resolve("BodyPart")).First(p => Get(p, "partType").ToString() == "Face");
                int cost = (int)Call(player, "GetSkillMpCost", skill);
                int mp = (int)Get(player, "mp"); Call(battle, "OnBodyPartTapped", part);
                int after = (int)Get(player, "mp");
                Assert.That(mp - after, Is.EqualTo(cost));
                Call(battle, "OnBodyPartTapped", part); Assert.That((int)Get(player, "mp"), Is.EqualTo(after));
                ((MonoBehaviour)battle).StopAllCoroutines(); Set(battle, "isBattleRunning", false);
                UnityEngine.Object.Destroy(root); yield return null;
            }
        }
    }

    [UnityTest]
    public IEnumerator NewEnemySceneAdvancesFramesRendersAndSupportsAllPartClicks()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/EnemyValidation.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        Component harness = Find("EnemyValidationHarness"); int frame = Time.frameCount;
        for (int index = 0; index < 3; index++)
        {
            Call(harness, "ShowEnemy", index); yield return null; yield return null;
            Component enemy = Find("EnemyManager"); Physics2D.SyncTransforms();
            foreach (Component part in enemy.GetComponentsInChildren(Resolve("BodyPart")))
            {
                var collider = part.GetComponent<PolygonCollider2D>(); bool clicked = false;
                for (int x = 1; x < 10 && !clicked; x++)
                for (int y = 1; y < 10 && !clicked; y++)
                    clicked = Physics2D.OverlapPoint(new Vector2(Mathf.Lerp(collider.bounds.min.x, collider.bounds.max.x, x / 10f),
                        Mathf.Lerp(collider.bounds.min.y, collider.bounds.max.y, y / 10f))) == collider;
                Assert.That(clicked, Is.True, part.name);
                Assert.That(part.GetComponentsInChildren<LineRenderer>().Any(l => l.enabled), Is.True);
            }
            Capture("new-enemy-playmode-" + index);
        }
        Assert.That(Time.frameCount, Is.GreaterThan(frame + 3));
    }

    [UnityTest]
    public IEnumerator GrowthAdvancesOnceBossRecoversAllClassesAndNewQuestResetsRewards()
    {
        foreach (string role in new[] { "Warrior", "Mage", "Berserker" })
        {
            yield return LoadClass(role);
            Component player = Find("PlayerManager"), battle = Find("BattleManager"), quest = Find("QuestManager");
            object life = Enum.Parse(Resolve("GrowthRewardId"), "LifeDrain"); Call(player, "TryAcquireGrowthReward", life);
            Call(quest, "OnNextButton"); yield return Until(() => (bool)Get(battle, "isBattleRunning"));
            Call(battle, "DebugKillEnemy"); Component growth = FindAll("GrowthSelectionManager").Single();
            yield return Until(() => (bool)Get(growth, "selectionOpen"));
            object choice = ((IEnumerable)Get(growth, "CurrentOffers")).Cast<object>().First(o =>
                new[] { "MaxHpUp", "AttackUp", "MagicAttackUp", "DefenseUp", "MaxMpUp" }.Contains(o.ToString()));
            int floor = (int)Get(quest, "currentFloorIndex"); Call(growth, "SelectReward", choice); Call(growth, "SelectReward", choice);
            Assert.That((int)Get(quest, "currentFloorIndex"), Is.EqualTo(floor + 1));
            yield return Until(() => (bool)Get(battle, "isBattleRunning")); Assert.That((bool)Call(player, "HasGrowthReward", life), Is.True);
            ((MonoBehaviour)battle).StopAllCoroutines(); Set(battle, "isBattleRunning", false);
            UnityEngine.Object.Destroy(((Component)Get(battle, "enemy")).gameObject); yield return null;
            var data = AssetDatabase.LoadMainAssetAtPath("Assets/Script/Enemy/EnemyAI/Enemy Action/04_Tank Boss.asset");
            GameObject root = UnityEngine.Object.Instantiate((GameObject)Get(data, "prefab"));
            Component boss = root.GetComponent(Resolve("EnemyManager")); Call(boss, "Setup", data);
            Set(player, "hp", 1); Set(player, "mp", 0); Call(battle, "Setup", boss);
            Assert.That(Get(player, "hp"), Is.EqualTo(Get(player, "maxHp"))); Assert.That(Get(player, "mp"), Is.EqualTo(Get(player, "maxMp")));
            Call(player, "Setup", Get(player, "Data")); Assert.That((bool)Call(player, "HasGrowthReward", life), Is.False);
        }
    }

    [UnityTest]
    public IEnumerator CounterDeathReturnsToTownAndNewQuestClearsRewards()
    {
        yield return LoadClass("Berserker");
        Component player = Find("PlayerManager"), battle = Find("BattleManager");
        object reward = Enum.Parse(Resolve("GrowthRewardId"), "LifeDrain");
        Call(player, "TryAcquireGrowthReward", reward);
        var data = AssetDatabase.LoadMainAssetAtPath("Assets/Script/Enemy/EnemyAI/Enemy Action/01_Tank.asset");
        GameObject root = UnityEngine.Object.Instantiate((GameObject)Get(data, "prefab"));
        Component enemy = root.GetComponent(Resolve("EnemyManager")); Call(enemy, "Setup", data);
        Call(battle, "Setup", enemy); yield return Ready(battle);
        Set(player, "hp", 1); Call(enemy, "ActivateCounter");
        Component belly = root.GetComponentsInChildren(Resolve("BodyPart")).Single(p => Get(p, "partType").ToString() == "Belly");
        Call(battle, "OnBodyPartTapped", belly);
        Assert.That((int)Get(player, "hp"), Is.Zero, "Counter must bypass guards and cause defeat");
        yield return Until(() => SceneManager.GetActiveScene().name == "Town");
        yield return null;
        Component select = FindAll("PlayerSelectButton").Single(b => Get(Get(b, "playerData"), "role").ToString() == "Berserker");
        select.GetComponent<Button>().onClick.Invoke();
        yield return Until(() => SceneManager.GetActiveScene().name == "Quest"); yield return null;
        player = Find("PlayerManager");
        Assert.That((bool)Call(player, "HasGrowthReward", reward), Is.False);
        Assert.That(Get(player, "hp"), Is.EqualTo(Get(player, "maxHp")));
    }

    private static IEnumerator LoadClass(string role)
    {
        Time.timeScale = 20; SceneManager.LoadScene("Town"); yield return null; yield return null;
        Set(Find("FadeIOManager"), "fadeDuration", .01f); Set(Find("DialogTextManager"), "intervalForCharacterDisplay", .001f);
        if (role == "Warrior") { yield return null; Capture("town-three-classes"); }
        Component button = FindAll("PlayerSelectButton").Single(b => Get(Get(b, "playerData"), "role").ToString() == role);
        button.GetComponent<Button>().onClick.Invoke(); yield return Until(() => SceneManager.GetActiveScene().name == "Quest");
        yield return null; yield return null; Call(Find("DialogTextManager"), "SetScenarios", (object)new[] { "" }); yield return null;
    }

    private static IEnumerator Ready(Component battle) => Until(() => (bool)Call(battle, "CanReceivePlayerInput"));
    private static IEnumerator Until(Func<bool> condition)
    {
        float limit = Time.realtimeSinceStartup + 12;
        while (!condition() && Time.realtimeSinceStartup < limit) yield return null;
        Assert.That(condition(), Is.True, "Timed out waiting for runtime state");
    }
    private static Color32[] Capture(string name, int width = 960, int height = 720, bool validateSkills = false)
    {
        Directory.CreateDirectory(ArtifactFolder);
        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null);
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas).Select(c => new { canvas = c, mode = c.renderMode, camera = c.worldCamera, distance = c.planeDistance }).ToArray();
        var target = new RenderTexture(width, height, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            foreach (var state in canvases)
            {
                state.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                state.canvas.worldCamera = camera; state.canvas.planeDistance = 5f;
            }
            Canvas.ForceUpdateCanvases();
            if (validateSkills)
            {
                Component pager = Find("SkillPanelPager");

                Call(pager, "Refresh");

                Canvas.ForceUpdateCanvases();
            }
            if (validateSkills)
            {
                Component pager = Find("SkillPanelPager");

                Call(pager, "Refresh");

                Canvas.ForceUpdateCanvases();
            }
            camera.Render(); RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Assert.That(image.GetPixels32().Where((p, i) => i % 100 == 0).Distinct().Count(), Is.GreaterThan(20), "Rendering must contain more than a flat background");
            File.WriteAllBytes(ArtifactFolder + "/" + name + ".png", image.EncodeToPNG());
            return image.GetPixels32();
        }
        finally
        {
            foreach (var state in canvases)
            {
                state.canvas.renderMode = state.mode; state.canvas.worldCamera = state.camera; state.canvas.planeDistance = state.distance;
            }
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            if (image != null) UnityEngine.Object.Destroy(image);
            target.Release(); UnityEngine.Object.Destroy(target);
        }
    }
    private static Type Resolve(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static Component[] FindAll(string name) => UnityEngine.Object.FindObjectsByType(Resolve(name), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().ToArray();
    private static Component Find(string name) => FindAll(name).First(c => c.gameObject.activeInHierarchy);
    private static object Get(object o, string field) => o.GetType().GetField(field, Flags)?.GetValue(o) ?? o.GetType().GetProperty(field, Flags)?.GetValue(o);
    private static void Set(object o, string field, object value) => o.GetType().GetField(field, Flags).SetValue(o, value);
    private static object Call(object o, string method, params object[] args) => o.GetType().GetMethods(Flags).First(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(o, args);
}
#endif
