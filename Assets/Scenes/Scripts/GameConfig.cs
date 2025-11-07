using UnityEngine;

[DefaultExecutionOrder(-100)] // 가장 먼저 초기화되도록
public class GameConfig : MonoBehaviour
{
    private static GameConfig _instance;
    public static GameConfig Instance => _instance;

    [Header("=== Global Game Constants ===")]
    [Tooltip("모든 타일/총알에서 공통으로 사용하는 속도 스케일 단위 (예: 30)")]
    public float speedScale = 30f;

    private void Awake()
    {
        // 싱글톤 보장
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        DontDestroyOnLoad(gameObject); // 씬 전환해도 유지
    }

    /// <summary>
    /// 전역 스케일 반환 (정적 접근)
    /// </summary>
    public static float SpeedScale => Instance != null ? Instance.speedScale : 30f;
}
