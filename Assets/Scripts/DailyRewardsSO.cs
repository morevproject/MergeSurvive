using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDailyRewards", menuName = "Game/Daily Rewards Setup")]
public class DailyRewardsSO : ScriptableObject
{
    [System.Serializable]
    public class DailyReward
    {
        public ItemData item;
        public int baseAmount = 1;
        [Tooltip("Особая награда (веха 7, 14, 21, 28 дней) - для подсветки в UI")]
        public bool isBigReward = false;
    }

    [Header("Цикл наград (строго 28 элементов)")]
    public List<DailyReward> rewards = new List<DailyReward>();

    [Header("Настройки экономики")]
    [Tooltip("Максимальный множитель, чтобы не сломать баланс в лейтгейме (например, 5)")]
    public int maxMultiplier = 5;

    [Tooltip("Сколько стоит прощение пропуска (в премиум валюте)")]
    public int recoveryCost = 50;
}