using UnityEngine;
using System;

public class DailyStreakManager : MonoBehaviour
{
    public static DailyStreakManager Instance;

    [Header("Настройки")]
    public DailyRewardsSO rewardsDatabase;

    [Header("UI References")]
    public GameObject dailyRewardWindow; // Перетащим сюда нашу Panel_DailyRewards

    // --- Ключи для сохранений (PlayerPrefs) ---
    private const string STREAK_KEY = "Player_DailyStreak";
    private const string LAST_LOGIN_KEY = "Player_LastLoginDate";

    // --- Состояния ---
    public int CurrentStreak { get; private set; } // Текущий день (0, 1, 2, 3...)
    public bool CanClaimToday { get; private set; }
    public bool NeedsRecovery { get; private set; }

    private void Awake()
    {
        Instance = this;
        LoadData();
        CheckLoginStatus();
    }

    private void Start()
    {
        // Если сегодня есть награда ИЛИ игрок в штрафниках -> показываем окно
        if (CanClaimToday || NeedsRecovery)
        {
            if (dailyRewardWindow != null)
            {
                dailyRewardWindow.SetActive(true);
            }
        }
    }

    private void LoadData()
    {
        CurrentStreak = PlayerPrefs.GetInt(STREAK_KEY, 0);
    }

    // --- ЛОГИКА ВРЕМЕНИ И СТРИКА ---
    private void CheckLoginStatus()
    {
        string lastLoginStr = PlayerPrefs.GetString(LAST_LOGIN_KEY, "");
        DateTime today = DateTime.Now.Date; // Берем только день, без часов/минут

        if (string.IsNullOrEmpty(lastLoginStr))
        {
            // Самый первый вход в игру
            CanClaimToday = true;
            NeedsRecovery = false;
            return;
        }

        if (DateTime.TryParse(lastLoginStr, out DateTime lastLoginDate))
        {
            int daysMissed = (today - lastLoginDate).Days;

            if (daysMissed == 0)
            {
                // Сегодня уже заходил и забрал
                CanClaimToday = false;
                NeedsRecovery = false;
            }
            else if (daysMissed == 1)
            {
                // Идеальный стрик! Зашел на следующий день
                CanClaimToday = true;
                NeedsRecovery = false;
            }
            else
            {
                // Прогульщик! Пропущено 2 или более дней
                CanClaimToday = false; 
                NeedsRecovery = true; // Триггерим окно спасения стрика
            }
        }
    }

    // --- МАТЕМАТИКА НАГРАД ---
    public void GetRewardDataForDay(int dayIndex, out ItemData item, out int finalAmount, out bool isBig)
    {
        int totalDaysInCycle = rewardsDatabase.rewards.Count; // 28
        
        // Находим индекс в массиве (0-27) с помощью остатка от деления
        int cycleIndex = dayIndex % totalDaysInCycle;
        
        // Считаем множитель (начинается с 1). Каждые 28 дней прибавляет +1.
        int multiplier = (dayIndex / totalDaysInCycle) + 1;
        
        // Ограничиваем множитель капом из настроек
        multiplier = Mathf.Min(multiplier, rewardsDatabase.maxMultiplier);

        var rewardDef = rewardsDatabase.rewards[cycleIndex];
        
        item = rewardDef.item;
        finalAmount = rewardDef.baseAmount * multiplier;
        isBig = rewardDef.isBigReward;
    }

    // --- ДЕЙСТВИЯ ИГРОКА ---
    public void ClaimReward()
    {
        if (!CanClaimToday) return;

        // Получаем данные награды для ТЕКУЩЕГО дня
        GetRewardDataForDay(CurrentStreak, out ItemData item, out int amount, out bool isBig);

        // Выдаем игроку
        InventoryManager.Instance.AddItem(item, amount);
        Debug.Log($"<color=green>Дейли забрано! День {CurrentStreak + 1}. Получено {item.ItemName} x{amount}</color>");

        // Двигаем стрик вперед и сохраняем сегодняшний день
        CurrentStreak++;
        PlayerPrefs.SetInt(STREAK_KEY, CurrentStreak);
        PlayerPrefs.SetString(LAST_LOGIN_KEY, DateTime.Now.Date.ToString());
        PlayerPrefs.Save();

        CanClaimToday = false;
    }

    // Кнопка: "Заплатить Х валюты, чтобы спасти стрик"
    public void PayToRecoverStreak()
    {
        int cost = rewardsDatabase.recoveryCost;
        
        // Пытаемся списать хард-валюту через наш новый менеджер
        if (CurrencyManager.Instance.SpendGems(cost))
        {
            // Успех! Прощаем! Делаем вид, что он заходил вчера.
            PlayerPrefs.SetString(LAST_LOGIN_KEY, DateTime.Now.AddDays(-1).Date.ToString());
            PlayerPrefs.Save();

            NeedsRecovery = false;
            CanClaimToday = true;
            Debug.Log("<color=yellow>Стрик спасен за Гемы!</color>");
        }
        else
        {
            // Денег нет
            Debug.Log("<color=red>Не хватает гемов для восстановления!</color>");
            // Тут потом можно вызвать попап "Купить гемы в магазине?"
        }
    }

    // Кнопка: "Отказаться и сбросить прогресс" (или крестик на попапе восстановления)
    public void RefuseRecovery()
    {
        // Жесткий сброс на День 1!
        CurrentStreak = 0;
        PlayerPrefs.SetInt(STREAK_KEY, CurrentStreak);
        
        // Делаем вид, что он заходил "вчера", чтобы сегодня он смог забрать награду 1-го дня
        PlayerPrefs.SetString(LAST_LOGIN_KEY, DateTime.Now.AddDays(-1).Date.ToString());
        PlayerPrefs.Save();

        NeedsRecovery = false;
        CanClaimToday = true;
        Debug.Log("<color=red>Стрик сброшен до нуля!</color>");
    }
}