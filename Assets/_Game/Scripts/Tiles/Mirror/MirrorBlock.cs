using UnityEngine;
using System.Collections.Generic;

public class MirrorBlock : MonoBehaviour, ILaserInteractable
{
    [Header("Mirror Settings")]
    public MirrorType mirrorType;
    public TriangleOrientation orientation;

    // 레이저 로직은 그대로 유지
    public LaserAction OnLaserHit(Vector2 inDir, out List<Vector2> outDirs)
    {
        outDirs = new List<Vector2>();

        if (Mathf.Abs(inDir.x) > Mathf.Abs(inDir.y))
            inDir = (inDir.x > 0) ? Vector2.right : Vector2.left;
        else
            inDir = (inDir.y > 0) ? Vector2.up : Vector2.down;

        switch (mirrorType)
        {
            case MirrorType.Square:
                outDirs.Add(-inDir);
                break;
            case MirrorType.Triangle:
                Vector2 reflectDir = GetReflectDir(inDir, false);
                if (reflectDir != Vector2.zero) outDirs.Add(reflectDir);
                break;
            case MirrorType.Half:
                outDirs.Add(inDir); 
                Vector2 halfReflect = GetReflectDir(inDir, true);
                if (halfReflect != Vector2.zero) outDirs.Add(halfReflect);
                break;
        }
        return outDirs.Count > 0 ? LaserAction.Reflect : LaserAction.Block;
    }

    private Vector2 GetReflectDir(Vector2 inDir, bool isHalf)
    {
        switch (orientation)
        {
            case TriangleOrientation.UpLeft:
                if (inDir == Vector2.right) return Vector2.up;
                if (inDir == Vector2.down) return Vector2.left;
                if (isHalf) { if (inDir == Vector2.left) return Vector2.down; if (inDir == Vector2.up) return Vector2.right; }
                break;
            case TriangleOrientation.RightDown:
                if (inDir == Vector2.left) return Vector2.down;
                if (inDir == Vector2.up) return Vector2.right;
                if (isHalf) { if (inDir == Vector2.right) return Vector2.up; if (inDir == Vector2.down) return Vector2.left; }
                break;
            case TriangleOrientation.UpRight:
                if (inDir == Vector2.left) return Vector2.up;
                if (inDir == Vector2.down) return Vector2.right;
                if (isHalf) { if (inDir == Vector2.right) return Vector2.down; if (inDir == Vector2.up) return Vector2.left; }
                break;
            case TriangleOrientation.DownLeft:
                if (inDir == Vector2.right) return Vector2.down;
                if (inDir == Vector2.up) return Vector2.left;
                if (isHalf) { if (inDir == Vector2.left) return Vector2.up; if (inDir == Vector2.down) return Vector2.right; }
                break;
        }
        return Vector2.zero;
    }
}