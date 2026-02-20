using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingIngredientUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI amountText;

    [Header("Settings")]
    public Color availableColor = Color.white;
    public Color missingColor = new Color(1f, 0.3f, 0.3f);

    private bool isPulseActive = false;

    // --- ÕŒ¬€≈ œŒÀﬂ ƒÀﬂ ƒ»Õ¿Ã» » ---
    public string ResourceName { get; private set; }
    public int RequiredAmount { get; private set; }

    public void Setup(string resourceName, int currentAmount, int requiredAmount)
    {
        ResourceName = resourceName;
        RequiredAmount = requiredAmount;

        ItemData itemData = Resources.Load<ItemData>("Items/" + resourceName);
        if (itemData == null) itemData = Resources.Load<ItemData>(resourceName);

        if (itemData != null && iconImage != null)
        {
            iconImage.sprite = itemData.Icon;
            iconImage.enabled = true;
        }

        UpdateCurrentAmount(currentAmount); // ¬˚Á˚‚‡ÂÏ Ó·ÌÓ‚ÎÂÌËÂ ÚÂÍÒÚ‡
    }

    // --- Œ“ƒ≈À‹Õ€… Ã≈“Œƒ Œ¡ÕŒ¬À≈Õ»ﬂ ÷»‘– ---
    public void UpdateCurrentAmount(int currentAmount)
    {
        if (amountText != null)
        {
            amountText.text = $"{currentAmount}/{RequiredAmount}";
            
            bool hasEnough = currentAmount >= RequiredAmount;
            amountText.color = hasEnough ? availableColor : missingColor;
            
            SetPulse(!hasEnough);
        }
    }

    private void SetPulse(bool active)
    {
        isPulseActive = active;
        if (!active && transform != null) transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (isPulseActive)
        {
            float scale = 1.0f + Mathf.PingPong(Time.unscaledTime * 0.5f, 0.1f);
            transform.localScale = Vector3.one * scale;
        }
    }
}