using UnityEngine;

public class HoleTile : MonoBehaviour
{
    public enum HoleState { Empty, Filled }
    public HoleState state = HoleState.Empty;

    [Header("Hole Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite filledSprite;
    [SerializeField] private string emptyLayer = "PlayerBlocker";
    [SerializeField] private string filledLayer = "PlayerPass";

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
            _col.isTrigger = (state == HoleState.Filled);
        }

        gameObject.layer = LayerMask.NameToLayer(
            state == HoleState.Empty ? emptyLayer : filledLayer
        );
    }

    public bool IsEmpty() => state == HoleState.Empty;
}
