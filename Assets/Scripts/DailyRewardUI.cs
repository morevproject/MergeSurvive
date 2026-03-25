using UnityEngine;
using UnityEngine.UI;

public class DailyRewardUI : MonoBehaviour
{
    [Header("Настройки Ленты")]
    public Transform contentPanel;     // Куда спавнить слоты (Content в ScrollView)
    public RewardSlotUI slotPrefab;    // Префаб нашей карточки
    public int daysToShowFuture = 10;  // Сколько дней вперед показывать

    [Header("UI Главного окна")]
    public Button claimButton;         // Кнопка "Забрать"

    [Header("UI Панель Штрафа (Прогул)")]
    public GameObject recoveryPanel;   // Всплывающее окно для прогульщиков
    public Button payButton;           // Кнопка "Оплатить гемами"
    public Button refuseButton;        // Кнопка "Сбросить прогресс"
    public Text costText;              // Текст с ценой спасения

    private void Start()
    {
        // Подписываем кнопки
        if (claimButton != null) claimButton.onClick.AddListener(OnClaimClicked);
        if (payButton != null) payButton.onClick.AddListener(OnPayClicked);
        if (refuseButton != null) refuseButton.onClick.AddListener(OnRefuseClicked);

        // Дублируем вызов здесь на случай, если OnEnable сработал слишком рано
        RefreshUI(); 
    }

    private void OnEnable()
    {
        // Проверяем, проснулся ли уже менеджер, прежде чем требовать у него данные
        if (DailyStreakManager.Instance != null)
        {
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        // 1. Очищаем старые слоты БЕЗ задержек
        foreach (Transform child in contentPanel)
        {
            child.gameObject.SetActive(false); // Прячем мгновенно, чтобы Layout Group о них забыл
            Destroy(child.gameObject);         // Уничтожаем в конце кадра
        }

        var manager = DailyStreakManager.Instance;
        int currentDay = manager.CurrentStreak;

        // --- НОВАЯ ЛОГИКА (Строго 5 карточек) ---
        int startDay = currentDay - 2;
        int endDay = currentDay + 2;

        // Если мы в самом начале (День 0 или 1), сдвигаем окно вправо
        if (startDay < 0)
        {
            endDay += Mathf.Abs(startDay); // Компенсируем недостающие дни спереди
            startDay = 0;
        }

        // 3. Генерируем карточки
        for (int i = startDay; i <= endDay; i++)
        {
            RewardSlotUI slot = Instantiate(slotPrefab, contentPanel);
            
            manager.GetRewardDataForDay(i, out ItemData item, out int amount, out bool isBig);

            int state = 2; // Будущее
            if (i < currentDay) state = 0; // Прошлое (Уже забрали)
            else if (i == currentDay && manager.CanClaimToday) state = 1; // Сегодня можно забрать
            else if (i == currentDay && !manager.CanClaimToday) state = 0; // Сегодня уже забрали

            slot.Setup(i, item, amount, isBig, state);
        }

        // 4. Настраиваем кнопки и панели
        if (manager.NeedsRecovery)
        {
            // Игрок прогулял! Включаем панель штрафа
            if (recoveryPanel != null) recoveryPanel.SetActive(true);
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (costText != null) costText.text = manager.rewardsDatabase.recoveryCost.ToString();
        }
        else
        {
            // Всё окей, прячем штраф
            if (recoveryPanel != null) recoveryPanel.SetActive(false);
            
            // Показываем кнопку "Забрать", только если есть что забирать
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(true);
                claimButton.interactable = manager.CanClaimToday;
            }
        }
    }

    // --- ОБРАБОТЧИКИ КНОПОК ---
    private void OnClaimClicked()
    {
        // 1. Выдаем награду в инвентарь
        DailyStreakManager.Instance.ClaimReward();
        
        // 2. Перерисовываем UI (кнопка мгновенно станет серой/исчезнет)
        RefreshUI(); 

        // 3. Ждем 1.5 секунды и закрываем окно (можешь поменять время)
        Invoke(nameof(CloseRewardWindow), 1.5f);
    }

    // Метод, который вызовется через 1.5 секунды
    private void CloseRewardWindow()
    {
        // Скрываем саму панель
        gameObject.SetActive(false);
    }

    private void OnPayClicked()
    {
        DailyStreakManager.Instance.PayToRecoverStreak();
        RefreshUI();
    }

    private void OnRefuseClicked()
    {
        DailyStreakManager.Instance.RefuseRecovery();
        RefreshUI();
    }
}