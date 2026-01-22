using System;

[Serializable]
public class SaveData
{
    public int sceneIndex;
    public int currentStage;
    public int highestReachedStage;
    public string saveTime; // 사용자에게 언제 저장했는지 보여주기 위함

    // 생성자
    public SaveData(int _sceneIndex, int _currStage, int _highStage)
    {
        sceneIndex = _sceneIndex;
        currentStage = _currStage;
        highestReachedStage = _highStage;
        saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm"); // 예: 2024-01-22 14:30
    }
}