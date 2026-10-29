using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public Text hpText;
    public Text mpText;
    public Slider hpSlider;
    public Slider mpSlider;

    [Header("Effect Icon")]
    [SerializeField] private Transform effectIconContainer;
    [SerializeField] private Image effectIconPrefab;
    [SerializeField] private Sprite poisonIcon;

    public Vector3 GetSkillEffectPosition(Camera worldCamera)
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            return Vector3.zero;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform portrait = canvas != null ? canvas.transform.Find("ClassPortrait") as RectTransform : null;
        float depth = Mathf.Abs(worldCamera.transform.position.z);
        if (portrait == null)
        {
            return worldCamera.ViewportToWorldPoint(new Vector3(.15f, .35f, depth));
        }

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            portrait.TransformPoint(portrait.rect.center));
        Vector3 position = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        position.z = 0;
        return position;
    }

    public void SetupUI(PlayerManager player)
    {
        hpSlider.minValue = 0;
        hpSlider.maxValue = player.maxHp;
        hpSlider.value = player.hp;
        mpSlider.minValue = 0;
        mpSlider.maxValue = player.maxMp;
        mpSlider.value = player.mp;
        UpdateResourceLabels(player);
    }

    public void UpdateUI(PlayerManager player)
    {
        hpSlider.maxValue = player.maxHp;
        mpSlider.maxValue = player.maxMp;
        hpSlider.value = player.hp;
        mpSlider.value = player.mp;
        UpdateResourceLabels(player);
    }

    private void UpdateResourceLabels(PlayerManager player)
    {
        hpText.text = $"{player.hp} / {player.maxHp}";
        mpText.text = $"{player.mp} / {player.maxMp}";
        RefreshEffectIcons(player);
    }

    private void RefreshEffectIcons(PlayerManager player)
    {
        if (effectIconContainer == null || effectIconPrefab == null)
        {
            return;
        }

        for (int i = effectIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(effectIconContainer.GetChild(i).gameObject);
        }

        if (player == null)
        {
            return;
        }

        if (player.IsPoisoned)
        {
            CreateEffectIcon(poisonIcon);
        }
    }

    private void CreateEffectIcon(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Image icon = Instantiate(effectIconPrefab, effectIconContainer);
        icon.sprite = sprite;
    }
}
