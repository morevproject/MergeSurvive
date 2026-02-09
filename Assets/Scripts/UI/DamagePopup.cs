using UnityEngine;
using UnityEngine.UI;
using TMPro; // Если используешь TextMeshPro (рекомендую), иначе убери и используй Text

public class DamagePopup : MonoBehaviour
{
    // Ссылка на префаб (заполним в инспекторе, но через менеджер было бы сложнее)
    // Для простоты сделаем через Resources.Load
    
    public TextMeshPro textMesh; // Или public Text text; если обычный UI
    public float disappearTimer = 1f;
    public float moveYSpeed = 2f;
    public Color damageColor = Color.red;
    public Color healColor = Color.green;

    private Color textColor;
    private static GameObject prefab;

    // Статический метод для создания
    public static void Create(Vector3 position, int amount, bool isCritical = false)
    {
        if (prefab == null) prefab = Resources.Load<GameObject>("DamagePopup"); // Префаб должен лежать в папке Resources!
        
        GameObject popup = Instantiate(prefab, position, Quaternion.identity);
        DamagePopup script = popup.GetComponent<DamagePopup>();
        script.Setup(amount, isCritical);
    }

    public void Setup(int amount, bool isCritical)
    {
        textMesh.text = amount.ToString();
        
        if (isCritical)
        {
            textMesh.fontSize += 2;
            textColor = Color.yellow;
        }
        else
        {
            textColor = damageColor;
        }
        
        textMesh.color = textColor;
        
        // Поворот к камере (чтобы цифры всегда смотрели на игрока)
        transform.forward = Camera.main.transform.forward;
    }

    void Update()
    {
        // 1. Движение вверх
        transform.position += new Vector3(0, moveYSpeed * Time.deltaTime, 0);

        // 2. Исчезновение
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}