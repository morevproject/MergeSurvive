using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerCombatController combatController;

    void Awake()
    {
        // При старте ищем скрипт боёвки на родительском объекте (Player_Main)
        combatController = GetComponentInParent<PlayerCombatController>();
    }

    // Этот метод поймает сигнал от Аниматора (название должно совпадать 1 в 1!)
    public void ExecuteAttackLogic()
    {
        if (combatController != null)
        {
            // Передаем сигнал настоящему скрипту!
            combatController.ExecuteAttackLogic();
        }
        else
        {
            Debug.LogWarning("AnimationEventRelay: Не могу найти PlayerCombatController на родителе!");
        }
    }
}