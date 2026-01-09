using UnityEngine;

public class UIButtonTextGroup : MonoBehaviour
{
    public UIButtonTextVisual[] buttons;

    public Color normalColor = Color.black;
    public Color hoverColor = new Color(0.2f, 0.7f, 1f, 1f);
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);

    int selectedIndex = -1;
    bool[] hovered;

    void Awake()
    {
        hovered = new bool[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].index = i;
            buttons[i].group = this;
        }

        RefreshAll();
    }

    public void Select(int idx)
    {
        selectedIndex = idx;
        RefreshAll();
    }

    public void SetHover(int idx, bool isHover)
    {
        if (idx < 0 || idx >= hovered.Length) return;
        hovered[idx] = isHover;
        RefreshAll();
    }

    void RefreshAll()
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].Apply(i == selectedIndex, hovered[i]);
    }
}
