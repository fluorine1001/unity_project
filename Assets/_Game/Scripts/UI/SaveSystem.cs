using UnityEngine;
using System.IO;

public class SaveSystem : MonoBehaviour
{

    // ✅ [추가] 슬롯 개수 상수 정의 (여기서만 바꾸면 게임 전체 적용)
    public const int SlotCount = 3;
    public static SaveSystem Instance { get; private set; }

    private void Awake()
    {
        // 씬이 바뀌어도 파괴되지 않도록 설정 (추후 로드 구현 대비)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 파일 경로 가져오기 (예: .../savefile_0.json)
    private string GetPath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"savefile_{slotIndex}.json");
    }

    // 저장 기능
    // SaveSystem.cs

    public void Save(int slotIndex)
    {
        if (StageManager.Instance == null) return;

        int sIdx = StageManager.Instance.sceneIndex;
        int cStage = StageManager.Instance.currentStage;
        int hStage = StageManager.Instance.highestReachedStage;

        // 기존 데이터 로드하여 진행도 보정 (기존 로직 유지)
        SaveData existingData = Load(slotIndex);
        int realHighest = hStage;
        if (existingData != null && existingData.highestReachedStage > hStage) 
            realHighest = existingData.highestReachedStage;

        int cStageToSave = (cStage == -1) ? realHighest : cStage;
        int hStageToSave = (cStage == -1) ? realHighest : (realHighest > cStage ? realHighest : cStage);

        // ✅ [수정] 현재 플레이 타임 저장
        float pTime = StageManager.Instance.currentPlayTime;

        SaveData data = new SaveData(sIdx, cStageToSave, hStageToSave, pTime);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slotIndex), json);
        
        // 세션 등록
        StageManager.Instance.RegisterSaveToSession(slotIndex);
    }

    public void DeleteSave(int slotIndex)
    {
        string path = GetPath(slotIndex);
        
        if (File.Exists(path))
        {
            File.Delete(path); // 파일 삭제
            Debug.Log($"[SaveSystem] Slot {slotIndex} 데이터 삭제됨");

            // ✅ [추가] StageManager가 존재한다면 관리 목록에서 이 슬롯 제거
            // (메인 메뉴에서도 삭제는 가능하므로 null 체크 필요)
            if (StageManager.Instance != null)
            {
                StageManager.Instance.UnregisterSaveFromSession(slotIndex);
            }
        }
    }

    // 로드 기능 (데이터 반환만 담당)
    public SaveData Load(int slotIndex)
    {
        string path = GetPath(slotIndex);
        if (!File.Exists(path)) return null; // 파일이 없으면 null 반환

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            Debug.LogError($"[SaveSystem] 세이브 파일 손상됨: Slot {slotIndex}");
            return null;
        }
    }
}