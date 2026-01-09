using UnityEngine;
using System.Collections.Generic;
using System;

public class LaserEmitter : MonoBehaviour
{
    [Header("Laser Settings")]
    public LaserDirection startDirection; 
    public float maxDistance = 500f;      
    public int maxBounces = 100;          
    public LayerMask hitMask = ~0;        // (참고용) 코드에서 ~0으로 강제할 예정
    public GameObject lineRendererPrefab; 

    [Header("Visual Settings")]
    [Range(0.01f, 1f)]
    public float laserWidth = 0.3f;       

    [Header("Sorting Settings")]
    private const float Y_AXIS_MULTIPLIER = 50f;
    private const int BASE_SORTING_ORDER = 29999;
    private const int LASER_DEPTH_OFFSET = 0;

    private List<LineRenderer> activeLasers = new List<LineRenderer>();
    private int poolIndex = 0;

    private void Start()
    {
        transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
    }

    private void Update()
    {
        poolIndex = 0;
        foreach (var lr in activeLasers) lr.gameObject.SetActive(false);
        CastLaser(transform.position, DirToVec(startDirection), 0, gameObject);
    }

    private void CastLaser(Vector2 startPos, Vector2 direction, int depth, GameObject ignoreObj)
    {
        if (depth > maxBounces) return;

        Vector2 rayOrigin = startPos + (direction * 0.1f);
        
        // 1. [핵심 변경] hitMask 대신 ~0 (Everything)을 사용하여 무조건 모든 충돌체를 다 가져옵니다.
        // 유니티 물리 설정에서 Queries Hit Triggers가 켜져 있어야 합니다.
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, maxDistance, ~0);
        
        // 거리순 정렬
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector2 endPos = startPos + (direction * maxDistance);
        
        bool hitInteractable = false;
        List<Vector2> nextDirs = new List<Vector2>();
        GameObject hitObject = null;

        foreach (var hit in hits)
        {
            GameObject hitGo = hit.collider.gameObject;

            // (1) 무시할 오브젝트들
            if (hitGo == ignoreObj) continue;
            if (depth == 0 && hitGo == gameObject) continue;

            // ------------------------------------------------------------------
            // ✅ [추가됨] 구멍(HoleTile)은 무조건 통과
            // ------------------------------------------------------------------
            HoleTile hole = hitGo.GetComponent<HoleTile>();
            if (hole == null) hole = hitGo.GetComponentInParent<HoleTile>();
            
            if (hole != null)
            {
                // 구멍 타일이 감지되면 레이저는 그냥 통과합니다. (다음 hit 검사로 넘어감)
                // Debug.Log("구멍 통과");
                continue; 
            }
            // ------------------------------------------------------------------

            // (2) GeneratorManager 블로커 확인
            if (GeneratorManager.Instance != null && GeneratorManager.Instance.IsBlockerTile(hit.collider.transform.position))
            {
                if (hit.collider.isTrigger) endPos = hit.collider.transform.position;
                else endPos = hit.point - (direction * 0.05f);

                hitObject = hitGo;
                break;
            }

            // ------------------------------------------------------------------
            // ✅ [기능 유지] 스피드 / 디스피드 타일 감지 시 멈춤
            // ------------------------------------------------------------------
            SpeedTile speedTile = hitGo.GetComponent<SpeedTile>();
            if (speedTile == null) speedTile = hitGo.GetComponentInParent<SpeedTile>();

            if (speedTile != null)
            {
                endPos = hit.collider.transform.position; // 타일 중앙에서 멈춤
                hitObject = hitGo;
                break; 
            }

            // (만약 DespeedTile이 있다면 동일하게 작성)
            // ------------------------------------------------------------------

            // (3) 상호작용 물체 (거울 등)
            ILaserInteractable interactable = hitGo.GetComponent<ILaserInteractable>();
            if (interactable == null) interactable = hitGo.GetComponentInParent<ILaserInteractable>();

            if (interactable != null)
            {
                LaserAction action = interactable.OnLaserHit(direction, out List<Vector2> outDirs);

                if (action == LaserAction.Pass) continue; 

                endPos = hit.collider.transform.position;
                hitObject = hitGo;
                
                if (action == LaserAction.Reflect)
                {
                    hitInteractable = true;
                    if (outDirs != null) nextDirs = outDirs;
                }
                break;
            }
            
            // (4) 클리어 타일
            else if (StageManager.Instance != null && StageManager.Instance.IsClearTile(hit.point))
            {
                endPos = hit.collider.transform.position;
                break;
            }
            
            // (5) 일반 물리 벽 (Trigger가 아닌 것)
            // 구멍이나 스피드타일은 위에서 처리했으므로, 여기까지 온 'Trigger가 아닌 녀석'은 진짜 벽입니다.
            else if (!hit.collider.isTrigger)
            {
                endPos = hit.point - (direction * 0.05f);
                hitObject = hitGo;
                break;
            }
        }

        // 2. 그래픽 처리
        LineRenderer lr = GetLineRenderer();
        lr.positionCount = 2;

        if (depth == 0) lr.SetPosition(0, startPos + (direction * 0.5f));
        else lr.SetPosition(0, startPos);

        lr.SetPosition(1, endPos);

        float lowestY = Mathf.Min(startPos.y, endPos.y);
        lr.sortingOrder = (int)(-lowestY * Y_AXIS_MULTIPLIER) + BASE_SORTING_ORDER;

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