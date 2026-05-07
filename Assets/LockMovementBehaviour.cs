using UnityEngine;

public class LockMovementBehaviour : StateMachineBehaviour
{
    // Срабатывает в самую первую миллисекунду, когда анимация началась
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerMovement pm = animator.GetComponent<PlayerMovement>();
        if (pm != null) pm.canMove = false;
    }

    // Срабатывает, когда анимация полностью закончилась и произошел переход
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerMovement pm = animator.GetComponent<PlayerMovement>();
        if (pm != null) pm.canMove = true;
    }
}