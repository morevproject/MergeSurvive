using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Camera playerCamera;
    private Rigidbody rb; // Ссылка на физическое тело

    void Start()
    {
        // Кэшируем ссылку на Rigidbody при старте (это эффективнее)
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement: No Rigidbody found on Player!");
        }

        // Если камеру не назначили вручную, берём главную
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        // Если игра не в состоянии "Playing" (например, открыт инвентарь) - блокируем ввод
        if (GameStateManager.Instance.CurrentState != GameStateManager.GameState.Playing)
        {
            // Сбрасываем скорость, чтобы персонаж не скользил по инерции
            if (rb != null)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
            return;
        }
    
        // 1. Считываем ввод с клавиатуры (PC)
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // 2. ИНТЕГРАЦИЯ ДЖОЙСТИКА (Mobile)
        // Если на клавиатуре ничего не нажато И джойстик существует на сцене
        if (horizontalInput == 0 && verticalInput == 0 && FloatingJoystick.Instance != null)
        {
            horizontalInput = FloatingJoystick.Instance.Direction.x;
            verticalInput = FloatingJoystick.Instance.Direction.y;
        }

        if (playerCamera == null) return;

        // Расчёт направления относительно камеры
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * verticalInput) + (cameraRight * horizontalInput);
        
        // Нормализация нужна, чтобы по диагонали не бежал быстрее (но джойстик это обычно сам делает)
        if (moveDirection.magnitude > 1f) moveDirection.Normalize();

        // Применяем физическую силу (Velocity)
        if (moveDirection != Vector3.zero)
        {
            rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);
            
            // Опционально: поворот персонажа в сторону движения
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        else
        {
            // Если ввода нет — гасим горизонтальную скорость, оставляя вертикальную (гравитацию)
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }
}