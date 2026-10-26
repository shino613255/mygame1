using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrowthSelectionManager : MonoBehaviour
{
    private const int RewardChoiceCount = 3;
    private const float StatusGrowthRate = 1.2f;

    private static readonly string[] RewardButtonNames =
    {
        "HpUpButton",
        "MagicAttackUpButton",
        "DefenceUpButton"
    };

    private static readonly RewardDefinition[] RewardDefinitions =
    {
        new RewardDefinition(GrowthRewardId.MaxHpUp, "\u6700\u5927HP\u4E0A\u6607", true),
        new RewardDefinition(GrowthRewardId.MagicAttackUp, "\u9B54\u6CD5\u653B\u6483\u4E0A\u6607", true),
        new RewardDefinition(GrowthRewardId.DefenseUp, "\u9632\u5FA1\u4E0A\u6607", true),
        new RewardDefinition(GrowthRewardId.AttackUp, "\u653B\u6483\u529B\u4E0A\u6607", true),
        new RewardDefinition(GrowthRewardId.MaxMpUp, "\u6700\u5927MP\u4E0A\u6607", true),
        new RewardDefinition(GrowthRewardId.LifeDrain, "\u751F\u547D\u5438\u53CE", false),
        new RewardDefinition(GrowthRewardId.FocusedAttack, "\u96C6\u4E2D\u653B\u6483", false),
        new RewardDefinition(GrowthRewardId.PrecisionShot, "\u7CBE\u5BC6\u5C04\u6483", false)
    };

    [SerializeField] private GameObject growthPanel;
    [SerializeField] private PlayerManager player;
    [SerializeField] private PlayerUIManager playerUI;
    [SerializeField] private QuestManager questManager;

    private readonly List<RewardSlot> rewardSlots = new List<RewardSlot>();
    private readonly List<GrowthRewardId> currentOffers = new List<GrowthRewardId>();
    private bool selectionOpen;
    private bool selectionCommitted;

    public IReadOnlyList<GrowthRewardId> CurrentOffers => currentOffers;

    private void Start()
    {
        InitializeRewardSlots();

        if (growthPanel != null)
        {
            growthPanel.SetActive(false);
        }
    }

    public void Open()
    {
        OpenWithSeed(UnityEngine.Random.Range(0, int.MaxValue));
    }

    private void OpenWithSeed(int seed)
    {
        if (player == null || playerUI == null || questManager == null || !InitializeRewardSlots())
        {
            Debug.LogError("GrowthSelectionManager references or reward button TMP labels are not configured.");
            return;
        }

        selectionOpen = true;
        selectionCommitted = false;

        List<GrowthRewardId> eligibleRewards = GetEligibleRewardIds(player);
        List<GrowthRewardId> offers = SelectRandomRewards(eligibleRewards, RewardChoiceCount, seed);
        BindRewardSlots(offers);
        growthPanel.SetActive(true);
    }

    public void SelectMaxHpUp()
    {
        SelectReward(GrowthRewardId.MaxHpUp);
    }

    public void SelectMagicAttackUp()
    {
        SelectReward(GrowthRewardId.MagicAttackUp);
    }

    public void SelectDefenseUp()
    {
        SelectReward(GrowthRewardId.DefenseUp);
    }

    public void SelectLifeDrain()
    {
        SelectReward(GrowthRewardId.LifeDrain);
    }

    public void SelectFocusedAttack()
    {
        SelectReward(GrowthRewardId.FocusedAttack);
    }

    public void SelectPrecisionShot()
    {
        SelectReward(GrowthRewardId.PrecisionShot);
    }

    public static List<GrowthRewardId> GetEligibleRewardIds(PlayerManager targetPlayer)
    {
        var eligibleRewards = new List<GrowthRewardId>();

        foreach (RewardDefinition definition in RewardDefinitions)
        {
            PlayerRole role = targetPlayer != null ? targetPlayer.Role : PlayerRole.Mage;
            if (definition.Id == GrowthRewardId.MagicAttackUp && role != PlayerRole.Mage) continue;
            if (definition.Id == GrowthRewardId.AttackUp && role == PlayerRole.Mage) continue;
            if (definition.Id == GrowthRewardId.DefenseUp && role == PlayerRole.Berserker) continue;
            if (definition.Id == GrowthRewardId.MaxMpUp && role != PlayerRole.Berserker) continue;
            if (definition.IsRepeatable || (targetPlayer != null && !targetPlayer.HasGrowthReward(definition.Id)))
            {
                eligibleRewards.Add(definition.Id);
            }
        }

        return eligibleRewards;
    }

    public static List<GrowthRewardId> SelectRandomRewards(
        IReadOnlyList<GrowthRewardId> eligibleRewards,
        int maxChoices,
        int seed)
    {
        var candidates = new List<GrowthRewardId>();
        if (eligibleRewards == null || maxChoices <= 0)
        {
            return candidates;
        }

        for (int i = 0; i < eligibleRewards.Count; i++)
        {
            GrowthRewardId rewardId = eligibleRewards[i];
            if (!candidates.Contains(rewardId))
            {
                candidates.Add(rewardId);
            }
        }

        var random = new System.Random(seed);
        int choiceCount = Math.Min(maxChoices, candidates.Count);

        for (int i = 0; i < choiceCount; i++)
        {
            int selectedIndex = random.Next(i, candidates.Count);
            GrowthRewardId selectedReward = candidates[selectedIndex];
            candidates[selectedIndex] = candidates[i];
            candidates[i] = selectedReward;
        }

        return candidates.GetRange(0, choiceCount);
    }

    public static string GetRewardDisplayName(GrowthRewardId rewardId)
    {
        return GetRewardDefinition(rewardId).DisplayName;
    }

    private bool InitializeRewardSlots()
    {
        if (rewardSlots.Count == RewardButtonNames.Length)
        {
            return true;
        }

        rewardSlots.Clear();
        if (growthPanel == null)
        {
            return false;
        }

        foreach (string buttonName in RewardButtonNames)
        {
            Transform buttonTransform = growthPanel.transform.Find(buttonName);
            if (buttonTransform == null)
            {
                rewardSlots.Clear();
                return false;
            }

            Button button = buttonTransform.GetComponent<Button>();
            TMP_Text label = buttonTransform.GetComponentInChildren<TMP_Text>(true);
            if (button == null || label == null)
            {
                rewardSlots.Clear();
                return false;
            }

            rewardSlots.Add(new RewardSlot(button, label));
        }

        return true;
    }

    private void BindRewardSlots(IReadOnlyList<GrowthRewardId> offers)
    {
        currentOffers.Clear();

        for (int i = 0; i < rewardSlots.Count; i++)
        {
            RewardSlot slot = rewardSlots[i];
            slot.Button.onClick = new Button.ButtonClickedEvent();

            bool hasOffer = i < offers.Count;
            slot.Button.gameObject.SetActive(hasOffer);
            slot.Button.interactable = hasOffer;

            if (!hasOffer)
            {
                continue;
            }

            GrowthRewardId rewardId = offers[i];
            currentOffers.Add(rewardId);
            slot.Label.text = GetRewardDisplayName(rewardId);
            slot.Button.onClick.AddListener(() => SelectReward(rewardId));
        }
    }

    private void SelectReward(GrowthRewardId rewardId)
    {
        if (!selectionOpen || selectionCommitted || !currentOffers.Contains(rewardId))
        {
            return;
        }

        selectionCommitted = true;
        selectionOpen = false;
        SetRewardButtonsInteractable(false);

        if (!TryApplyReward(rewardId))
        {
            selectionCommitted = false;
            Open();
            return;
        }

        FinishSelection();
    }

    private bool TryApplyReward(GrowthRewardId rewardId)
    {
        if (player == null)
        {
            return false;
        }

        switch (rewardId)
        {
            case GrowthRewardId.MaxHpUp:
                player.EnhanceMaxHpRate(StatusGrowthRate);
                return true;
            case GrowthRewardId.MagicAttackUp:
                player.EnhanceMagicAttackRate(StatusGrowthRate);
                return true;
            case GrowthRewardId.DefenseUp:
                player.EnhanceDefenseRate(StatusGrowthRate);
                return true;
            case GrowthRewardId.AttackUp:
                player.EnhanceAttackRate(StatusGrowthRate);
                return true;
            case GrowthRewardId.MaxMpUp:
                player.EnhanceMaxMpRate(StatusGrowthRate);
                return true;
            case GrowthRewardId.LifeDrain:
                return player.TryAcquireGrowthReward(GrowthRewardId.LifeDrain);
            case GrowthRewardId.FocusedAttack:
                return player.TryAcquireGrowthReward(GrowthRewardId.FocusedAttack);
            case GrowthRewardId.PrecisionShot:
                return player.TryAcquireGrowthReward(GrowthRewardId.PrecisionShot);
            default:
                return false;
        }
    }

    private void SetRewardButtonsInteractable(bool interactable)
    {
        foreach (RewardSlot slot in rewardSlots)
        {
            slot.Button.interactable = interactable;
        }
    }

    private void FinishSelection()
    {
        playerUI.UpdateUI(player);
        growthPanel.SetActive(false);
        questManager.ContinueAfterGrowthSelection();
    }

    private static RewardDefinition GetRewardDefinition(GrowthRewardId rewardId)
    {
        foreach (RewardDefinition definition in RewardDefinitions)
        {
            if (definition.Id == rewardId)
            {
                return definition;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(rewardId), rewardId, "Unknown growth reward.");
    }

    private sealed class RewardSlot
    {
        public RewardSlot(Button button, TMP_Text label)
        {
            Button = button;
            Label = label;
        }

        public Button Button { get; }
        public TMP_Text Label { get; }
    }

    private readonly struct RewardDefinition
    {
        public RewardDefinition(GrowthRewardId id, string displayName, bool isRepeatable)
        {
            Id = id;
            DisplayName = displayName;
            IsRepeatable = isRepeatable;
        }

        public GrowthRewardId Id { get; }
        public string DisplayName { get; }
        public bool IsRepeatable { get; }
    }
}
