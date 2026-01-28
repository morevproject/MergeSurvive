using UnityEngine;

public class CombatEvents : MonoBehaviour
{
    private PlayerCombatController combatController;

    void Start()
    {
        // Ищем контроллер боя у Папы (в родительском объекте)
        combatController = GetComponentInParent<PlayerCombatController>();
    }

    // Этот метод мы вызовем прямо из Анимации
    public void OnAnimationAttackHit()
    {
        if (combatController != null)
        {
            combatController.ExecuteAttackLogic();
        }
    }
}