using UnityEngine;
using UnityEngine.UI;

public class RewardSlotUI : MonoBehaviour
{
    [Header("UI Элементы")]
    public Text dayText;
    public Image iconImage;
    public Text amountText;
    public Image bgImage;       // Подложка слота
    public GameObject glowEffect; // Эффект свечения (для "вех")

    [Header("Цвета состояний")]
    public Color claimedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Серый для полученных
    public Color todayColor = new Color(0.2f, 0.8f, 0.2f, 1f);     // Зеленый/Яркий для сегодня
    public Color futureColor = Color.white;                        // Обычный для будущих

    // state: 0 = Забрано (Прошлое), 1 = Доступно (Сегодня), 2 = Заблокировано (Будущее)
    public void Setup(int dayIndex, ItemData item, int amount, bool isBig, int state)
    {
        if (dayText != null) dayText.text = "День " + (dayIndex + 1);
        if (amountText != null) amountText.text = amount.ToString();
        
        if (iconImage != null && item != null)
        {
            iconImage.sprite = item.Icon; // Убедись, что в ItemData иконка называется Icon
        }

        if (glowEffect != null) glowEffect.SetActive(isBig);

        // Раскрашиваем в зависимости от статуса
        if (bgImage != null)
        {
            if (state == 0) bgImage.color = claimedColor;
            else if (state == 1) bgImage.color = todayColor;
            else bgImage.color = futureColor;
        }

        // Делаем иконку полупрозрачной, если уже забрали
        if (iconImage != null)
        {
            iconImage.color = state == 0 ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
        }
    }
}