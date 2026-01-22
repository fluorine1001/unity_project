using UnityEngine;
using System.IO;

public class SaveSystem : MonoBehaviour
{
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
    public void Save(int slotIndex)
    {
        // StageManager가 없으면 저장 불가
        if (StageManager.Instance == null)
        {
            Debug.LogError("StageManager가 없습니다!");
            return;
        }

        // 1. StageManager에서 데이터 추출
        int sIdx = StageManager.Instance.sceneIndex;
        int cStage = StageManager.Instance.currentStage;
        int hStage = StageManager.Instance.highestReachedStage;

        // 2. 데이터 객체 생성
        SaveData data = new SaveData(sIdx, cStage, hStage);

        // 3. JSON 변환 및 파일 쓰기
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slotIndex), json);

        Debug.Log($"[SaveSystem] Slot {slotIndex} 저장 완료: {json}");
    }

    public void DeleteSave(int slotIndex)
    {
        string path = GetPath(slotIndex);
        
        if (File.Exists(path))
        {
            File.Delete(path); // 파일 삭제
            Debug.Log($"[SaveSystem] Slot {slotIndex} 데이터 삭제됨");
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