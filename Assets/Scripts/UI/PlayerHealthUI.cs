using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public Slider hpSlider; // Ссылка на слайдер в UI
    
    // Мы найдем игрока автоматически, чтобы не тянуть ссылки каждый раз
    private FighterStats playerStats;

    void Start()
    {
        // 1. Ищем игрока по тегу
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            playerStats = player.GetComponent<FighterStats>();
        }

        // 2. Если нашли — подписываемся
        if (playerStats != null)
        {
            // Настраиваем максимум слайдера
            hpSlider.maxValue = playerStats.maxHealth;
            hpSlider.value = playerStats.currentHealth;

            // Подписываемся на событие изменения здоровья
            playerStats.OnHealthChanged += UpdateHealth;
        }
        else
        {
            Debug.LogError("PlayerHealthUI: Не найден объект с тегом 'Player' или на нем нет FighterStats!");
        }
    }

    void UpdateHealth(int currentHealth)
    {
        hpSlider.value = currentHealth;
    }

    // Очень важно отписываться при уничтожении, чтобы не было ошибок
    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealth;
        }
    }
}