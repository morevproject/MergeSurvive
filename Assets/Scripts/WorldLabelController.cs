using UnityEngine;
using UnityEngine.UI;

public class WorldLabelController : MonoBehaviour
{
    [Header("UI Reference")]
    public Text nameText; // СЮДА нужно перетащить компонент Text

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0); // Высота над головой объекта

    private Transform targetTransform;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        
        // Если камеры нет, пытаемся найти по тегу (на всякий случай)
        if (mainCam == null) mainCam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();

        // Сразу выключаем лейбл, чтобы он не висел посередине экрана
        gameObject.SetActive(false);
    }

    public void Show(Transform target, string text)
    {
        if (nameText == null)
        {
            Debug.LogError("WorldLabel: Не назначен компонент Text в инспекторе!");
            return;
        }

        targetTransform = target;
        nameText.text = text;
        
        gameObject.SetActive(true);
        
        // Сразу обновляем позицию, чтобы не было мерцания в центре экрана
        UpdatePosition();
    }

    public void Hide()
    {
        targetTransform = null;
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        if (targetTransform == null || mainCam == null)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }

        // Переводим координаты из 3D мира в 2D экран
        Vector3 screenPos = mainCam.WorldToScreenPoint(targetTransform.position + offset);

        // Проверяем, что объект перед камерой (Z > 0), иначе текст будет перевернут
        if (screenPos.z > 0)
        {
            transform.position = screenPos;
        }
        else
        {
            // Если объект за спиной — прячем текст (опционально)
            // Но обычно достаточно просто не обновлять позицию
        }
    }
}