using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Цель, за которой следует камера (наш Player)
    public Vector3 offset; // Смещение камеры относительно цели. Будем считать его КОНСТАНТНЫМ.

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: Target not assigned!");
            return;
        }

        // Всё просто: позиция камеры = позиция игрока + фиксированное смещение.
        // Это гарантирует, что камера будет двигаться ровно так же, как игрок,
        // сохраняя один и тот же угол и расстояние.
        transform.position = target.position + offset;
    }
}