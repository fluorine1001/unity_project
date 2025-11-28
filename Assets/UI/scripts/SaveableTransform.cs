using System;
using UnityEngine;

[Serializable]
public class TransformRecord
{
    public string id;
    public Vector3 position;
    public Quaternion rotation;
    public bool active;
}

public class SaveableTransform : MonoBehaviour
{
    public string saveId;

    public TransformRecord Capture()
    {
        TransformRecord r = new TransformRecord();
        r.id = saveId;
        r.position = transform.position;
        r.rotation = transform.rotation;
        r.active = gameObject.activeSelf;
        return r;
    }
}
