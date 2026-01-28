using UnityEngine;
using UnityEngine.UI;

public class CraftingResultSlot : MonoBehaviour
{
    [Header("UI References")]
    // Сюда перетащишь дочерний объект Icon
    public Image resultIcon; 
    
    // Сюда перетащишь дочерний объект AmountText (будем писать туда имя)
    public Text resultNameText; 

    void Awake()
    {
        // На старте прячем всё
        ClearSlot();
    }

    public void SetResult(Recipe recipe)
    {
        if (recipe == null) { ClearSlot(); return; }

        // 1. Настраиваем иконку
        if (resultIcon != null)
        {
            resultIcon.sprite = recipe.icon;
            resultIcon.color = new Color(1f, 1f, 1f, 1f); // Делаем непрозрачной
            resultIcon.enabled = true;
            
            // Если иконка сплющилась, можно раскомментировать эту строку:
            // resultIcon.preserveAspect = true; 
        }

        // 2. Настраиваем текст (имя предмета)
        if (resultNameText != null) 
        {
            resultNameText.text = recipe.recipeName; // Или recipe.outputResourceName
            resultNameText.enabled = true;
        }
    }

    public void ClearSlot()
    {
        // Прячем иконку (делаем прозрачной или выключаем)
        if (resultIcon != null)
        {
            resultIcon.sprite = null;
            resultIcon.color = new Color(1f, 1f, 1f, 0f); // Полная прозрачность
            resultIcon.enabled = false;
        }

        // Очищаем текст
        if (resultNameText != null) 
        {
            resultNameText.text = "";
            resultNameText.enabled = false;
        }
    }
}