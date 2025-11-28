using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{

    [field: Header("Music")]
    [field: SerializeField] public EventReference Scene1Music {get; private set;}

    [field: Header("SFX")]
    [field: SerializeField] public EventReference BulletLaunched {get; private set;}
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
