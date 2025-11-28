using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveGame
{
    public List<TransformRecord> transforms = new List<TransformRecord>();
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, "save_slot_" + slot + ".json");
    }

    public void Save(int slot)
    {
        SaveGame data = new SaveGame();

        SaveableTransform[] targets = FindObjectsOfType<SaveableTransform>(true);
        for (int i = 0; i < targets.Length; i++)
        {
            if (string.IsNullOrEmpty(targets[i].saveId)) continue;
            data.transforms.Add(targets[i].Capture());
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slot), json);

        Debug.Log("Saved to: " + GetPath(slot));
    }
}
