using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Control")]
    public int currentStage = 0;
    public List<Vector3> cameraPositions;  // 각 스테이지의 카메라 위치
    public float cameraMoveSpeed = 2f;

    private Vector3 clearTilePos;
    private bool cameraMoving = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterClearTile(Vector3 pos)
    {
        clearTilePos = pos;
    }

    private void Update()
    {
        if (cameraMoving)
        {
            MoveCameraToTarget();
        }
    }

    public void OnPlayerStepOnClearTile()
    {
        currentStage++;
        MoveCameraToNextStage();
    }

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

    // 나중에 다른 시스템에서 불러서 쓸 수 있도록
    public bool IsClearTile(Vector3 playerPos)
    {
        return Vector3.Distance(playerPos, clearTilePos) < 0.1f;
    }
}
