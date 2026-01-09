using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // New Input System 필수
using System.Collections; // 코루틴을 위해 필요

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button[] buttons; // Play, Credit, Quit 순서대로
    public RectTransform cursor; // 파란 삼각형
    public Vector3 cursorOffset = new Vector3(-150, 0, 0); // 위치가 이상하면 이 값을 조절

    private int currentIndex = 0;

    // void Start 대신 IEnumerator Start를 사용하여 한 프레임 대기합니다.
    IEnumerator Start()
    {
        // UI 배치가 완전히 끝날 때까지 한 프레임 기다립니다 (시작 위치 오류 해결)
        yield return null;
        UpdateCursorPosition();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. 위쪽 화살표
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = buttons.Length - 1;
            UpdateCursorPosition();
        }
        // 2. 아래쪽 화살표
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            currentIndex++;
            if (currentIndex >= buttons.Length) currentIndex = 0;
            UpdateCursorPosition();
        }
        // 3. 엔터키 (메인 엔터와 숫자패드 엔터 모두 감지)
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            ExecuteButton();
        }
    }

    void UpdateCursorPosition()
    {
        if (cursor != null && buttons.Length > 0 && buttons[currentIndex] != null)
        {
            // 버튼의 world position을 가져와 오프셋을 더합니다.
            cursor.position = buttons[currentIndex].transform.position + cursorOffset;
        }
    }

    void ExecuteButton()
    {
        // 현재 인덱스의 버튼 클릭 이벤트를 강제로 실행
        if (buttons[currentIndex] != null)
        {
            Debug.Log(buttons[currentIndex].name + " 버튼 실행!");
            buttons[currentIndex].onClick.Invoke();
        }
    }

    // 버튼 연결용 함수들 (기존과 동일)
    public void PlayGame() { SceneManager.LoadScene("GameScene_1"); }
    public void QuitGame() { Application.Quit(); }
    public void OpenCredit() { /* Credit 패널 활성화 */ }
}