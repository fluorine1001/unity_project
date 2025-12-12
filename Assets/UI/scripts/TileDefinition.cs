using UnityEngine;

[CreateAssetMenu(menuName = "Game/Tile Definition")]
public class TileDefinition : ScriptableObject
{
    public string id;
    public Sprite icon;
    public GameObject prefab; // 나중에 드래그 설치할 때 사용
}
