using TMPro;
using UnityEngine;
using System.Collections;

public class UIStatusToast : MonoBehaviour
{
    public TMP_Text text;
    public float showSeconds = 2f;
    Coroutine co;

    void Awake()
    {
        if (text != null)
        {
            text.gameObject.SetActive(false);
            
            // ✅ [수정됨] 토스트 메시지가 게임 조작을 방해하지 않도록 설정
            text.raycastTarget = false; 
        }
    }

    public void Show(string msg)
    {
        if (text == null) return;

        text.text = msg;
        text.gameObject.SetActive(true);

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(HideAfter());
    }

    IEnumerator HideAfter()
    {
        yield return new WaitForSecondsRealtime(showSeconds);
        if (text != null) text.gameObject.SetActive(false);
        co = null;
    }
}