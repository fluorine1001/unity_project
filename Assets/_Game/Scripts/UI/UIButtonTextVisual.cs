using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonTextVisual : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    ISelectHandler, IDeselectHandler
{
    public TMP_Text label;
    public UIButtonTextGroup group;
    public int index;

    void Reset()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
    }

    void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
    }

    public void Apply(bool selected, bool hovered)
    {
        if (label == null || group == null) return;

        if (selected) label.color = group.selectedColor;
        else if (hovered) label.color = group.hoverColor;
        else label.color = group.normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //if (group != null) group.SetHover(index, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (group != null) group.SetHover(index, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (group != null) group.Select(index);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (group != null) group.Select(index);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (group != null) group.SetHover(index, false);
    }
}
