// SpeedCodexUI.cs
using UnityEngine;
using UnityEngine.UI;

public class SpeedCodexUI : MonoBehaviour
{
    [Header("패턴을 그릴 컨테이너")]
    public RectTransform patternContainer;

    [Header("셀 프리팹")]
    public GameObject speedUpCellPrefab;
    public GameObject speedDownCellPrefab;

    [Header("셀 크기(px)")]
    public float cellSize = 32f;

    [Header("현재 보여줄 패턴")]
    public SpeedCodexEntry currentEntry;

    private void Start()
    {
        RenderCurrentEntry();
    }

    public void SetEntry(SpeedCodexEntry entry)
    {
        currentEntry = entry;
        RenderCurrentEntry();
    }

    public void RenderCurrentEntry()
    {
        // 1) 기존 자식 제거
        for (int i = patternContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(patternContainer.GetChild(i).gameObject);
        }

        if (currentEntry == null || currentEntry.cells == null)
            return;

        // 2) 가운데 정렬을 위해, 기준 offset 계산
        Vector2Int size = currentEntry.size;
        Vector2 centerOffset = new Vector2(
            -(size.x - 1) * 0.5f * cellSize,
            -(size.y - 1) * 0.5f * cellSize
        );

        // 3) 각 셀 Instantiate
        foreach (var cell in currentEntry.cells)
        {
            if (cell.kind == SpeedTileKind.None)
                continue;

            GameObject prefab = null;
            switch (cell.kind)
            {
                case SpeedTileKind.SpeedUp:
                    prefab = speedUpCellPrefab;
                    break;
                case SpeedTileKind.SpeedDown:
                    prefab = speedDownCellPrefab;
                    break;
            }

            if (prefab == null) continue;

            GameObject go = Instantiate(prefab, patternContainer);
            RectTransform rt = go.GetComponent<RectTransform>();

            // 좌표 → anchoredPosition
            Vector2 pos = new Vector2(
                cell.offset.x * cellSize,
                cell.offset.y * cellSize
            );
            rt.anchoredPosition = centerOffset + pos;
        }
    }
}
