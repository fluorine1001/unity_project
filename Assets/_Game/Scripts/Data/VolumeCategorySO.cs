using UnityEngine;

[CreateAssetMenu(fileName = "VolumeCategory", menuName = "Audio/Volume Category")]
public class VolumeCategorySO : ScriptableObject
{
    [Header("Category Info")]
    public string categoryName; // 에디터 구별용 (또는 기본값)
    
    // ✅ [추가] 번역 시스템에서 찾을 Key 값 (예: "Label_BGM")
    public string localizationKey; 

    [Header("FMOD Settings")]
    public string vcaPath;
    public string saveKey;
}