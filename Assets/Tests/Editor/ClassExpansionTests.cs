using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ClassExpansionTests
{
    private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }

    [TestCase("Warrior", 1000, 100, 55, 40, 4, 8, 8)]
    [TestCase("Mage", 800, 150, 30, 80, 0, 20, 12)]
    [TestCase("Berserker", 1500, 60, 95, 10, 0, 0, 8)]
    public void ProfilesInitializeMatchingStatsAndAllSkills(string name, int hp, int mp, int at, int mag, int def, int mdef, int count)
    {
        PlayerManager player = Player(name);
        Assert.That(new[] { player.maxHp, player.maxMp, player.at, player.mag, player.def, player.mdef },
            Is.EqualTo(new[] { hp, mp, at, mag, def, mdef }));
        Assert.That(player.Skills.Count, Is.EqualTo(count));
        Assert.That(player.Skills.All(s => SkillUseRules.IsAllowedForRole(s, player.Role)), Is.True);
        Assert.That(player.Skills.Distinct().Count(), Is.EqualTo(count));
        Assert.That(player.Data.portrait, Is.Not.Null);
    }

    [TestCase("Warrior", GrowthRewardId.AttackUp, GrowthRewardId.DefenseUp)]
    [TestCase("Mage", GrowthRewardId.MagicAttackUp, GrowthRewardId.DefenseUp)]
    [TestCase("Berserker", GrowthRewardId.AttackUp, GrowthRewardId.MaxMpUp)]
    public void ClassGrowthHasExactlyThreeUsefulStatusChoices(string name, GrowthRewardId attack, GrowthRewardId third)
    {
        PlayerManager player = Player(name);
        player.TryAcquireGrowthReward(GrowthRewardId.LifeDrain);
        player.TryAcquireGrowthReward(GrowthRewardId.FocusedAttack);
        player.TryAcquireGrowthReward(GrowthRewardId.PrecisionShot);
        CollectionAssert.AreEquivalent(new[] { GrowthRewardId.MaxHpUp, attack, third },
            GrowthSelectionManager.GetEligibleRewardIds(player));
        int mp = player.mp = 7;
        int maxMp = player.maxMp;
        player.EnhanceMaxMpRate(1.2f);
        Assert.That(player.mp, Is.EqualTo(mp + Mathf.RoundToInt(maxMp * 1.2f) - maxMp));
    }

    [Test]
    public void WarriorLearnsEachKindOnlyOnceAndBattleResetPreservesQuestRewards()
    {
        PlayerManager player = Player("Warrior");
        BodyPart face = Part(PartType.Face);
        BodyPart hand = Part(PartType.Hand);
        BodyPart otherHand = Part(PartType.Hand);
        BodyPart leg = Part(PartType.Leg);
        BodyPart belly = Part(PartType.Belly, false);
        Assert.That(player.BattleEffects.LearnTechnique(player.Role, face), Is.False);
        foreach (BodyPart part in new[] { face, hand, otherHand, leg, belly }) part.TakePartDamage(1000);
        Assert.That(player.BattleEffects.LearnTechnique(player.Role, face), Is.True);
        Assert.That(player.BattleEffects.LearnTechnique(player.Role, hand), Is.True);
        Assert.That(player.BattleEffects.LearnTechnique(player.Role, otherHand), Is.False);
        Assert.That(player.BattleEffects.LearnTechnique(player.Role, leg), Is.True);
        Assert.That(player.BattleEffects.LearnTechnique(player.Role, belly), Is.False);
        Assert.That(player.BattleEffects.TechniqueCount, Is.EqualTo(3));
        Assert.That(player.BattleEffects.GetMpCost(14), Is.EqualTo(12));
        Assert.That(player.BattleEffects.GetMpCost(1), Is.EqualTo(1));
        Assert.That(player.BattleEffects.GetMpCost(0), Is.Zero);
        player.TryAcquireGrowthReward(GrowthRewardId.LifeDrain);
        player.TryAcquireGrowthReward(GrowthRewardId.FocusedAttack);
        player.ResetBattleState();
        Assert.That(player.BattleEffects.TechniqueCount, Is.Zero);
        Assert.That(player.HasLifeDrainReward && player.HasFocusedAttackReward, Is.True);
        player.Setup(player.Data);
        Assert.That(player.HasLifeDrainReward || player.HasFocusedAttackReward, Is.False);
    }

    [Test]
    public void OtherClassesNeverLearnWarriorTechniques()
    {
        BodyPart face = Part(PartType.Face); face.TakePartDamage(1000);
        Assert.That(Player("Mage").BattleEffects.LearnTechnique(PlayerRole.Mage, face), Is.False);
        Assert.That(Player("Berserker").BattleEffects.LearnTechnique(PlayerRole.Berserker, face), Is.False);
    }

    [Test]
    public void RejectedBloodPriceConsumesNeitherCostsCooldownNorPreparedEffects()
    {
        PlayerManager player = Player("Berserker");
        EnemyManager target = Enemy();
        SkillData skill = Skill(SkillId.Berserker_BloodPrice);
        player.hp = 150;
        player.BattleEffects.GrantAim();
        player.BattleEffects.GrantWarCry();
        Assert.That(SkillUseRules.TryPay(player, target, skill, out _), Is.False);
        Assert.That(player.hp, Is.EqualTo(150)); Assert.That(player.mp, Is.EqualTo(60));
        Assert.That(player.GetComponent<SkillCooldowns>().Remaining(skill), Is.Zero);
        Assert.That(player.BattleEffects.IsAiming, Is.True);
        Assert.That(player.BattleEffects.EmpoweredAttacks, Is.EqualTo(2));
        player.hp = 151;
        Assert.That(SkillUseRules.TryPay(player, target, skill, out _), Is.True);
        Assert.That(player.hp, Is.EqualTo(1));
    }

    [Test]
    public void WrongRoleMissingMpAndWrongTargetDoNotSpendAnything()
    {
        PlayerManager player = Player("Warrior");
        EnemyManager target = Enemy();
        Assert.That(SkillExecutor.Execute(player, target, Skill(SkillId.Mage_ArcaneBurst)).executed, Is.False);
        Assert.That(SkillExecutor.Execute(player, target, Skill(SkillId.Common_Guard)).executed, Is.False);
        player.mp = 0;
        Assert.That(SkillUseRules.TryPay(player, target, Skill(SkillId.Warrior_BreakSlash), out _), Is.False);
        Assert.That(player.hp, Is.EqualTo(1000)); Assert.That(player.mp, Is.Zero);
    }

    [Test]
    public void FirstAidAndAntidoteValidateBeforeCosts()
    {
        PlayerManager player = Player("Mage");
        SkillData aid = Skill(SkillId.Common_FirstAid);
        SkillData antidote = Skill(SkillId.Common_Antidote);
        Assert.That(SkillExecutor.Execute(player, player, aid).executed, Is.False);
        Assert.That(SkillExecutor.Execute(player, player, antidote).executed, Is.False);
        Assert.That(player.mp, Is.EqualTo(150));
        player.hp = 750;
        Assert.That(SkillExecutor.Execute(player, player, aid).value, Is.EqualTo(50));
        Assert.That(player.hp, Is.EqualTo(800)); Assert.That(player.mp, Is.EqualTo(142));
        SkillData poisonSkill = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Skill Data/Skill_Poison.asset");
        Assert.That(player.ApplyPoison(poisonSkill.statusEffect), Is.True);
        Assert.That(SkillExecutor.Execute(player, player, antidote).executed, Is.True);
        Assert.That(player.IsPoisoned, Is.False); Assert.That(player.hp, Is.EqualTo(800));
    }

    [Test]
    public void GuardUsesStrongestRateAndStacksOnlyWithWarriorStance()
    {
        PlayerManager player = Player("Warrior");
        BodyPart leg = Part(PartType.Leg); leg.TakePartDamage(1000);
        player.BattleEffects.LearnTechnique(player.Role, leg);
        player.BattleEffects.GrantGuard(.25f); player.BattleEffects.GrantGuard(.5f); player.BattleEffects.GrantGuard(.25f);
        Assert.That(player.BattleEffects.ReduceDirectDamage(200), Is.EqualTo(85));
        int before = player.hp;
        player.TakeDirectDamage(200, true);
        Assert.That(before - player.hp, Is.EqualTo(85));
        before = player.hp;
        player.TakeDamageRaw(170);
        Assert.That(before - player.hp, Is.EqualTo(170));
        player.BattleEffects.EndEnemyAction();
        Assert.That(player.BattleEffects.ReduceDirectDamage(200), Is.EqualTo(170));
    }

    [Test]
    public void AimSurvivesExtraActionAndExpiresAtNextPlayerTurnEnd()
    {
        PlayerManager player = Player("Mage");
        player.BattleEffects.BeginPlayerTurn(); player.BattleEffects.GrantAim();
        player.BattleEffects.EndPlayerTurn();
        Assert.That(player.BattleEffects.IsAiming, Is.True);
        player.BattleEffects.BeginPlayerTurn();
        Assert.That(player.BattleEffects.IsAiming, Is.True);
        player.BattleEffects.EndPlayerTurn();
        Assert.That(player.BattleEffects.IsAiming, Is.False);
    }

    [Test]
    public void AimAndFocusedAttackMultiplyOnlyPartsOnceAndWardOnlyProtectsBody()
    {
        PlayerManager player = Player("Mage"); player.BattleEffects.GrantAim();
        EnemyManager enemy = Enemy();
        BodyPart part = Part(PartType.Hand, true, enemy.transform); Set(part, "partHp", 1000);
        var parts = enemy.gameObject.AddComponent<EnemyPartsController>();
        typeof(EnemyPartsController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(parts, null);
        parts.SetSelectedPart(part);
        enemy.BattleEffects.GrantWard();
        var context = new AttackContext { baseDamage = 100, mainDamageRate = 1, partDamageRate = 1, focusedPartDamageMultiplier = 1.4f };
        SkillExecutor.PreparePlayerAttack(player, ref context);
        var result = parts.ApplyAttack(context);
        Assert.That(result.mainDamage, Is.EqualTo(70)); Assert.That(result.partDamage, Is.EqualTo(175));
        Assert.That(context.accuracyBonus, Is.EqualTo(.15f).Within(.0001f));
        Assert.That(player.BattleEffects.IsAiming, Is.False);
        enemy.BattleEffects.EndPlayerTurn();
        Assert.That(enemy.TakeDirectDamage(100), Is.EqualTo(100));
    }

    [Test]
    public void WarCryRefreshesToTwoAndConsumesOnlyPhysicalAttackAttempts()
    {
        PlayerManager player = Player("Berserker"); player.hp = 1000;
        var result = SkillExecutor.Execute(player, player, Skill(SkillId.Berserker_WarCry));
        Assert.That(result.value, Is.EqualTo(225));
        var first = new AttackContext { baseDamage = 100 };
        SkillExecutor.PreparePlayerAttack(player, ref first);
        Assert.That(first.baseDamage, Is.EqualTo(125));
        Assert.That(player.BattleEffects.EmpoweredAttacks, Is.EqualTo(1));
        player.BattleEffects.GrantWarCry(); Assert.That(player.BattleEffects.EmpoweredAttacks, Is.EqualTo(2));
        var second = new AttackContext { baseDamage = 100 }; SkillExecutor.PreparePlayerAttack(player, ref second);
        var third = new AttackContext { baseDamage = 100 }; SkillExecutor.PreparePlayerAttack(player, ref third);
        var fourth = new AttackContext { baseDamage = 100 }; SkillExecutor.PreparePlayerAttack(player, ref fourth);
        Assert.That(new[] { second.baseDamage, third.baseDamage, fourth.baseDamage }, Is.EqualTo(new[] { 125f, 125f, 100f }));
    }

    [Test]
    public void NewEnemyMagicDamagesPlayerAndLevyClampsMpWithoutAbsorbing()
    {
        PlayerManager player = Player("Mage"); player.mp = 5;
        EnemyManager enemy = Enemy(); enemy.mag = 26; enemy.mp = 80;
        var result = SkillExecutor.Execute(enemy, player, Skill(SkillId.Auditor_ManaLevy));
        Assert.That(result.executed && result.hit, Is.True); Assert.That(result.value, Is.GreaterThan(0));
        Assert.That(player.mp, Is.Zero); Assert.That(enemy.mp, Is.EqualTo(74));
    }

    [Test]
    public void AllTwentyThreeAssetsHaveUniqueIdsAndExpectedTargets()
    {
        ClassExpansionAssets.ValidateClassesAndSkills();
        foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
        {
            if (id == SkillId.None) continue;
            SkillData skill = Skill(id);
            Assert.That(skill, Is.Not.Null, id.ToString()); Assert.That(skill.id, Is.EqualTo(id));
            Assert.That(skill.accuracy, Is.EqualTo(1f));
            Assert.That(skill.targetType, Is.EqualTo(SkillUseRules.IsDirectAttack(skill) ? TargetType.Single : TargetType.Self));
        }
    }

    [Test]
    public void NewUiFontContainsAllSkillNamesAndDescriptionsWithoutChangingOldFonts()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Skill Data/Phase3/Phase3UI.asset");
        Assert.That(font, Is.Not.Null);
        Assert.That(font.atlasPopulationMode, Is.EqualTo(TMPro.AtlasPopulationMode.Static));
        foreach (string guid in AssetDatabase.FindAssets("t:SkillData"))
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (char c in (skill.skillName + skill.description).Where(c => !char.IsControl(c)).Distinct())
                Assert.That(font.HasCharacter(c), Is.True, skill.name + " missing " + c);
        }
    }

    [Test]
    public void PoisonKeepsThreeTicksOfFivePercentAndBypassesGuard()
    {
        PlayerManager player = Player("Warrior");
        SkillData poison = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Skill Data/Skill_Poison.asset");
        Assert.That(poison.cooldown, Is.EqualTo(4));
        Assert.That(player.ApplyPoison(poison.statusEffect), Is.True);
        player.BattleEffects.GrantGuard(.5f);
        Assert.That(player.TickPoisonDamage(), Is.EqualTo(50));
        Assert.That(player.TickPoisonDamage(), Is.EqualTo(50));
        Assert.That(player.TickPoisonDamage(), Is.EqualTo(50));
        Assert.That(player.IsPoisoned, Is.False);
        Assert.That(player.TickPoisonDamage(), Is.Zero);
        Assert.That(player.hp, Is.EqualTo(850));
    }

    private PlayerManager Player(string name)
    {
        var player = Go("Player").AddComponent<PlayerManager>();
        player.Setup(AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Script/Player Data/" + name + ".asset"));
        return player;
    }

    private EnemyManager Enemy()
    {
        var enemy = Go("Enemy").AddComponent<EnemyManager>(); enemy.hp = enemy.maxHp = 2000;
        enemy.mp = enemy.maxMp = 100; return enemy;
    }

    private BodyPart Part(PartType type, bool canBreak = true, Transform parent = null)
    {
        GameObject go = Go(type.ToString());
        if (parent != null) go.transform.SetParent(parent, false);
        go.AddComponent<PolygonCollider2D>();
        var part = go.AddComponent<BodyPart>(); part.partType = type; part.canBreak = canBreak;
        typeof(BodyPart).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(part, null);
        return part;
    }

    private GameObject Go(string name) { var go = new GameObject(name); objects.Add(go); return go; }
    private static SkillData Skill(SkillId id) => ClassExpansionAssets.LoadSkill(id);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
}
