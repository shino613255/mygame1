using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLifeDrainTests
{
    private GameObject playerObject;
    private GameObject partObject;

    [TearDown]
    public void TearDown()
    {
        if (partObject != null)
            UnityEngine.Object.DestroyImmediate(partObject);

        if (playerObject != null)
            UnityEngine.Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void LifeDrainReward_CannotBeAcquiredTwice()
    {
        PlayerManager player = CreatePlayer(100, 50);

        Assert.That(player.TryAcquireLifeDrainReward(), Is.True);
        Assert.That(player.TryAcquireLifeDrainReward(), Is.False);
    }

    [Test]
    public void BrokenPart_RecoversFivePercentOnceAndDoesNotExceedMaxHp()
    {
        PlayerManager player = CreatePlayer(100, 50);
        BodyPart part = CreatePart(true);
        player.TryAcquireLifeDrainReward();
        part.TakePartDamage(1000);

        Assert.That(player.TryRecoverHpFromPartBreak(part), Is.EqualTo(5));
        Assert.That(player.hp, Is.EqualTo(55));
        Assert.That(player.TryRecoverHpFromPartBreak(part), Is.Zero);

        BodyPart nextEnemyPart = CreatePart(true);
        partObject.transform.DetachChildren();
        partObject = nextEnemyPart.gameObject;
        nextEnemyPart.TakePartDamage(1000);
        player.hp = 98;

        Assert.That(player.TryRecoverHpFromPartBreak(nextEnemyPart), Is.EqualTo(2));
        Assert.That(player.hp, Is.EqualTo(100));
    }

    [Test]
    public void UnbreakablePart_DoesNotRecoverHp()
    {
        PlayerManager player = CreatePlayer(100, 50);
        BodyPart part = CreatePart(false);
        player.TryAcquireLifeDrainReward();
        part.TakePartDamage(1000);

        Assert.That(part.IsBroken, Is.False);
        Assert.That(player.TryRecoverHpFromPartBreak(part), Is.Zero);
        Assert.That(player.hp, Is.EqualTo(50));
    }

    [Test]
    public void SetupForNewQuest_ResetsLifeDrainReward()
    {
        PlayerManager player = CreatePlayer(100, 50);
        player.TryAcquireLifeDrainReward();
        SetPrivateField(player, "skillSlots", Array.Empty<SkillSlotUI>());

        PlayerData data = ScriptableObject.CreateInstance<PlayerData>();
        data.playerName = "Test Player";
        data.startMaxHp = 100;
        data.startMaxMp = 30;
        data.startAt = 10;
        data.startMag = 10;
        data.startDef = 5;
        data.startMdef = 3;

        player.Setup(data);

        Assert.That(player.HasLifeDrainReward, Is.False);
        UnityEngine.Object.DestroyImmediate(data);
    }

    private PlayerManager CreatePlayer(int maxHp, int hp)
    {
        playerObject = new GameObject("Player");
        PlayerManager player = playerObject.AddComponent<PlayerManager>();
        player.maxHp = maxHp;
        player.hp = hp;
        return player;
    }

    private BodyPart CreatePart(bool canBreak)
    {
        if (partObject != null)
            UnityEngine.Object.DestroyImmediate(partObject);

        partObject = new GameObject("BodyPart");
        partObject.AddComponent<PolygonCollider2D>();
        BodyPart part = partObject.AddComponent<BodyPart>();
        part.canBreak = canBreak;
        return part;
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

public class GrowthSelectionManagerTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

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
    public void RandomOffer_SelectsThreeDistinctRewardsDeterministically()
    {
        var eligible = new List<GrowthRewardId>
        {
            GrowthRewardId.MaxHpUp,
            GrowthRewardId.MagicAttackUp,
            GrowthRewardId.DefenseUp,
            GrowthRewardId.LifeDrain,
            GrowthRewardId.FocusedAttack,
            GrowthRewardId.PrecisionShot
        };

        List<GrowthRewardId> first =
            GrowthSelectionManager.SelectRandomRewards(eligible, 3, 12345);
        List<GrowthRewardId> second =
            GrowthSelectionManager.SelectRandomRewards(eligible, 3, 12345);

        Assert.That(first, Has.Count.EqualTo(3));
        Assert.That(first.Distinct().Count(), Is.EqualTo(3));
        CollectionAssert.AreEqual(first, second);
        Assert.That(first.All(eligible.Contains), Is.True);
    }

    [Test]
    public void RandomOffer_IsSafeWhenFewerThanThreeUniqueRewardsExist()
    {
        var eligible = new List<GrowthRewardId>
        {
            GrowthRewardId.MaxHpUp,
            GrowthRewardId.MaxHpUp,
            GrowthRewardId.DefenseUp
        };

        List<GrowthRewardId> offers =
            GrowthSelectionManager.SelectRandomRewards(eligible, 3, 7);

        Assert.That(offers, Has.Count.EqualTo(2));
        Assert.That(offers.Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void AcquiredLifeDrain_IsExcludedAndFocusedAttackRemainsEligible()
    {
        PlayerManager player = CreatePlayer();
        Assert.That(player.TryAcquireGrowthReward(GrowthRewardId.LifeDrain), Is.True);

        List<GrowthRewardId> eligible = GrowthSelectionManager.GetEligibleRewardIds(player);

        Assert.That(eligible, Has.Count.EqualTo(5));
        CollectionAssert.DoesNotContain(eligible, GrowthRewardId.LifeDrain);
        CollectionAssert.Contains(eligible, GrowthRewardId.MaxHpUp);
        CollectionAssert.Contains(eligible, GrowthRewardId.MagicAttackUp);
        CollectionAssert.Contains(eligible, GrowthRewardId.DefenseUp);
        CollectionAssert.Contains(eligible, GrowthRewardId.FocusedAttack);
        CollectionAssert.Contains(eligible, GrowthRewardId.PrecisionShot);
    }

    [Test]
    public void ExistingStatusRewards_KeepTwentyPercentGrowth()
    {
        PlayerManager player = CreatePlayer();
        player.maxHp = 100;
        player.hp = 50;
        player.mag = 10;
        player.def = 10;

        GrowthSelectionManager manager =
            CreateObject("GrowthSelectionManager").AddComponent<GrowthSelectionManager>();
        SetPrivateField(manager, "player", player);

        Assert.That(InvokePrivate(manager, "TryApplyReward", GrowthRewardId.MaxHpUp), Is.True);
        Assert.That(InvokePrivate(manager, "TryApplyReward", GrowthRewardId.MagicAttackUp), Is.True);
        Assert.That(InvokePrivate(manager, "TryApplyReward", GrowthRewardId.DefenseUp), Is.True);

        Assert.That(player.maxHp, Is.EqualTo(120));
        Assert.That(player.hp, Is.EqualTo(70));
        Assert.That(player.mag, Is.EqualTo(12));
        Assert.That(player.def, Is.EqualTo(12));
    }

    [Test]
    public void FocusedAttackReward_CanBeAppliedOnlyOnce()
    {
        PlayerManager player = CreatePlayer();
        GrowthSelectionManager manager =
            CreateObject("GrowthSelectionManager").AddComponent<GrowthSelectionManager>();
        SetPrivateField(manager, "player", player);

        Assert.That(InvokePrivate(manager, "TryApplyReward", GrowthRewardId.FocusedAttack), Is.True);
        Assert.That(player.HasFocusedAttackReward, Is.True);
        Assert.That(InvokePrivate(manager, "TryApplyReward", GrowthRewardId.FocusedAttack), Is.False);
    }

    [Test]
    public void PrecisionShot_AddsTenPointsAfterEvasionAndCapsAtOne()
    {
        Assert.That(DamageRule.CalcHitChance(1f, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(DamageRule.CalcHitChance(1f, 0.5f, 0.1f), Is.EqualTo(0.6f).Within(0.0001f));
        Assert.That(DamageRule.CalcHitChance(1f, 0.02f, 0.1f), Is.EqualTo(1f));
        Assert.That(DamageRule.CalcHitChance(1f, 0f, 0.1f), Is.EqualTo(1f));
    }

    [Test]
    public void PrecisionShotReward_IsUniqueExcludedAndResetForNewQuest()
    {
        PlayerManager player = CreatePlayer();
        GrowthSelectionManager manager =
            CreateObject("GrowthSelectionManager").AddComponent<GrowthSelectionManager>();
        SetPrivateField(manager, "player", player);

        Assert.That(player.GetPrecisionShotAccuracyBonus(), Is.Zero);
        Assert.That(InvokePrivate(manager, "TryApplyReward", GrowthRewardId.PrecisionShot), Is.True);
        Assert.That(player.HasPrecisionShotReward, Is.True);
        Assert.That(player.GetPrecisionShotAccuracyBonus(), Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(InvokePrivate(manager, "TryApplyReward", GrowthRewardId.PrecisionShot), Is.False);
        Assert.That(GrowthSelectionManager.GetRewardDisplayName(GrowthRewardId.PrecisionShot), Is.EqualTo("精密射撃"));

        List<GrowthRewardId> eligible = GrowthSelectionManager.GetEligibleRewardIds(player);
        CollectionAssert.DoesNotContain(eligible, GrowthRewardId.PrecisionShot);
        CollectionAssert.Contains(eligible, GrowthRewardId.LifeDrain);
        CollectionAssert.Contains(eligible, GrowthRewardId.FocusedAttack);
        Assert.That(eligible, Has.Count.EqualTo(5));

        SetPrivateField(player, "skillSlots", Array.Empty<SkillSlotUI>());
        PlayerData data = ScriptableObject.CreateInstance<PlayerData>();
        player.Setup(data);

        Assert.That(player.HasPrecisionShotReward, Is.False);
        Assert.That(player.GetPrecisionShotAccuracyBonus(), Is.Zero);
        UnityEngine.Object.DestroyImmediate(data);
    }

    [Test]
    public void TmpLabelsMatchOffersAndRepeatedClickAppliesOnlyOnce()
    {
        PlayerManager player = CreatePlayer();
        player.maxHp = 100;
        player.hp = 50;
        player.mag = 10;
        player.def = 10;

        GameObject panel = CreateObject("GrowthSelectionPanel", typeof(RectTransform));
        Button[] buttons =
        {
            CreateRewardButton(panel.transform, "HpUpButton"),
            CreateRewardButton(panel.transform, "MagicAttackUpButton"),
            CreateRewardButton(panel.transform, "DefenceUpButton")
        };

        PlayerUIManager playerUI = CreatePlayerUI();
        QuestManager questManager = CreateObject("QuestManager").AddComponent<QuestManager>();
        GrowthSelectionManager manager =
            CreateObject("GrowthSelectionManager").AddComponent<GrowthSelectionManager>();

        SetPrivateField(manager, "growthPanel", panel);
        SetPrivateField(manager, "player", player);
        SetPrivateField(manager, "playerUI", playerUI);
        SetPrivateField(manager, "questManager", questManager);
        InvokePrivate(manager, "OpenWithSeed", 12345);

        Assert.That(manager.CurrentOffers, Has.Count.EqualTo(3));
        for (int i = 0; i < buttons.Length; i++)
        {
            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
            Assert.That(
                label.text,
                Is.EqualTo(GrowthSelectionManager.GetRewardDisplayName(manager.CurrentOffers[i]))
            );
        }

        int statusIndex = manager.CurrentOffers
            .Select((reward, index) => new { reward, index })
            .First(item =>
                item.reward == GrowthRewardId.MaxHpUp ||
                item.reward == GrowthRewardId.MagicAttackUp ||
                item.reward == GrowthRewardId.DefenseUp)
            .index;
        GrowthRewardId selectedReward = manager.CurrentOffers[statusIndex];

        buttons[statusIndex].onClick.Invoke();
        buttons[statusIndex].onClick.Invoke();

        Assert.That(panel.activeSelf, Is.False);
        Assert.That(buttons.All(button => !button.interactable), Is.True);

        switch (selectedReward)
        {
            case GrowthRewardId.MaxHpUp:
                Assert.That(player.maxHp, Is.EqualTo(120));
                Assert.That(player.hp, Is.EqualTo(70));
                Assert.That(player.mag, Is.EqualTo(10));
                Assert.That(player.def, Is.EqualTo(10));
                break;
            case GrowthRewardId.MagicAttackUp:
                Assert.That(player.maxHp, Is.EqualTo(100));
                Assert.That(player.mag, Is.EqualTo(12));
                Assert.That(player.def, Is.EqualTo(10));
                break;
            case GrowthRewardId.DefenseUp:
                Assert.That(player.maxHp, Is.EqualTo(100));
                Assert.That(player.mag, Is.EqualTo(10));
                Assert.That(player.def, Is.EqualTo(12));
                break;
        }
    }

    private PlayerManager CreatePlayer()
    {
        return CreateObject("Player").AddComponent<PlayerManager>();
    }

    private PlayerUIManager CreatePlayerUI()
    {
        PlayerUIManager playerUI = CreateObject("PlayerUI").AddComponent<PlayerUIManager>();
        playerUI.hpText = CreateObject("HpText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        playerUI.mpText = CreateObject("MpText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        playerUI.hpSlider = CreateObject("HpSlider", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
        playerUI.mpSlider = CreateObject("MpSlider", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
        return playerUI;
    }

    private Button CreateRewardButton(Transform parent, string buttonName)
    {
        GameObject buttonObject = CreateObject(
            buttonName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        GameObject labelObject = CreateObject(
            "Text (TMP)",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        labelObject.transform.SetParent(buttonObject.transform, false);

        return buttonObject.GetComponent<Button>();
    }

    private GameObject CreateObject(string objectName, params Type[] components)
    {
        var gameObject = new GameObject(objectName, components);
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

    private static object InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, arguments);
    }
}
