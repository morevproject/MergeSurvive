using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;      // Ссылка на компонент Animator (на модельке)
    public Rigidbody rb;           // Ссылка на физику игрока

    // Константы, чтобы не ошибиться в буквах (должны совпадать с Parameters в Animator)
    private const string IS_MOVING = "IsMoving";
    private const string ATTACK = "Attack";
    private const string GATHER = "Gather";

    void Update()
    {
        if (animator == null || rb == null) return;

        // 1. ПРОВЕРКА БЕГА
        // Берем скорость, но игнорируем вертикальную (падение/прыжок), чтобы не сбивать анимацию
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        
        // Если скорость больше 0.1, считаем, что мы движемся
        bool isMoving = horizontalVelocity.magnitude > 0.1f;

        // Передаем это в Аниматор (включаем/выключаем стрелочки)
        animator.SetBool(IS_MOVING, isMoving);
    }

    // Этот метод будет вызывать наш CombatController при нажатии кнопки
    public void TriggerAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(ATTACK);
        }
    }

    public void TriggerGather()
    {
        if (animator != null)
        {
            animator.SetTrigger(GATHER);
        }
    }
}