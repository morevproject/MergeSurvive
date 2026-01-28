using UnityEngine;
using UnityEngine.UI;

public class ResourceSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;    
    public Text amountText;    

    [Header("Data")]
    public string resourceName; 
    public int currentAmount;
    public ItemData itemData; 

    private bool shouldPulse = false; 
    private Button buttonComponent; // Ссылка на кнопку

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
    }

    private void Update()
    {
        // Если пульсация включена и иконка есть -> анимируем
        if (shouldPulse && iconImage != null)
        {
            // Параметры анимации
            float speed = 8.0f; 
            float minScale = 0.9f;
            float maxScale = 1.1f;
            
            Color normalColor = Color.white;
            Color alertColor = new Color(1f, 0.3f, 0.3f); // Светло-красный

            // UnscaledTime чтобы работало даже на паузе
            float time = Time.unscaledTime * speed;
            float factor = (Mathf.Sin(time) + 1.0f) / 2.0f; 

            // 1. Анимация Размера
            iconImage.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, factor);

            // 2. Анимация Цвета
            iconImage.color = Color.Lerp(normalColor, alertColor, factor);
        }
    }

    public void UpdateSlot(string newResourceName, Sprite newIcon, int newAmount, ItemData newItemData)
    {
        resourceName = newResourceName;
        currentAmount = newAmount;
        itemData = newItemData;

        // Автопоиск иконки, если пустая ссылка
        if (iconImage == null)
        {
            Transform t = transform.Find("Icon");
            if (t != null) iconImage = t.GetComponent<Image>();
            if (iconImage == null) iconImage = GetComponent<Image>();
        }

        if (iconImage != null)
        {
            if (newIcon != null)
            {
                iconImage.sprite = newIcon;
                iconImage.enabled = true;
                
                // СБРОС СОСТОЯНИЯ ПРИ ОБНОВЛЕНИИ
                iconImage.color = Color.white; 
                iconImage.transform.localScale = Vector3.one;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        UpdateAmount(newAmount);
        
        var draggable = GetComponent<UIDraggableItem>();
        if (draggable != null)
        {
            InventorySlot tempSlot = new InventorySlot(newItemData, newAmount);
            draggable.SetSlotData(tempSlot);
        }
    }

    public void UpdateAmount(int newAmount)
    {
        currentAmount = newAmount;
        if (amountText != null)
        {
            amountText.text = newAmount > 0 ? newAmount.ToString() : ""; 
            amountText.color = Color.white;
        }
    }

    public void SetHighlight(bool isActive)
    {
        shouldPulse = isActive;
        
        // --- ГЛАВНЫЙ ФИКС: Управление Кнопкой ---
        if (buttonComponent != null)
        {
            if (isActive)
            {
                // Если пульсируем - отключаем вмешательство кнопки в цвет
                buttonComponent.transition = Selectable.Transition.None;
            }
            else
            {
                // Если закончили - возвращаем стандартное поведение (Tint или что было)
                buttonComponent.transition = Selectable.Transition.ColorTint;
            }
        }

        // Если выключаем пульсацию - принудительно сбрасываем цвета в норму
        if (!isActive && iconImage != null)
        {
            iconImage.color = Color.white;
            iconImage.transform.localScale = Vector3.one;
        }
    }

    private void OnDisable()
    {
        // Страховка: при выключении объекта сбрасываем визуал
        if (iconImage != null)
        {
            iconImage.color = Color.white;
            iconImage.transform.localScale = Vector3.one;
        }
    }
}