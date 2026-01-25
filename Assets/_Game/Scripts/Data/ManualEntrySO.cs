using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

// 블록 종류
public enum ManualBlockType 
{ 
    Heading1, 
    Heading2, 
    BodyText, 
    Image, 
    Video, 
    Spacer 
}

// 정렬 옵션
public enum BlockAlignment { Left, Center, Right }

[System.Serializable]
public class ManualBlock
{
    [Header("Block Type")]
    public ManualBlockType type = ManualBlockType.BodyText;
    
    [Header("Content")]
    [TextArea(3, 10)] 
    [Tooltip("텍스트 블록의 경우, 여기에 Localization Key를 입력하세요. (예: 'MANUAL_BODY_01')")]
    // 🔥 [수정] textContent -> textKey (키값임을 명시)
    public string textKey;      
    
    public Sprite imageContent;    
    public VideoClip videoContent;  

    [Header("Style Options")]
    public BlockAlignment alignment = BlockAlignment.Left; 
    
    [Tooltip("이미지/비디오 높이 또는 Spacer 높이")]
    public float sizeValue = 200f;  
}

[CreateAssetMenu(fileName = "Manual_Entry", menuName = "Manual/Entry")]
public class ManualEntrySO : ScriptableObject
{
    [Tooltip("목록에 표시될 제목의 Localization Key (예: 'MANUAL_TITLE_MOVE')")]
    // 🔥 [수정] entryTitle -> titleKey (키값임을 명시)
    public string titleKey;       
    
    [Tooltip("태그의 Localization Key 리스트 (예: 'TAG_BASIC')")]
    // 🔥 [수정] tags -> tagKeys (키값임을 명시)
    public List<string> tagKeys;       
    
    [Header("Notion-style Blocks")]
    public List<ManualBlock> blocks; 
}