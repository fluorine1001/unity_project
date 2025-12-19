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

    [Header("Stage UI")]
    public TilePaletteUI paletteUI;           // LeftBar에 붙은 TilePaletteUI 연결
    public List<StageLoadout> stageLoadouts;  // 스테이지별 프리셋 데이터

    private List<Vector3> clearTilePositions = new List<Vector3>();
    private List<Vector3> spawnTilePositions = new List<Vector3>();
    private bool pendingPaletteRefresh = false;


    private bool cameraMoving = false;
    private bool stageCleared = false;
    private bool stageSpawned = false;

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
    }

    public void RegisterClearTile(Vector3 pos) => clearTilePositions.Add(pos);
    public void RegisterSpawnTile(Vector3 pos) => spawnTilePositions.Add(pos);

    public void OnPlayerStepOnClearTile()
    {
        if (stageCleared) return;
        stageCleared = true;
        stageSpawned = false;
        currentStage++;
        
        //RefreshStagePalette();

        if (currentStage == 9)
        {
            ResetStageSystem(); // ✅ 전체 초기화 함수 호출
        }
        else
        {
            pendingPaletteRefresh = true;
            MoveCameraToNextStage();
        }

        print(currentStage);
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
        if (pendingPaletteRefresh)
        {
            pendingPaletteRefresh = false;
            RefreshStagePalette();
        }
    }

    public bool IsClearTile(Vector3 playerPos)
    {
        if (stageCleared) return false;
        foreach (var pos in clearTilePositions)
            if (Vector3.Distance(playerPos, pos) < 0.5f)
                return true;
        return false;
    }

    public bool IsSpawnTile(Vector3 playerPos)
    {
        if (stageSpawned) return false;
        foreach (var pos in spawnTilePositions)
            if (Vector3.Distance(playerPos, pos) < 0.5f)
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
    private void RefreshStagePalette()
    {
        Debug.Log($"[Palette] Refresh stage={currentStage} paletteUI={(paletteUI ? "OK" : "NULL")} loadouts={(stageLoadouts==null ? "NULL" : stageLoadouts.Count.ToString())}");

        if (paletteUI == null) return;

        StageLoadout loadout = null;
        if (stageLoadouts != null && currentStage >= 0 && currentStage < stageLoadouts.Count)
            loadout = stageLoadouts[currentStage];

        Debug.Log($"[Palette] Loadout={(loadout ? loadout.name : "NULL")}");
        paletteUI.Build(loadout);
    }

private void Start()
{
    RefreshStagePalette();
}

}
