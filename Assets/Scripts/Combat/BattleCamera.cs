using UnityEngine;

public class BattleCamera : MonoBehaviour
{
    public static BattleCamera Instance; // Чтобы легко найти из CombatManager

    [Header("Settings")]
    public float zoomSpeed = 2f;   // Как быстро камера отъезжает
    public float battleZoom = 70f; // Значение FOV для боя (чем больше, тем дальше)
    
    // Для Ортографической камеры (если используешь её)
    public float battleOrthoSize = 8f; 

    private Camera cam;
    private float defaultFOV;
    private float defaultOrthoSize;
    private bool isBattleMode = false;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        
        // Запоминаем стартовые значения
        defaultFOV = cam.fieldOfView;
        defaultOrthoSize = cam.orthographicSize;
    }

    void Update()
    {
        // Выбираем целевое значение в зависимости от режима
        if (cam.orthographic)
        {
            float targetSize = isBattleMode ? battleOrthoSize : defaultOrthoSize;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
        }
        else
        {
            float targetFOV = isBattleMode ? battleZoom : defaultFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }

    // Эти методы мы вызовем из CombatManager
    public void SetBattleZoom(bool active)
    {
        isBattleMode = active;
    }
}