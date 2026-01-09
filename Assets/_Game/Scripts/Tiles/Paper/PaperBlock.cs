using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class PaperBlock : MonoBehaviour, ILaserInteractable
{
    // 레이저가 맞았을 때 호출
    public LaserAction OnLaserHit(Vector2 inDir, out List<Vector2> outDirs)
    {
        outDirs = null;

        // 즉시 파괴 (종이가 타버림)
        Destroy(gameObject);

        AudioManager.instance.PlayOneShot(FMODEvents.instance.PaperBurnt, transform.position);

        // 레이저 입장에서는 막힌 것으로 처리 (이번 프레임)
        // LaserDefinitions.cs에 정의된 'Block' 사용
        return LaserAction.Block; 
    }
}