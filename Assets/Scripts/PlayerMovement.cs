using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public Camera playerCamera;

    [Header("Визуал (Матрешка)")]
    public Transform characterModel; 

    [HideInInspector] public bool canMove = true; 

    private Rigidbody rb;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("PlayerMovement: No Rigidbody found!");
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameStateManager.GameState.Playing)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (!canMove)
        {
            moveDirection = Vector3.zero;
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

        moveDirection = (cameraForward * verticalInput) + (cameraRight * horizontalInput);
        if (moveDirection.magnitude > 1f) moveDirection.Normalize();

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            if (characterModel != null)
            {
                characterModel.rotation = Quaternion.Slerp(characterModel.rotation, targetRotation, Time.deltaTime * 30f);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30f);
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Прямая передача скорости. Если уперлись в Box Collider - физика сама сбросит её в 0.
        rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);
    }
}