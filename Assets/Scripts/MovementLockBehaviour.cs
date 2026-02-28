using UnityEngine;

// Обрати внимание: он наследуется не от MonoBehaviour, а от StateMachineBehaviour!
public class MovementLockBehaviour : StateMachineBehaviour
{
    private PlayerMovement playerMovement;

    // Срабатывает в первый кадр, когда Аниматор заходит в это состояние
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Ищем наш скрипт движения на родительском объекте
        if (playerMovement == null)
        {
            playerMovement = animator.GetComponentInParent<PlayerMovement>();
        }
        
        // Дергаем рубильник: двигаться НЕЛЬЗЯ
        if (playerMovement != null)
        {
            playerMovement.canMove = false;
        }
    }

    // Срабатывает в последний кадр, когда Аниматор выходит из этого состояния
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Возвращаем рубильник: двигаться МОЖНО
        if (playerMovement != null)
        {
            playerMovement.canMove = true;
        }
    }
}