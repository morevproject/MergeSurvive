using UnityEngine;
using System.Collections;

public class CombatProjectile : MonoBehaviour
{
    // Метод, который запускает полет
    public void Launch(Vector3 targetPos, float duration)
    {
        StartCoroutine(FlyRoutine(targetPos, duration));
    }

    IEnumerator FlyRoutine(Vector3 target, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0;

        // Поворачиваем нос стрелы к цели
        transform.LookAt(target);

        while (elapsed < duration)
        {
            // Lerp обеспечивает плавное перемещение от А к Б
            transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Гарантируем, что в конце мы точно в цели
        transform.position = target;
        
        // Уничтожаем стрелу, чтобы не засорять сцену
        Destroy(gameObject); 
    }
}