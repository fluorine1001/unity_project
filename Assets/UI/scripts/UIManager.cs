using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager:MonoBehaviour
{
    public GameObject Panel;
    void Start()
    {
        if(Panel!=null)
        {
            Panel.SetActive(false);
        }
    }
    public void bookPanel(bool newState)
    {
        if(Panel!=null)
        {
            Panel.SetActive(newState);
           
        }
        if(newState==false)
        {
             EventSystem.current.SetSelectedGameObject(null);
        }
    }
    

}
