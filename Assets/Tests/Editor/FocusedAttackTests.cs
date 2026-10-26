using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using static EnemyPartsController;

public class FocusedAttackTests
{
    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void NotAcquired_AlwaysUsesOneTimesMultiplier()
    {
        PlayerManager player = CreatePlayer();
        CreateEnemyWithPart("Enemy", out EnemyManager enemy, out _, out BodyPart part);

        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1f));

        player.RecordFocusedAttackResult(enemy, part, 10);

        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1f));
    }

    [Test]
    public void ConsecutiveHits_UseOneThenOnePointTwoThenOnePointFourAndCap()
    {
        PlayerManager player = CreatePlayerWithFocusedAttack();
        CreateEnemyWithPart("Enemy", out EnemyManager enemy, out _, out BodyPart part);

        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1f));

        player.RecordFocusedAttackResult(enemy, part, 1);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1.2f));

        player.RecordFocusedAttackResult(enemy, part, 1);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1.4f));

        player.RecordFocusedAttackResult(enemy, part, 1);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1.4f));
    }

    [Test]
    public void ConsecutiveAttackResults_ApplyFiveThenSixThenSevenThenSevenPartDamage()
    {
        PlayerManager player = CreatePlayerWithFocusedAttack();
        CreateEnemyWithPart(
            "Enemy",
            out EnemyManager enemy,
            out EnemyPartsController parts,
            out BodyPart part
        );

        Assert.That(ApplyAttack(player, enemy, parts, part, 5).partDamage, Is.EqualTo(5));
        Assert.That(ApplyAttack(player, enemy, parts, part, 5).partDamage, Is.EqualTo(6));
        Assert.That(ApplyAttack(player, enemy, parts, part, 5).partDamage, Is.EqualTo(7));
        Assert.That(ApplyAttack(player, enemy, parts, part, 5).partDamage, Is.EqualTo(7));
    }

    [Test]
    public void MissPartChangeAndNoPartDamage_ResetOrRestartTheChain()
    {
        PlayerManager player = CreatePlayerWithFocusedAttack();
        CreateEnemyWithPart("Enemy", out EnemyManager enemy, out _, out BodyPart firstPart);
        BodyPart secondPart = CreatePart(enemy.transform, "SecondPart", PartType.Hand, true);

        player.RecordFocusedAttackResult(enemy, firstPart, 1);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, firstPart), Is.EqualTo(1.2f));

        player.ResetFocusedAttackChain();
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, firstPart), Is.EqualTo(1f));

        player.RecordFocusedAttackResult(enemy, firstPart, 1);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, secondPart), Is.EqualTo(1f));

        player.RecordFocusedAttackResult(enemy, secondPart, 1);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, secondPart), Is.EqualTo(1.2f));

        player.RecordFocusedAttackResult(enemy, secondPart, 0);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, secondPart), Is.EqualTo(1f));
    }

    [Test]
    public void EnemyChange_StartsFromOneTimesMultiplier()
    {
        PlayerManager player = CreatePlayerWithFocusedAttack();
        CreateEnemyWithPart("FirstEnemy", out EnemyManager firstEnemy, out _, out BodyPart firstPart);
        CreateEnemyWithPart("SecondEnemy", out EnemyManager secondEnemy, out _, out BodyPart secondPart);

        player.RecordFocusedAttackResult(firstEnemy, firstPart, 1);

        Assert.That(player.GetFocusedAttackPartDamageMultiplier(secondEnemy, secondPart), Is.EqualTo(1f));

        player.RecordFocusedAttackResult(secondEnemy, secondPart, 1);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(secondEnemy, secondPart), Is.EqualTo(1.2f));
    }

    [Test]
    public void PartBreak_AppliesCurrentMultiplierThenResetsAndLifeDrainStillWorks()
    {
        PlayerManager player = CreatePlayerWithFocusedAttack();
        player.maxHp = 100;
        player.hp = 50;
        Assert.That(player.TryAcquireGrowthReward(GrowthRewardId.LifeDrain), Is.True);

        CreateEnemyWithPart(
            "Enemy",
            out EnemyManager enemy,
            out EnemyPartsController parts,
            out BodyPart part
        );
        player.RecordFocusedAttackResult(enemy, part, 1);

        AttackResult result = ApplyAttack(player, enemy, parts, part, 100);

        Assert.That(result.mainDamage, Is.EqualTo(100));
        Assert.That(result.partDamage, Is.EqualTo(30));
        Assert.That(part.IsBroken, Is.True);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1f));
        Assert.That(player.TryRecoverHpFromPartBreak(part), Is.EqualTo(5));
        Assert.That(player.hp, Is.EqualTo(55));
    }

    [Test]
    public void FocusedMultiplier_ChangesOnlyPartDamageAndCounterUsesMainHpLoss()
    {
        PlayerManager player = CreatePlayerWithFocusedAttack();
        CreateEnemyWithPart(
            "CounterEnemy",
            out EnemyManager enemy,
            out EnemyPartsController parts,
            out BodyPart part
        );

        EnemyData data = CreateEnemyData();
        data.role = EnemyRole.Tank;
        data.enemyType = EnemyType.Normal;
        enemy.data = data;
        enemy.ActivateCounter();

        player.RecordFocusedAttackResult(enemy, part, 1);
        player.RecordFocusedAttackResult(enemy, part, 1);
        int hpBefore = enemy.hp;

        AttackResult result = ApplyAttack(player, enemy, parts, part, 10);
        int actualMainDamage = hpBefore - enemy.hp;

        Assert.That(result.mainDamage, Is.EqualTo(10));
        Assert.That(actualMainDamage, Is.EqualTo(10));
        Assert.That(result.partDamage, Is.EqualTo(14));
        Assert.That(enemy.GetCounterDamage(actualMainDamage), Is.EqualTo(17));
    }

    [TestCase(2f, 1.2f, 20, 24)]
    [TestCase(0.5f, 1.4f, 5, 7)]
    public void WeaknessAndResistance_CombineWithFocusedMultiplier(
        float weaknessMultiplier,
        float focusedMultiplier,
        int expectedMainDamage,
        int expectedPartDamage)
    {
        CreateEnemyWithPart(
            "Enemy",
            out EnemyManager enemy,
            out EnemyPartsController parts,
            out BodyPart part
        );

        EnemyData data = CreateEnemyData();
        data.partDamageMultipliers.Add(new EnemyData.PartDamageMultiplier
        {
            partType = PartType.Face,
            physicalMultiplier = weaknessMultiplier,
            magicMultiplier = 1f
        });
        enemy.data = data;
        parts.SetSelectedPart(part);

        var context = new AttackContext
        {
            baseDamage = 10,
            mainDamageRate = 1f,
            partDamageRate = 1f,
            focusedPartDamageMultiplier = focusedMultiplier
        };

        AttackResult result = parts.ApplyAttack(context);

        Assert.That(result.mainDamage, Is.EqualTo(expectedMainDamage));
        Assert.That(result.partDamage, Is.EqualTo(expectedPartDamage));
    }

    [Test]
    public void AcquiredFocusedAttack_IsExcludedFromRewardCandidates()
    {
        PlayerManager player = CreatePlayer();
        Assert.That(player.TryAcquireGrowthReward(GrowthRewardId.FocusedAttack), Is.True);

        List<GrowthRewardId> eligible = GrowthSelectionManager.GetEligibleRewardIds(player);

        CollectionAssert.DoesNotContain(eligible, GrowthRewardId.FocusedAttack);
        CollectionAssert.Contains(eligible, GrowthRewardId.LifeDrain);
        CollectionAssert.Contains(eligible, GrowthRewardId.PrecisionShot);
        Assert.That(eligible, Has.Count.EqualTo(5));

        Assert.That(player.TryAcquireGrowthReward(GrowthRewardId.LifeDrain), Is.True);
        eligible = GrowthSelectionManager.GetEligibleRewardIds(player);

        Assert.That(eligible, Has.Count.EqualTo(4));
        CollectionAssert.AreEquivalent(
            new[]
            {
                GrowthRewardId.MaxHpUp,
                GrowthRewardId.MagicAttackUp,
                GrowthRewardId.DefenseUp,
                GrowthRewardId.PrecisionShot
            },
            eligible
        );
    }

    [Test]
    public void NewQuestSetup_ResetsFocusedAttackRewardAndChain()
    {
        PlayerManager player = CreatePlayerWithFocusedAttack();
        CreateEnemyWithPart("Enemy", out EnemyManager enemy, out _, out BodyPart part);
        player.RecordFocusedAttackResult(enemy, part, 1);
        SetPrivateField(player, "skillSlots", Array.Empty<SkillSlotUI>());

        PlayerData data = CreatePlayerData();
        player.Setup(data);

        Assert.That(player.HasFocusedAttackReward, Is.False);
        Assert.That(player.GetFocusedAttackPartDamageMultiplier(enemy, part), Is.EqualTo(1f));
    }

    private AttackResult ApplyAttack(
        PlayerManager player,
        EnemyManager enemy,
        EnemyPartsController parts,
        BodyPart part,
        int baseDamage)
    {
        parts.SetSelectedPart(part);
        var context = new AttackContext
        {
            baseDamage = baseDamage,
            mainDamageRate = 1f,
            partDamageRate = 1f,
            focusedPartDamageMultiplier =
                player.GetFocusedAttackPartDamageMultiplier(enemy, part)
        };

        AttackResult result = parts.ApplyAttack(context);
        player.RecordFocusedAttackResult(enemy, part, result.partDamage);
        return result;
    }

    private PlayerManager CreatePlayer()
    {
        GameObject playerObject = CreateGameObject("Player");
        return playerObject.AddComponent<PlayerManager>();
    }

    private PlayerManager CreatePlayerWithFocusedAttack()
    {
        PlayerManager player = CreatePlayer();
        Assert.That(player.TryAcquireGrowthReward(GrowthRewardId.FocusedAttack), Is.True);
        return player;
    }

    private void CreateEnemyWithPart(
        string objectName,
        out EnemyManager enemy,
        out EnemyPartsController parts,
        out BodyPart part)
    {
        GameObject enemyObject = CreateGameObject(objectName);
        enemy = enemyObject.AddComponent<EnemyManager>();
        enemy.maxHp = 500;
        enemy.hp = 500;
        parts = enemyObject.AddComponent<EnemyPartsController>();
        typeof(EnemyPartsController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(parts, null);
        part = CreatePart(enemyObject.transform, "Face", PartType.Face, true);
    }

    private BodyPart CreatePart(Transform parent, string objectName, PartType type, bool canBreak)
    {
        GameObject partObject = CreateGameObject(objectName);
        partObject.transform.SetParent(parent, false);
        partObject.AddComponent<PolygonCollider2D>();
        BodyPart part = partObject.AddComponent<BodyPart>();
        part.partType = type;
        part.canBreak = canBreak;
        typeof(BodyPart).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(part, null);
        return part;
    }

    private EnemyData CreateEnemyData()
    {
        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        createdObjects.Add(data);
        return data;
    }

    private PlayerData CreatePlayerData()
    {
        PlayerData data = ScriptableObject.CreateInstance<PlayerData>();
        data.playerName = "Test Player";
        data.startMaxHp = 100;
        data.startMaxMp = 30;
        data.startAt = 10;
        data.startMag = 10;
        data.startDef = 5;
        data.startMdef = 3;
        createdObjects.Add(data);
        return data;
    }

    private GameObject CreateGameObject(string objectName)
    {
        var gameObject = new GameObject(objectName);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
