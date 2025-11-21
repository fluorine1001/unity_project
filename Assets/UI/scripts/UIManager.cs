using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager:MonoBehaviour
{
    public GameObject Panel;
    public bool IsPanelOpen { get; private set; }
    void Start()
    {
        if(Panel!=null)
        {
            Panel.SetActive(false);
        }
        IsPanelOpen = false;
    }
    public void bookPanel(bool newState)
    {
        if (Panel != null)
        {
            Panel.SetActive(newState);
        }
        IsPanelOpen = newState;
        if (newState == false)
        {
             EventSystem.current.SetSelectedGameObject(null);
        }
    }
    

}
