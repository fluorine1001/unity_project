using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Control")]
    public int currentStage = 0;
    public List<Vector3> cameraPositions;     // 스테이지별 카메라 위치
    public float cameraMoveSpeed = 2f;        // 카메라 이동 속도

    private List<Vector3> clearTilePositions = new List<Vector3>(); // 클리어 타일 좌표들
    private List<Vector3> spawnTilePositions = new List<Vector3>(); // 스폰 타일 좌표들

    private bool cameraMoving = false;
    private bool stageCleared = false;
    private bool stageSpawned = false; // 새 스테이지 진입 완료 여부

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // === GeneratorManager에서 등록 ===
    public void RegisterClearTile(Vector3 pos)
    {
        clearTilePositions.Add(pos);
    }

    public void RegisterSpawnTile(Vector3 pos)
    {
        spawnTilePositions.Add(pos);
    }

    private void Update()
    {
        if (cameraMoving)
            MoveCameraToTarget();
    }

    // === 플레이어가 클리어 타일 밟았을 때 ===
    public void OnPlayerStepOnClearTile()
    {
        if (stageCleared) return; // 중복 방지
        stageCleared = true;
        stageSpawned = false;

        currentStage++;
        MoveCameraToNextStage();
        print(currentStage);
    }

    // === 플레이어가 스폰 타일 밟았을 때 ===
    public void OnPlayerStepOnSpawnTile()
    {
        if (stageSpawned) return; // 중복 방지
        stageSpawned = true;

        // ✅ 스테이지 클리어 상태 해제 → 다시 클리어 가능
        stageCleared = false;
        Debug.Log($"Stage {currentStage} 시작 위치 진입 완료");
    }

    // === 카메라 이동 ===
    private void MoveCameraToNextStage()
    {
        if (currentStage < cameraPositions.Count)
        {
            cameraMoving = true;
        }
        else
        {
            Debug.Log("No more stages!");
        }
    }

    private void MoveCameraToTarget()
    {
        Camera mainCam = Camera.main;
        Vector3 targetPos = cameraPositions[currentStage];
        targetPos.z = mainCam.transform.position.z;

        mainCam.transform.position = Vector3.Lerp(
            mainCam.transform.position,
            targetPos,
            Time.deltaTime * cameraMoveSpeed
        );

        // 도착 판정
        if (Vector3.Distance(mainCam.transform.position, targetPos) < 0.05f)
        {
            mainCam.transform.position = targetPos;
            cameraMoving = false;
        }
    }

    // === 타일 판정 ===
    public bool IsClearTile(Vector3 playerPos)
    {
        if (stageCleared) return false; // 클리어 중에는 무시
        foreach (var pos in clearTilePositions)
        {
            if (Vector3.Distance(playerPos, pos) < 0.1f)
                return true;
        }
        return false;
    }

    public bool IsSpawnTile(Vector3 playerPos)
    {
        if (stageSpawned) return false; // 이미 스폰 완료한 스테이지면 무시
        foreach (var pos in spawnTilePositions)
        {
            if (Vector3.Distance(playerPos, pos) < 0.1f)
                return true;
        }
        return false;
    }
}