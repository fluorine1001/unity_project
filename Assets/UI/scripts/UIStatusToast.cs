using UnityEngine;
using TMPro;

public class UIStatusToast : MonoBehaviour
{
    public GameObject statusRoot;
    public TextMeshProUGUI statusText;
    public float showSeconds = 2f;

    void Awake()
    {
        HideImmediate();
    }

    public void Show(string msg)
    {
        if (statusRoot == null || statusText == null) return;

        statusText.text = msg;
        statusRoot.SetActive(true);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), showSeconds);
    }

    void Hide()
    {
        if (statusRoot == null) return;
        statusRoot.SetActive(false);
    }

    public void HideImmediate()
    {
        if (statusRoot != null) statusRoot.SetActive(false);
    }
}
