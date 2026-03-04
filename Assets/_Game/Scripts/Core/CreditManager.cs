using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class CreditManager : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("여기에 개발자/도움준 분들의 닉네임을 입력하세요.")]
    public List<string> nameList; // 인스펙터에서 이름 추가

    [Header("UI References")]
    public Transform contentContainer; // Scroll View의 Content 오브젝트
    public GameObject nameTextPrefab;  // 생성할 닉네임 텍스트 프리팹
    public string mainMenuSceneName = "MainMenu"; // 돌아갈 메인 메뉴 씬 이름

    void Start()
    {
        GenerateCreditList();
    }

    private void GenerateCreditList()
    {
        // 기존에 혹시 테스트용으로 넣어둔 자식들이 있다면 삭제
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 리스트에 있는 이름들을 순서대로 생성
        foreach (string nickname in nameList)
        {
            GameObject go = Instantiate(nameTextPrefab, contentContainer);
            
            // TextMeshPro 컴포넌트 찾아서 텍스트 설정
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = nickname;
            }
        }
    }

    // 뒤로가기 버튼에 연결할 함수
    public void OnBackButtonClicked()
    {
        // 메인 메뉴 씬으로 이동
        // (주의: Build Settings에 MainMenu 씬이 등록되어 있어야 함)
        SceneManager.LoadScene(mainMenuSceneName);
    }
}