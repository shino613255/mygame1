using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ExpansionEnemyTests
{
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

    [TearDown]
    public void Cleanup()
    {
        for (int i = created.Count - 1; i >= 0; i--)
            if (created[i] != null) UnityEngine.Object.DestroyImmediate(created[i]);
        created.Clear();
    }

    [TestCase("Auditor", 440, 80, 10, 26, 12, 28)]
    [TestCase("Raider", 700, 45, 24, 0, 8, 8)]
    [TestCase("Pilgrim", 560, 90, 8, 18, 20, 25)]
    public void EnemyProfilesAndAllFiveColliderRegionsAreValid(string id, int hp, int mp, int at, int mag, int def, int mdef)
    {
        EnemyManager enemy = Enemy(id);
        Assert.That(new[] { enemy.hp, enemy.mp, enemy.at, enemy.mag, enemy.def, enemy.mdef }, Is.EqualTo(new[] { hp, mp, at, mag, def, mdef }));
        BodyPart[] parts = enemy.GetComponentsInChildren<BodyPart>();
        Assert.That(parts.Length, Is.EqualTo(5));
        Assert.That(parts.Single(p => p.partType == PartType.Belly).canBreak, Is.False);
        Physics2D.SyncTransforms();
        foreach (BodyPart part in parts)
        {
            var collider = part.GetComponent<PolygonCollider2D>();
            bool found = false;
            Bounds bounds = collider.bounds;
            for (int x = 1; x < 10 && !found; x++)
            for (int y = 1; y < 10 && !found; y++)
            {
                Vector2 position = new Vector2(Mathf.Lerp(bounds.min.x, bounds.max.x, x / 10f), Mathf.Lerp(bounds.min.y, bounds.max.y, y / 10f));
                var hits = Physics2D.OverlapPointAll(position);
                found = hits.Length == 1 && hits[0] == collider;
            }
            Assert.That(found, Is.True, id + " " + part.name + " needs an unambiguous clickable region");
        }
        enemy.SetPartViewVisible(true);
        Assert.That(parts.All(p => p.GetComponentsInChildren<LineRenderer>().All(l => l.enabled)), Is.True);
    }

    [Test]
    public void AuditorUsesLevyThenChargeThenBoltAndFallsBackWithoutMp()
    {
        var enemy = Enemy("Auditor"); var player = Player();
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Auditor_ManaLevy));
        player.mp = 0;
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Auditor_ChargedLance));
        enemy.GetComponent<SkillCooldowns>().StartCooldown(Skill(SkillId.Auditor_ChargedLance));
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Auditor_ArcaneBolt));
        enemy.mp = 0; Assert.That(Choose(enemy, player), Is.Null);
    }

    [Test]
    public void RaiderUsesChargeThenFuryThenStandardAndCannotSuicide()
    {
        var enemy = Enemy("Raider"); var player = Player();
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Raider_ChargedAxe));
        enemy.GetComponent<SkillCooldowns>().StartCooldown(Skill(SkillId.Raider_ChargedAxe));
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Raider_BloodFury));
        enemy.hp = 56;
        Assert.That(SkillExecutor.Execute(enemy, enemy, Skill(SkillId.Raider_BloodFury)).executed, Is.False);
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Raider_CleavingBlow));
        enemy.hp = 57;
        Assert.That(SkillExecutor.Execute(enemy, enemy, Skill(SkillId.Raider_BloodFury)).executed, Is.True);
        Assert.That(enemy.hp, Is.EqualTo(1)); Assert.That(enemy.BattleEffects.EmpoweredAttacks, Is.EqualTo(2));
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Raider_CleavingBlow));
    }

    [Test]
    public void PilgrimPrioritizesHealingWardAndAttackWithFiniteResources()
    {
        var enemy = Enemy("Pilgrim"); var player = Player();
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Pilgrim_TalismanWard));
        enemy.hp = 300;
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Pilgrim_Renewal));
        var result = SkillExecutor.Execute(enemy, enemy, Skill(SkillId.Pilgrim_Renewal));
        Assert.That(result.value, Is.EqualTo(84)); Assert.That(enemy.mp, Is.EqualTo(78));
        SkillExecutor.Execute(enemy, enemy, Skill(SkillId.Pilgrim_TalismanWard));
        Assert.That(Choose(enemy, player).id, Is.EqualTo(SkillId.Pilgrim_TalismanBolt));
        enemy.mp = 0; Assert.That(Choose(enemy, player), Is.Null);
    }

    [TestCase("Auditor", SkillId.Auditor_ManaLevy, SkillId.Auditor_ChargedLance, 1)]
    [TestCase("Raider", SkillId.Raider_BloodFury, SkillId.Raider_ChargedAxe, 2)]
    [TestCase("Pilgrim", SkillId.Pilgrim_Renewal, SkillId.Pilgrim_TalismanWard, 1)]
    public void FaceAndHandsSealOnlySpecifiedSkills(string id, SkillId faceSkill, SkillId handSkill, int handCount)
    {
        EnemyManager enemy = Enemy(id);
        BodyPart face = enemy.GetComponentsInChildren<BodyPart>().Single(p => p.partType == PartType.Face);
        face.TakePartDamage(10000);
        Assert.That(ExpansionEnemyAction.IsSealed(enemy, Skill(faceSkill)), Is.True);
        BodyPart[] hands = enemy.GetComponentsInChildren<BodyPart>().Where(p => p.partType == PartType.Hand).ToArray();
        hands[0].TakePartDamage(10000);
        Assert.That(ExpansionEnemyAction.IsSealed(enemy, Skill(handSkill)), Is.EqualTo(handCount == 1));
        if (handCount == 2) { hands[1].TakePartDamage(10000); Assert.That(ExpansionEnemyAction.IsSealed(enemy, Skill(handSkill)), Is.True); }
    }

    [TestCase("Auditor")]
    [TestCase("Raider")]
    public void ChargedAttackIsReservedInterruptedAndConsumedOnlyOnce(string id)
    {
        EnemyManager enemy = Enemy(id);
        GameObject go = Go("Battle"); go.AddComponent<EnemyAI>(); var battle = go.AddComponent<BattleManager>();
        Set(battle, "enemy", enemy);
        Invoke(battle, "StartEnemyCharge", enemy.data.telegraphedSkill, 1);
        object plan = Get(battle, "enemyActionPlan");
        plan.GetType().GetField("isTelegraphed").SetValue(plan, true); Set(battle, "enemyActionPlan", plan);
        int mp = enemy.mp;
        BodyPart hand = enemy.GetComponentsInChildren<BodyPart>().First(p => p.partType == PartType.Hand);
        hand.TakePartDamage(10000);
        Assert.That(Invoke(battle, "TryInterruptEnemyAction", hand, true), Is.True);
        Assert.That(Invoke(battle, "TryInterruptEnemyAction", hand, true), Is.False);
        object[] output = { null, false };
        Assert.That(Invoke(battle, "TryConsumeEnemyAction", output), Is.True);
        Assert.That(output[0], Is.EqualTo(enemy.data.telegraphedSkill)); Assert.That(output[1], Is.True);
        Assert.That(Invoke(battle, "TryConsumeEnemyAction", output), Is.False);
        Assert.That(enemy.mp, Is.EqualTo(mp));
    }

    [Test]
    public void SealingDoesNotRemoveAlreadyGrantedWardOrFury()
    {
        var pilgrim = Enemy("Pilgrim"); pilgrim.BattleEffects.GrantWard();
        pilgrim.GetComponentsInChildren<BodyPart>().First(p => p.partType == PartType.Hand).TakePartDamage(10000);
        Assert.That(pilgrim.BattleEffects.HasWard, Is.True);
        pilgrim.BattleEffects.EndPlayerTurn(); Assert.That(pilgrim.BattleEffects.HasWard, Is.False);
        var raider = Enemy("Raider"); raider.BattleEffects.GrantBloodFury();
        raider.GetComponentsInChildren<BodyPart>().Single(p => p.partType == PartType.Face).TakePartDamage(10000);
        Assert.That(raider.BattleEffects.EmpoweredAttacks, Is.EqualTo(2));
    }

    [Test]
    public void LegacyKingKeepsPoisonThresholdsHeavyIntentAndPartSeals()
    {
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Script/Enemy/EnemyAI/Enemy Action/04_Tank Boss.asset");
        GameObject root = UnityEngine.Object.Instantiate(data.prefab); created.Add(root);
        var enemy = root.GetComponent<EnemyManager>(); enemy.Setup(data);
        foreach (BodyPart part in root.GetComponentsInChildren<BodyPart>()) Invoke(part, "Awake");
        var player = Player();
        var poison = data.skillList.Single(s => s.statusEffect != null && s.statusEffect.type == StatusEffectType.Poison);
        var action = new TankBossRoleAction();
        enemy.hp = Mathf.FloorToInt(enemy.maxHp * .75f) + 1;
        Assert.That(action.ChooseSkill(enemy, new List<SkillData> { poison }, player), Is.Null);
        enemy.hp = Mathf.FloorToInt(enemy.maxHp * .75f);
        Assert.That(action.ChooseSkill(enemy, new List<SkillData> { poison }, player), Is.EqualTo(poison));
        action.ResetState(); enemy.hp = Mathf.FloorToInt(enemy.maxHp * .5f);
        Assert.That(action.ChooseSkill(enemy, new List<SkillData> { poison }, player), Is.EqualTo(poison));
        action.ResetState(); enemy.hp = Mathf.FloorToInt(enemy.maxHp * .35f);
        Assert.That(action.ChooseSkill(enemy, new List<SkillData> { data.telegraphedSkill }, player, true), Is.Null);
        Assert.That(action.IsHeavyAttackUnlocked, Is.True);
        Assert.That(action.TakePendingHeavyAttack(enemy), Is.EqualTo(data.telegraphedSkill));
        Assert.That(action.TakePendingHeavyAttack(enemy), Is.Null);
        Assert.That(data.telegraphedSkill.power, Is.EqualTo(300));
        foreach (BodyPart part in root.GetComponentsInChildren<BodyPart>().Where(p => p.partType == PartType.Hand))
            part.TakePartDamage(10000);
        var sealedSkills = TankBossRoleAction.GetSealedSkills(enemy);
        CollectionAssert.Contains(sealedSkills, poison);
        CollectionAssert.Contains(sealedSkills, data.telegraphedSkill);
    }

    private EnemyManager Enemy(string id)
    {
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(ClassExpansionAssets.EnemyFolder + "/" + id + ".asset");
        Assert.That(data, Is.Not.Null);
        GameObject root = UnityEngine.Object.Instantiate(data.prefab); created.Add(root);
        var enemy = root.GetComponent<EnemyManager>(); enemy.Setup(data);
        foreach (BodyPart part in root.GetComponentsInChildren<BodyPart>()) Invoke(part, "Awake");
        Invoke(root.GetComponent<EnemyPartsController>(), "Awake");
        return enemy;
    }
    private PlayerManager Player() { var p = Go("Player").AddComponent<PlayerManager>(); p.Setup(AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Script/Player Data/Mage.asset")); return p; }
    private SkillData Choose(EnemyManager enemy, PlayerManager player)
    {
        var ai = Go("AI").AddComponent<EnemyAI>(); Invoke(ai, "Awake"); return ai.ChooseSkill(enemy, 0, player);
    }
    private static SkillData Skill(SkillId id) => ClassExpansionAssets.LoadSkill(id);
    private GameObject Go(string name) { var go = new GameObject(name); created.Add(go); return go; }
    private static object Invoke(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    private static object Get(object owner, string field) => owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
}
