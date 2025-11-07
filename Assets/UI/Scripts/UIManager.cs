using UnityEngine;

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
    public void TogglePanel(bool isOn)
    {
        Panel.SetActive(isOn);
    }
}
