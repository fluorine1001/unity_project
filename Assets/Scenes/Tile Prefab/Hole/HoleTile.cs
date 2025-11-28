using UnityEngine;

public class HoleTile : MonoBehaviour
{
    public enum HoleState { Empty, Filled }
    public HoleState state = HoleState.Empty;

    [Header("Hole Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite filledSprite;

    private SpriteRenderer _sr;
    private Collider2D _col;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();

        ApplyState();
    }

    // 외부에서 호출할 함수: 상자가 빠지고 채운 상태로 변경
    public void FillHole()
    {
        state = HoleState.Filled;
        ApplyState();
    }

    private void ApplyState()
    {
        if (_sr != null)
        {
            _sr.sprite = (state == HoleState.Empty) ? emptySprite : filledSprite;
        }

        if (_col != null)
        {
            // Empty: 플레이어/상자 충돌해야 함 (벽)
            // Filled: 모두 통과 가능
            _col.isTrigger = (state == HoleState.Filled);
        }
    }

    public bool IsEmpty() => state == HoleState.Empty;
}
