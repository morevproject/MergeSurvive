using UnityEngine;
using UnityEngine.UI; // Для обычного Text (если используешь TextMeshPro, добавь using TMPro;)

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("UI")]
    [Tooltip("Перетащи сюда текстовый объект с экрана, который показывает гемы")]
    public Text gemsText; 

    // Текущий баланс гемов
    public int Gems { get; private set; }

    private const string GEMS_SAVE_KEY = "Player_HardCurrency";

    private void Awake()
    {
        Instance = this;
        LoadGems();
    }

    // Добавить гемы (за донат, квесты или из нашего Сундука/Рулетки!)
    public void AddGems(int amount)
    {
        Gems += amount;
        SaveGems();
        UpdateUI();
        Debug.Log($"<color=cyan>Получено гемов: {amount}. Баланс: {Gems}</color>");
    }

    // Потратить гемы (возвращает true, если покупка успешна)
    public bool SpendGems(int amount)
    {
        if (Gems >= amount)
        {
            Gems -= amount;
            SaveGems();
            UpdateUI();
            return true;
        }
        
        Debug.LogWarning("Недостаточно гемов!");
        return false;
    }

    private void SaveGems()
    {
        PlayerPrefs.SetInt(GEMS_SAVE_KEY, Gems);
        PlayerPrefs.Save();
    }

    private void LoadGems()
    {
        // Выдаем 100 гемов при самом первом запуске игры (стартовый капитал)
        Gems = PlayerPrefs.GetInt(GEMS_SAVE_KEY, 100); 
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (gemsText != null)
        {
            gemsText.text = Gems.ToString();
        }
    }
}