using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public Camera playerCamera;

    [Header("Визуал (Матрешка)")]
    public Transform characterModel; 

    // === НОВЫЙ РУБИЛЬНИК ДЛЯ БЛОКИРОВКИ ===
    [HideInInspector] public bool canMove = true; 

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("PlayerMovement: No Rigidbody found!");
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Update()
    {
        // 1. Блокировка от интерфейса (открыт инвентарь и т.д.)
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameStateManager.GameState.Playing)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        // === 2. БЛОКИРОВКА ОТ АНИМАЦИИ (удар, сбор лута) ===
        if (!canMove)
        {
            // Тормозим персонажа, чтобы он не скользил по инерции
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }
    
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        if (horizontalInput == 0 && verticalInput == 0 && FloatingJoystick.Instance != null)
        {
            horizontalInput = FloatingJoystick.Instance.Direction.x;
            verticalInput = FloatingJoystick.Instance.Direction.y;
        }

        if (playerCamera == null) return;

        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * verticalInput) + (cameraRight * horizontalInput);
        
        if (moveDirection.magnitude > 1f) moveDirection.Normalize();

        if (moveDirection != Vector3.zero)
        {
            rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);
            
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            if (characterModel != null)
            {
                // Оставили резкое вращение 30f!
                characterModel.rotation = Quaternion.Slerp(characterModel.rotation, targetRotation, Time.deltaTime * 30f);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30f);
            }
        }
        else
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }
}