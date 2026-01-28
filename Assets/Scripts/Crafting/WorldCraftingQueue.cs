using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WorldCraftingQueue : MonoBehaviour
{
    public static WorldCraftingQueue Instance;

    [Header("Queue Settings")]
    public int maxQueueSize = 3; 
    
    [Header("UI References (Визуал)")]
    public Transform queueUIContainer; 
    public GameObject queueItemPrefab; 

    public List<QueueItem> craftingQueue = new List<QueueItem>();
    private List<GameObject> uiObjects = new List<GameObject>();

    [System.Serializable]
    public class QueueItem
    {
        public Recipe recipe;
        public float duration;
        public float timeRemaining;
        
        public QueueItem(Recipe r)
        {
            recipe = r;
            duration = 5.0f; 
            timeRemaining = duration;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    public void AddToQueue(Recipe recipe)
    {
        if (craftingQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning("Очередь заполнена!");
            return;
        }

        QueueItem newItem = new QueueItem(recipe);
        craftingQueue.Add(newItem);
        
        CreateQueueUI(newItem);
        
        Debug.Log($"Добавлено в очередь: {recipe.recipeName}");
    }

    void Update()
    {
        if (craftingQueue.Count > 0)
        {
            var currentItem = craftingQueue[0];
            currentItem.timeRemaining -= Time.deltaTime;

            UpdateQueueUI(0, currentItem);

            if (currentItem.timeRemaining <= 0)
            {
                CompleteCrafting(currentItem);
            }
        }
    }

    void CompleteCrafting(QueueItem item)
    {
        // Выдача предмета
        ItemData resultItem = Resources.Load<ItemData>("Items/" + item.recipe.outputResourceName);
        if (resultItem == null) resultItem = Resources.Load<ItemData>(item.recipe.outputResourceName);

        if (resultItem != null)
        {
            InventoryManager.Instance.AddItem(resultItem, item.recipe.outputAmount);
            Debug.Log($"<color=green>Крафт завершен: {resultItem.ItemName}</color>");
        }
        else
        {
            Debug.LogError($"Ошибка крафта: Не найден предмет '{item.recipe.outputResourceName}'");
        }

        craftingQueue.RemoveAt(0);

        if (uiObjects.Count > 0)
        {
            Destroy(uiObjects[0]);
            uiObjects.RemoveAt(0);
        }
    }

    // --- ВИЗУАЛИЗАЦИЯ ---

    void CreateQueueUI(QueueItem item)
    {
        if (queueUIContainer == null || queueItemPrefab == null) return;

        GameObject obj = Instantiate(queueItemPrefab, queueUIContainer);
        uiObjects.Add(obj);

        // 1. НАСТРОЙКА КОЛИЧЕСТВА (ИСПРАВЛЕНИЕ)
        var slotUI = obj.GetComponent<ResourceSlotUI>();
        if (slotUI != null)
        {
            // Используем метод из скрипта слота (он сам разберется с цветом и видимостью)
            slotUI.UpdateAmount(item.recipe.outputAmount);
        }
        else
        {
            // Fallback: ищем текст вручную, если скрипта нет
            var txt = obj.GetComponentInChildren<Text>();
            if (txt != null) txt.text = item.recipe.outputAmount.ToString();
        }

        // 2. НАСТРОЙКА ИКОНКИ (ТАЙМЕР)
        Image icon = null;

        // Ищем иконку (сначала вложенную, потом компонент, потом любую)
        Transform iconTr = obj.transform.Find("Icon");
        if (iconTr != null) icon = iconTr.GetComponent<Image>();
        
        if (icon == null && slotUI != null) icon = slotUI.iconImage;
        if (icon == null) icon = obj.GetComponent<Image>();

        // Применяем спрайт и включаем режим заполнения
        if (icon != null && item.recipe.icon != null)
        {
            icon.sprite = item.recipe.icon;
            icon.enabled = true;
            
            icon.type = Image.Type.Filled; 
            icon.fillMethod = Image.FillMethod.Radial360;
            icon.fillAmount = 1f; 
        }
    }

    void UpdateQueueUI(int index, QueueItem item)
    {
        if (index >= uiObjects.Count) return;

        GameObject obj = uiObjects[index];
        if (obj == null) return;

        // Ищем ту же иконку для обновления таймера
        Image icon = null;
        Transform iconTr = obj.transform.Find("Icon");
        if (iconTr != null) icon = iconTr.GetComponent<Image>();
        
        if (icon == null)
        {
             var slotUI = obj.GetComponent<ResourceSlotUI>();
             if (slotUI != null) icon = slotUI.iconImage;
        }

        if (icon != null)
        {
            float progress = item.timeRemaining / item.duration;
            icon.fillAmount = progress;
        }
    }
}