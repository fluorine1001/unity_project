using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Bullet Launch SFX")]
    [field: SerializeField] public EventReference BulletLaunched {get; private set;}

    [field: Header("Dash SFX")]
    [field: SerializeField] public EventReference PlayerDash {get; private set;}

    public static FMODEvents instance {get; private set;}

    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogError("Found more than one FMOD Events instance in the scene.");
        }
        instance = this;
    }
}
