using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{

    private List<EventInstance> eventInstances;

    private EventInstance musicEventInstance;

    public static AudioManager instance {get; private set;}

    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogError("Found more than one Audio Manager in the scene.");
        }
        instance = this;

        eventInstances = new List<EventInstance>();
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateInstance(EventReference EventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(EventReference);
        eventInstances.Add(eventInstance);

        return eventInstance;
    }

    private void CleanUp()
    {
        if (eventInstances != null)
        {
            foreach (EventInstance eventInstance in eventInstances)
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
            // 안전하게 리스트 비우기 (선택 사항)
            eventInstances.Clear(); 
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void Start()
    {
        if(StageManager.Instance.sceneIndex==1) InitializeMusic(FMODEvents.instance.Scene1Music);
        else if(StageManager.Instance.sceneIndex==2) InitializeMusic(FMODEvents.instance.Scene2Music);
    }

    private void InitializeMusic(EventReference musicEventReference)
    {
        musicEventInstance = CreateInstance(musicEventReference);
        musicEventInstance.start();
    }
}
