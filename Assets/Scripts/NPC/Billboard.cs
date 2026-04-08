using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        // Находим главную камеру на сцене
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    // Используем LateUpdate, чтобы поворот происходил ПОСЛЕ того, 
    // как отработают все скрипты движения камеры и персонажа
    void LateUpdate()
    {
        if (camTransform != null)
        {
            // Жестко привязываем поворот облачка к повороту камеры.
            // Теперь оно всегда будет идеально параллельно экрану!
            transform.rotation = camTransform.rotation;
        }
    }
}