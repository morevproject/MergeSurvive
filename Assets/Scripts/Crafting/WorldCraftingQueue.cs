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
            duration = 5.0f; // Можно сделать зависимым от рецепта
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
            // Берем первый элемент
            var currentItem = craftingQueue[0];
            currentItem.timeRemaining -= Time.deltaTime;

            // Обновляем визуал (таймер)
            UpdateQueueUI(0, currentItem);

            if (currentItem.timeRemaining <= 0)
            {
                CompleteCrafting(currentItem);
            }
        }
    }

    void CompleteCrafting(QueueItem item)
    {
        // Загружаем предмет для выдачи
        ItemData resultItem = LoadItemData(item.recipe.outputResourceName);

        if (resultItem != null)
        {
            InventoryManager.Instance.AddItem(resultItem, item.recipe.outputAmount);
            Debug.Log($"<color=green>Крафт завершен: {resultItem.ItemName}</color>");
        }
        else
        {
            Debug.LogError($"Ошибка крафта: Не найден предмет '{item.recipe.outputResourceName}'");
        }

        // Удаляем из данных
        craftingQueue.RemoveAt(0);

        // Удаляем из UI
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

        // --- ВАЖНОЕ ДОБАВЛЕНИЕ: СБРОС ТРАНСФОРМА ---
        // Чтобы объект не улетал в космос и имел нормальный размер
        obj.transform.localScale = Vector3.one; 
        obj.transform.localPosition = Vector3.zero;
        // -------------------------------------------

        // Пытаемся найти слот UI скрипт
        var slotUI = obj.GetComponent<ResourceSlotUI>();
        
        // 1. НАХОДИМ ИКОНКУ (через загрузку Result Item)
        ItemData resultItem = LoadItemData(item.recipe.outputResourceName);
        Sprite correctIcon = (resultItem != null) ? resultItem.Icon : null;

        // Если в ItemData иконки нет, пробуем взять из рецепта (резерв)
        if (correctIcon == null) correctIcon = item.recipe.icon;

        // 2. ПРИМЕНЯЕМ ДАННЫЕ В СЛОТ
        if (slotUI != null)
        {
            // Используем штатный метод обновления слота
            slotUI.UpdateSlot(item.recipe.recipeName, correctIcon, item.recipe.outputAmount, null);
            
            // Настраиваем иконку под таймер (Radial Fill)
            if (slotUI.iconImage != null)
            {
                slotUI.iconImage.type = Image.Type.Filled;
                slotUI.iconImage.fillMethod = Image.FillMethod.Radial360;
                slotUI.iconImage.fillOrigin = (int)Image.Origin360.Top;
                slotUI.iconImage.fillAmount = 1f; 
            }
        }
        else
        {
            // Fallback: Ручная настройка
            Transform iconTr = obj.transform.Find("Icon");
            Image icon = (iconTr != null) ? iconTr.GetComponent<Image>() : obj.GetComponent<Image>();

            if (icon != null && correctIcon != null)
            {
                icon.sprite = correctIcon;
                icon.enabled = true;
                icon.type = Image.Type.Filled; 
                icon.fillMethod = Image.FillMethod.Radial360;
                icon.fillAmount = 1f; 
                
                // Убедимся, что цвет белый (не прозрачный)
                icon.color = Color.white; 
            }

            var txt = obj.GetComponentInChildren<Text>();
            if (txt != null) txt.text = item.recipe.outputAmount.ToString();
        }
    }

    void UpdateQueueUI(int index, QueueItem item)
    {
        if (index >= uiObjects.Count) return;

        GameObject obj = uiObjects[index];
        if (obj == null) return;

        // Ищем иконку для обновления заливки
        Image icon = null;
        
        var slotUI = obj.GetComponent<ResourceSlotUI>();
        if (slotUI != null) 
        {
            icon = slotUI.iconImage;
        }
        else
        {
            Transform iconTr = obj.transform.Find("Icon");
            if (iconTr != null) icon = iconTr.GetComponent<Image>();
            if (icon == null) icon = obj.GetComponent<Image>();
        }

        if (icon != null)
        {
            // Прогресс убывает (1.0 -> 0.0)
            float progress = item.timeRemaining / item.duration;
            icon.fillAmount = progress;
        }
    }

    // Вспомогательный метод загрузки
    private ItemData LoadItemData(string itemName)
    {
        ItemData item = Resources.Load<ItemData>("Items/" + itemName);
        if (item == null) item = Resources.Load<ItemData>(itemName);
        return item;
    }
}