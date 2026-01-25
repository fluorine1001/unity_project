using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VolumePageUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainPagePanel;
    [SerializeField] private GameObject volumePagePanel;

    [Header("Master Volume")]
    [SerializeField] private VolumeSliderUI masterSliderUI; // 리스트 밖 별도 슬라이더

    [Header("Category List")]
    [SerializeField] private Transform listContainer;       // ScrollView의 Content
    [SerializeField] private VolumeSliderUI sliderPrefab;   // 프리팹
    [SerializeField] private List<VolumeCategorySO> categories; // 인스펙터에서 할당

    // 버튼 이벤트 연결용 함수
    public void OpenVolumePage()
    {
        mainPagePanel.SetActive(false);
        volumePagePanel.SetActive(true);

        // 페이지 열 때 UI 갱신
        InitializeUI();
    }

    public void CloseVolumePage()
    {
        volumePagePanel.SetActive(false);
        mainPagePanel.SetActive(true);
    }

    public void InitializeUI()
    {
        // 1. 마스터 슬라이더 설정
        if (masterSliderUI != null)
        {
            masterSliderUI.SetupMaster();
        }

        // 2. 리스트 초기화 (중복 생성 방지)
        // 기존 아이템 삭제
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        // 3. 리스트 생성
        foreach (var category in categories)
        {
            VolumeSliderUI newSlider = Instantiate(sliderPrefab, listContainer);
            newSlider.SetupCategory(category);
        }
    }
}   