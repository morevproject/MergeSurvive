using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Vector3 offset = new Vector3(0, 2.5f, 0); // ¬ысота над головой

    public void Setup(float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }

    public void UpdateHealth(float currentHealth)
    {
        slider.value = currentHealth;
    }

    void LateUpdate() // LateUpdate чтобы не дергалось при движении камеры
    {
        // ѕоворот к камере
        transform.rotation = Camera.main.transform.rotation;
        // ѕозици€ над родителем (если бар не дочерний, а просто следует)
        // Ќо мы сделаем его дочерним, так что просто поворот.
    }
}