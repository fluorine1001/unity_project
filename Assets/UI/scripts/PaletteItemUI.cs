using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaletteItemUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text countText;

    public void Bind(TileDefinition def, int count)
    {
        if (icon) icon.sprite = def.icon;
        if (countText) countText.text = count.ToString();
    }
}
