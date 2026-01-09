using UnityEngine;

public class MenuActions : MonoBehaviour
{
    public UIStatusToast toast;

    public void OnSave()
    {
        if (SaveSystem.Instance == null) return;

        SaveSystem.Instance.Save(0);
        if (toast != null) toast.Show("Saved complete");
    }
}
