using UnityEngine;
using System.Collections.Generic;
using System;

public class LaserEmitter : MonoBehaviour
{
    [Header("Laser Settings")]
    public LaserDirection startDirection; // 발사 방향
    public float maxDistance = 50f;       // 최대 거리
    public int maxBounces = 20;           // 최대 반사 횟수
    public LayerMask hitMask = ~0;        // 충돌 레이어 (Box, Mirror, Wall 포함 필수)
    public GameObject lineRendererPrefab; // 레이저 프리팹

    [Header("Visual Settings")]
    [Range(0.01f, 1f)]
    public float laserWidth = 0.3f;       // ✅ 레이저 굵기 조절 슬라이더

    [Header("Sorting Settings")]
    private const float Y_AXIS_MULTIPLIER = 50f;
    private const int BASE_SORTING_ORDER = 29999;
    
    // 레이저는 '중간' 계층입니다.
    // 박스(Offset +50) > 레이저(Offset 0) > 거울(Offset -100)
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
    private void CastLaser(Vector2 startPos, Vector2 direction, int depth, GameObject ignoreObj)
    {
        if (depth > maxBounces) return;

        // 1. 물리적 충돌 감지 (Raycast)
        // 시작점: 내 타일 안쪽(0.1f)에서 시작 -> 바로 앞(0.5f 거리)의 박스를 정확히 감지
        Vector2 rayOrigin = startPos + (direction * 0.1f);
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, maxDistance, hitMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector2 endPos = startPos + (direction * maxDistance); // 허공일 때 기본 끝점
        
        bool hitInteractable = false;
        List<Vector2> nextDirs = null;
        GameObject hitObject = null;

        foreach (var hit in hits)
        {
            // 방금 튕겨 나온 물체나 자기 자신 무시
            if (hit.collider.gameObject == ignoreObj) continue;
            if (hit.collider.gameObject == gameObject) continue;

            // (A) 상호작용 물체 (거울 등)
            if (hit.collider.TryGetComponent(out ILaserInteractable interactable))
            {
                LaserAction action = interactable.OnLaserHit(direction, out nextDirs);
                if (action == LaserAction.Pass) continue; // 투과

                // 거울은 타일 정중앙까지 그림
                endPos = hit.collider.transform.position; 
                hitObject = hit.collider.gameObject;
                
                if (action == LaserAction.Reflect) hitInteractable = true;
                break;
            }
            // (B) 클리어 타일 (필요 시)
            else if (StageManager.Instance != null && StageManager.Instance.IsClearTile(hit.point))
            {
                endPos = hit.collider.transform.position;
                break;
            }
            // (C) 일반 장애물 (박스, 벽) - isTrigger가 꺼진 콜라이더
            else if (!hit.collider.isTrigger) 
            {
                // 물리적으로 표면에 닿았지만, 시각적으로 겹치지 않게 아주 살짝(0.05f) 뒤로 당김
                endPos = hit.point - (direction * 0.05f); 
                hitObject = hit.collider.gameObject;
                break;
            }
        }

        // 2. 그래픽 처리 (LineRenderer)
        LineRenderer lr = GetLineRenderer();
        lr.positionCount = 2;

        // 시각적 시작점 설정
        if (depth == 0)
            // 첫 발사는 발사기 입구(0.5f 앞)에서 시작하여 발사기를 가리지 않음
            lr.SetPosition(0, startPos + (direction * 0.5f));
        else
            // 반사된 레이저는 중심에서 시작하여 끊김 없이 연결
            lr.SetPosition(0, startPos);

        lr.SetPosition(1, endPos);

        // Sorting Order 계산
        // 선분의 가장 낮은 Y값(화면상 가장 앞쪽)을 기준으로 깊이 설정
        float lowestY = Mathf.Min(startPos.y, endPos.y);
        int sortOrder = (int)(-lowestY * Y_AXIS_MULTIPLIER) + BASE_SORTING_ORDER + LASER_DEPTH_OFFSET;
        lr.sortingOrder = sortOrder;

        // 3. 재귀 호출 (반사)
        if (hitInteractable && nextDirs != null)
        {
            foreach (var nextDir in nextDirs)
            {
                // 다음 레이저의 시작점은 '시각적 끝점(endPos)'이 아니라
                // '물리적 오브젝트의 중심(hitObject.transform.position)'이어야 정확함
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
            
            // 굵기 실시간 업데이트
            lr.startWidth = laserWidth;
            lr.endWidth = laserWidth;
            return lr;
        }

        if (lineRendererPrefab == null) return null;

        GameObject go = Instantiate(lineRendererPrefab, transform);
        go.name = "LaserLine_" + poolIndex;
        LineRenderer newLr = go.GetComponent<LineRenderer>();
        
        // 굵기 설정
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