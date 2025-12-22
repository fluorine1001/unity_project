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
    public Sprite offSprite; 
    public Sprite onSprite;  

    public bool IsActive { get; private set; } = false;
    public int StageID { get; private set; } = -1;

    private SpriteRenderer spriteRenderer;
    private bool wasHitThisFrame = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();

        var generator = FindObjectOfType<GeneratorManager>();
        if (generator != null)
        {
            StageID = generator.GetStageIndexFromWorldPos(transform.position);
        }

        if (StageManager.Instance != null && StageID != -1)
        {
            StageManager.Instance.RegisterPuzzleBlock(StageID, this);
        }
    }

    private void LateUpdate()
    {
        // 이번 프레임에 레이저를 맞았는지 확인하여 상태 갱신
        bool newState = wasHitThisFrame;

        if (IsActive != newState)
        {
            IsActive = newState;
            UpdateSprite();

            if (StageManager.Instance != null)
                StageManager.Instance.CheckDoorState(StageID);
        }

        // 다음 프레임 체크를 위해 초기화
        wasHitThisFrame = false;
    }

    // ✅ 인터페이스 구현 (수정됨)
    public LaserAction OnLaserHit(Vector2 inDir, out List<Vector2> outDirs)
    {
        wasHitThisFrame = true; // 레이저 맞음 표시
        
        // 반사각 없음 (흡수)
        outDirs = null; 

        // Reflect를 리턴하면 LaserEmitter는 충돌로 인식하고 Loop를 멈춤(break).
        // 하지만 outDirs가 없으므로 새로운 레이저가 나가지 않음 -> 결과적으로 Stop.
        return LaserAction.Reflect; 
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        if (IsActive && onSprite != null){
            spriteRenderer.sprite = onSprite;
            // 오디오 재생
            if(isTarget) AudioManager.instance.PlayOneShot(FMODEvents.instance.TargetActivated, transform.position);
            else AudioManager.instance.PlayOneShot(FMODEvents.instance.NonTargetActivated, transform.position);
        }
        else if (!IsActive && offSprite != null) spriteRenderer.sprite = offSprite;
    }
}