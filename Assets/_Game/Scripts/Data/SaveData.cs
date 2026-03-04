using System;

[Serializable]
public class SaveData
{
    public int sceneIndex;
    public int currentStage;
    public int highestReachedStage;
    public string saveTime; // 사용자에게 언제 저장했는지 보여주기 위함

    // [추가] 플레이 타임 (초 단위)
    public float playTime; 

    // 생성자 수정
    public SaveData(int sceneIdx, int curStage, int highStage, float time)
    {
        sceneIndex = sceneIdx;
        currentStage = curStage;
        highestReachedStage = highStage;
        saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        playTime = time; // 시간 저장
    }
}