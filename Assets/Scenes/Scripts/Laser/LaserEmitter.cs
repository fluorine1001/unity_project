using UnityEngine;
using System.Collections.Generic;
using System;

public class LaserEmitter : MonoBehaviour
{
    [Header("Laser Settings")]
    public LaserDirection startDirection; // 발사 방향
    public float maxDistance = 500f;      // 최대 거리
    public int maxBounces = 100;          // 최대 반사 횟수
    public LayerMask hitMask = ~0;        // 충돌 레이어 (Box, Mirror, Wall 포함 필수)
    public GameObject lineRendererPrefab; // 레이저 프리팹

    [Header("Visual Settings")]
    [Range(0.01f, 1f)]
    public float laserWidth = 0.3f;       // ✅ 레이저 굵기 조절 슬라이더

    [Header("Sorting Settings")]
    private const float Y_AXIS_MULTIPLIER = 50f;
    private const int BASE_SORTING_ORDER = 29999;
    
    // 레이저는 '중간' 계층입니다.
    private const int LASER_DEPTH_OFFSET = 0; 

    // 오브젝트 풀링
    private List<LineRenderer> activeLasers = new List<LineRenderer>();
    private int poolIndex = 0;

    private void Start()
    {
        // 위치를 그리드 정중앙으로 보정
        transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
    }

    private void Update()
    {
        // 매 프레임 초기화
        poolIndex = 0;
        foreach (var lr in activeLasers) lr.gameObject.SetActive(false);

        // 첫 발사 시작
        CastLaser(transform.position, DirToVec(startDirection), 0, gameObject);
    }

    // 재귀적 레이저 발사 함수
    // 재귀적 레이저 발사 함수
    private void CastLaser(Vector2 startPos, Vector2 direction, int depth, GameObject ignoreObj)
    {
        if (depth > maxBounces) return;

        // 1. 물리적 충돌 감지 (Raycast)
        Vector2 rayOrigin = startPos + (direction * 0.1f);
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, maxDistance, hitMask);
        
        // 거리순 정렬
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector2 endPos = startPos + (direction * maxDistance);
        
        bool hitInteractable = false;
        List<Vector2> nextDirs = new List<Vector2>();
        GameObject hitObject = null;

        foreach (var hit in hits)
        {
            // (1) 방금 튕겨 나온 물체(ignoreObj) 무시 (즉시 재충돌 방지)
            if (hit.collider.gameObject == ignoreObj) continue;

            // ✅ (2) 발사기 자신(gameObject) 처리 [중요 수정]
            // 첫 발사(depth == 0)일 때는 발사기 내부에서 나가야 하므로 무시하지만,
            // 반사되어 돌아왔을 때(depth > 0)는 발사기도 '벽'처럼 막혀야 합니다.
            if (depth == 0 && hit.collider.gameObject == gameObject) continue;


            // (A) 상호작용 물체 (거울 등)
            if (hit.collider.TryGetComponent(out ILaserInteractable interactable))
            {
                LaserAction action = interactable.OnLaserHit(direction, out List<Vector2> outDirs);

                if (action == LaserAction.Pass) continue; 

                endPos = hit.collider.transform.position; 
                hitObject = hit.collider.gameObject;
                
                if (action == LaserAction.Reflect)
                {
                    hitInteractable = true;
                    if (outDirs != null) nextDirs = outDirs;
                }
                break;
            }
            // (B) 클리어 타일
            else if (StageManager.Instance != null && StageManager.Instance.IsClearTile(hit.point))
            {
                endPos = hit.collider.transform.position;
                break;
            }
            // (C) 일반 장애물 (박스, 벽, 그리고 **돌아온 발사기**)
            else if (!hit.collider.isTrigger) 
            {
                // 물리 표면에 닿음
                endPos = hit.point - (direction * 0.05f); 
                hitObject = hit.collider.gameObject;
                break;
            }
        }

        // ... (이하 그래픽 처리 및 재귀 호출 로직은 기존과 동일) ...
        
        // 2. 그래픽 처리 (LineRenderer)
        LineRenderer lr = GetLineRenderer();
        lr.positionCount = 2;

        if (depth == 0)
            lr.SetPosition(0, startPos + (direction * 0.5f));
        else
            lr.SetPosition(0, startPos);

        lr.SetPosition(1, endPos);

        // Sorting Order 설정
        float lowestY = Mathf.Min(startPos.y, endPos.y);
        int sortOrder = (int)(-lowestY * Y_AXIS_MULTIPLIER) + BASE_SORTING_ORDER + LASER_DEPTH_OFFSET;
        lr.sortingOrder = sortOrder;

        // 3. 재귀 호출
        if (hitInteractable && nextDirs != null && nextDirs.Count > 0)
        {
            foreach (var nextDir in nextDirs)
            {
                Vector2 logicalStart = (hitObject != null) ? (Vector2)hitObject.transform.position : endPos;
                CastLaser(logicalStart, nextDir, depth + 1, hitObject);
            }
        }
    }

    // LineRenderer 가져오기 (오브젝트 풀링)
    private LineRenderer GetLineRenderer()
    {
        if (poolIndex < activeLasers.Count)
        {
            var lr = activeLasers[poolIndex++];
            lr.gameObject.SetActive(true);
            lr.startWidth = laserWidth;
            lr.endWidth = laserWidth;
            return lr;
        }

        if (lineRendererPrefab == null) return null;

        GameObject go = Instantiate(lineRendererPrefab, transform);
        go.name = "LaserLine_" + poolIndex;
        LineRenderer newLr = go.GetComponent<LineRenderer>();
        
        newLr.startWidth = laserWidth;
        newLr.endWidth = laserWidth;
        
        activeLasers.Add(newLr);
        poolIndex++;
        return newLr;
    }

    private Vector2 DirToVec(LaserDirection d)
    {
        switch (d) {
            case LaserDirection.Up: return Vector2.up;
            case LaserDirection.Down: return Vector2.down;
            case LaserDirection.Left: return Vector2.left;
            case LaserDirection.Right: return Vector2.right;
            default: return Vector2.up;
        }
    }
}