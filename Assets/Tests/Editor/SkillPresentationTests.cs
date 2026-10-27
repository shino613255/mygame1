using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SkillPresentationTests
{
    [TestCase(PlayerRole.Warrior)]
    [TestCase(PlayerRole.Mage)]
    [TestCase(PlayerRole.Berserker)]
    public void InitialRuntimeAndUiLoadoutsAgreeAndRemovedSkillsCannotExecute(PlayerRole role)
    {
        var data = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Script/Player Data/" + role + ".asset");
        CollectionAssert.AreEqual(SkillPresentationAssets.Loadout(role), data.startSkills);
        var go = new GameObject("Player"); var target = new GameObject("Target");
        try
        {
            var player = go.AddComponent<PlayerManager>(); player.Setup(data);
            var enemy = target.AddComponent<EnemyManager>(); enemy.hp = 100;
            Assert.That(player.Skills.Count, Is.EqualTo(6));
            foreach (string guid in AssetDatabase.FindAssets("t:SkillData"))
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guid));
                if (player.HasSkill(skill)) continue;
                int hp = player.hp, mp = player.mp;
                Assert.That(SkillExecutor.Execute(player, skill.targetType == TargetType.Self ? player : enemy, skill).executed, Is.False, skill.name);
                Assert.That(player.hp, Is.EqualTo(hp)); Assert.That(player.mp, Is.EqualTo(mp));
                player.AddSkill(skill); Assert.That(player.Skills.Count, Is.EqualTo(6));
            }
            player.Setup(data); Assert.That(player.Skills.Count, Is.EqualTo(6));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(target); }
    }

    [Test]
    public void OnlyDescriptionsAndPresentationFieldsChangeOnSkillAssets()
    {
        if (!Directory.Exists("Logs/SkillPresentation/baseline/Assets/Skill Data"))
            Assert.Ignore("This local preservation check requires the pre-change asset snapshot.");
        string[] fields = { "mpCost", "cooldown", "power", "multiplier", "accuracy", "canCrit", "critBonus", "element",
            "targetType", "canTargetPart", "mainDamageRate", "partDamageRate", "statusEffect", "buff", "debuff", "applyChance" };
        foreach (string guid in AssetDatabase.FindAssets("t:SkillData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string beforePath = "Logs/SkillPresentation/baseline/" + path;
            if (!File.Exists(beforePath)) continue;
            string[] before = File.ReadAllLines(beforePath), after = File.ReadAllLines(path);
            foreach (string field in fields)
            {
                string prefix = "  " + field + ":";
                Assert.That(after.SingleOrDefault(l => l.StartsWith(prefix)), Is.EqualTo(before.SingleOrDefault(l => l.StartsWith(prefix))), path + " " + field);
            }
        }
    }

    [Test]
    public void EveryRetainedSkillHasValidEffectAndReadableDescription()
    {
        foreach (var skill in Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().SelectMany(SkillPresentationAssets.Loadout).Distinct())
        {
            Assert.That(skill.description, Is.Not.Empty); Assert.That(skill.effectPrefab, Is.Not.Null, skill.name);
            Assert.That(skill.effectDuration, Is.InRange(.1f, 2f));
            Assert.That(skill.effectPrefab.GetComponentsInChildren<ParticleSystem>(true).Length, Is.GreaterThan(0));
            foreach (Renderer renderer in skill.effectPrefab.GetComponentsInChildren<Renderer>(true))
                Assert.That(renderer.sharedMaterial, Is.Not.Null, skill.name + " " + renderer.name);
        }
    }
}
