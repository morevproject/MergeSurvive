using UnityEngine;
using System;

public class WorldObjectID : MonoBehaviour
{
    [Header("Паспорт объекта")]
    public string id;

    // ИЗМЕНЕНИЕ: Было Awake, стало Start
    // Теперь мы ждем, пока SaveManager точно загрузит файл в своем Awake
    private void Start()
    {
        if (SaveManager.Instance.IsObjectDestroyed(id))
        {
            gameObject.SetActive(false); 
        }
    }

    public void RegisterDestruction()
    {
        SaveManager.Instance.MarkAsDestroyed(id);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }
    
    [ContextMenu("Generate New ID")]
    private void GenerateID()
    {
        id = Guid.NewGuid().ToString();
    }
#endif
}