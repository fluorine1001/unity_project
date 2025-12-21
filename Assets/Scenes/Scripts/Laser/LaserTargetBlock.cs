using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class LaserTargetBlock : MonoBehaviour, ILaserInteractable
{
    [Header("Settings")]
    [Tooltip("체크 시: 타겟 블록(켜져야 문 열림) / 해제 시: 논타겟 블록(꺼져야 문 열림)")]
    public bool isTarget = true;

    [Header("Visuals")]
    public Sprite offSprite; // 평소 이미지
    public Sprite onSprite;  // 레이저 닿았을 때 이미지

    // 현재 활성화 여부
    public bool IsActive { get; private set; } = false;
    public int StageID { get; private set; } = -1;

    private SpriteRenderer spriteRenderer;
    private bool wasHitThisFrame = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();

        // 1. 스테이지 번호 찾기
        var generator = FindObjectOfType<GeneratorManager>();
        if (generator != null)
        {
            StageID = generator.GetStageIndexFromWorldPos(transform.position);
        }

        // 2. StageManager에 등록
        if (StageManager.Instance != null && StageID != -1)
        {
            StageManager.Instance.RegisterPuzzleBlock(StageID, this);
        }
    }

    private void LateUpdate()
    {
        // 레이저가 이번 프레임에 닿았는지 여부로 상태 결정
        bool newState = wasHitThisFrame;

        if (IsActive != newState)
        {
            IsActive = newState;
            UpdateSprite();

            // 상태 변화가 생기면 StageManager에게 문 검사 요청
            if (StageManager.Instance != null)
                StageManager.Instance.CheckDoorState(StageID);
        }

        // 다음 프레임 체크를 위해 초기화
        wasHitThisFrame = false;
    }

    // 인터페이스 구현
    public LaserAction OnLaserHit(Vector2 inDir, out List<Vector2> outDirs)
    {
        wasHitThisFrame = true; // 레이저 맞음 표시
        outDirs = null;
        return LaserAction.Pass; // 레이저는 통과함 (Pass는 정의되어 있으므로 그대로 사용)
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        if (IsActive && onSprite != null) spriteRenderer.sprite = onSprite;
        else if (!IsActive && offSprite != null) spriteRenderer.sprite = offSprite;
    }
}