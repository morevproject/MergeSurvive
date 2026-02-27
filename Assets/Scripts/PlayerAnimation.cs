using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;      
    public Rigidbody rb;           

    private const string IS_MOVING = "isMoving";
    private const string GATHER = "doGather";
    private const string ATTACK = "doAttack"; 
    private const string SHOOT = "doShoot";

    void Update()
    {
        if (animator == null || rb == null) return;

        // Просто смотрим на реальную скорость Rigidbody.
        // Box Collider жестко остановит капсулу, скорость упадет, анимация выключится.
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        bool isMoving = horizontalVelocity.magnitude > 0.1f;

        animator.SetBool(IS_MOVING, isMoving);
    }

    public void TriggerGather()
    {
        if (animator != null) animator.SetTrigger(GATHER);
    }

    public void TriggerAttack()
    {
        if (animator != null) animator.SetTrigger(ATTACK);
    }

    public void TriggerShoot()
    {
        if (animator != null) animator.SetTrigger(SHOOT);
    }
}