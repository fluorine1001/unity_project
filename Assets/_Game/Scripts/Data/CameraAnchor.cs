using UnityEngine;

public class CameraAnchor : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("이 앵커가 활성화될 스테이지 번호입니다.")]
    public int stageIndex;

    [Tooltip("카메라 줌 배율입니다. 1.0 = 기본, >1.0 = 확대(Zoom In), <1.0 = 축소(Zoom Out)")]
    public float zoomScale = 1.0f;

    [Header("Debug")]
    [Tooltip("에디터에서 앵커 위치를 쉽게 확인하기 위한 기즈모 색상")]
    public Color gizmoColor = Color.cyan;

    // 에디터에서 위치를 눈으로 확인하기 위한 시각화 코드
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(16f / zoomScale, 9f / zoomScale, 1f)); // 16:9 비율 가이드
    }
}