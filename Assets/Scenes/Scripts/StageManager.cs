using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Control")]
    public int currentStage = 0;
    public List<Vector3> cameraPositions;
    public float cameraMoveSpeed = 2f;
    public Transform player;
    public Vector3 startPosition = new Vector3(-17.92f, 2.56f, 0f);

    private List<Vector3> clearTilePositions = new List<Vector3>();
    private List<Vector3> spawnTilePositions = new List<Vector3>();

    private bool cameraMoving = false;
    private bool stageCleared = false;
    private bool stageSpawned = false;

    private float leftBoundaryX = float.NegativeInfinity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cameraPositions == null || cameraPositions.Count == 0)
        {
            cameraPositions = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(104.96f, 0f, 0f),
                new Vector3(212.48f, 0f, 0f),
                new Vector3(319.99f, 0f, 0f),
                new Vector3(427.52f, 0f, 0f),
                new Vector3(535.04f, 0f, 0f),
                new Vector3(642.56f, 0f, 0f),
                new Vector3(750.08f, 0f, 0f),
                new Vector3(857.60f, 5.12f, 0f),
                new Vector3(0f, 0f, 0f)
            };
        }
    }

    private void Update()
    {
        if (cameraMoving)
            MoveCameraToTarget();
        ClampPlayerToLeftBoundary();
    }

    private void ClampPlayerToLeftBoundary()
    {
        if (player == null) return;
        if (float.IsNegativeInfinity(leftBoundaryX)) return; // 아직 경계가 설정 안 됐으면 패스

        Vector3 pos = player.position;
        if (pos.x < leftBoundaryX)
        {
            pos.x = leftBoundaryX;
            player.position = pos;
        }
    }

    public void RegisterClearTile(Vector3 pos) => clearTilePositions.Add(pos);
    public void RegisterSpawnTile(Vector3 pos) => spawnTilePositions.Add(pos);

    public void OnPlayerStepOnClearTile()
    {
        if (stageCleared) return;
        stageCleared = true;
        stageSpawned = false;
        currentStage++;

        int previousStage = currentStage; // ⬅ 지금 스테이지 기억
        currentStage++;

        if (currentStage == 9)
        {
            ResetStageSystem(); // ✅ 전체 초기화 함수 호출
        }
        else
        {
            UpdateLeftBoundary(previousStage, currentStage);
            MoveCameraToNextStage();
        }

        print(currentStage);
    }

    private void UpdateLeftBoundary(int prevStage, int newStage)
    {
        if (cameraPositions == null) return;
        if (prevStage < 0 || newStage >= cameraPositions.Count) return;

        float prevX = cameraPositions[prevStage].x;
        float newX = cameraPositions[newStage].x;

        // 두 카메라 중앙 사이의 중간 지점을 경계선으로 사용
        leftBoundaryX = 0.5f * (prevX + newX);
        // Debug.Log($"왼쪽 경계 업데이트: {leftBoundaryX}");
    }

    public void OnPlayerStepOnSpawnTile()
    {
        if (stageSpawned) return;
        stageSpawned = true;
        stageCleared = false;
        Debug.Log($"Stage {currentStage} 시작 위치 진입 완료");
    }

    private void MoveCameraToNextStage()
    {
        if (currentStage < cameraPositions.Count)
            cameraMoving = true;
        else
            Debug.Log("No more stages!");
    }

    private void MoveCameraToTarget()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || currentStage >= cameraPositions.Count) return;

        Vector3 targetPos = cameraPositions[currentStage];
        targetPos.z = mainCam.transform.position.z;

        mainCam.transform.position = Vector3.Lerp(
            mainCam.transform.position,
            targetPos,
            Time.deltaTime * cameraMoveSpeed
        );

        if (Vector3.Distance(mainCam.transform.position, targetPos) < 0.05f)
        {
            mainCam.transform.position = targetPos;
            cameraMoving = false;
        }
    }

    public bool IsClearTile(Vector3 playerPos)
    {
        if (stageCleared) return false;
        foreach (var pos in clearTilePositions)
            if (Vector3.Distance(playerPos, pos) < 0.1f)
                return true;
        return false;
    }

    public bool IsSpawnTile(Vector3 playerPos)
    {
        if (stageSpawned) return false;
        foreach (var pos in spawnTilePositions)
            if (Vector3.Distance(playerPos, pos) < 0.1f)
                return true;
        return false;
    }

    // === ✅ 전체 시스템 초기화 함수 ===
    private void ResetStageSystem()
    {
        currentStage = 0;
        stageCleared = false;
        stageSpawned = false;
        cameraMoving = false;

        leftBoundaryX = float.NegativeInfinity;
        
        clearTilePositions.Clear();
        spawnTilePositions.Clear();

        if (player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
                rb.position = startPosition;
            else
                player.position = startPosition;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 resetPos = cameraPositions[0];
            resetPos.z = mainCam.transform.position.z;
            mainCam.transform.position = resetPos;
        }

        Debug.Log("=== 전체 스테이지 및 상태 초기화 완료 ===");

        // ✅ 초기 스테이지 강제 재시작
        OnPlayerStepOnSpawnTile();
    }
}
