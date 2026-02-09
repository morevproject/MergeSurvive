using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [Header("Настройки парения")]
    public float amplitude = 0.5f; // Насколько сильно двигается вверх-вниз
    public float frequency = 1f;   // Как быстро двигается

    [Header("Вращение (опционально)")]
    public float rotationSpeed = 0f; // Можно поставить 10-20, если хочешь, чтобы он медленно крутился

    private Vector3 startPos;

    void Start()
    {
        // Запоминаем локальную позицию (относительно родителя)
        startPos = transform.localPosition;
    }

    void Update()
    {
        // 1. Парение (Синусоида)
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        // 2. Вращение (если нужно)
        if (rotationSpeed != 0)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}