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
    
    // 🔥 [추가] 텍스트 간격 설정
    [Header("Text Spacing Settings")]
    [Tooltip("줄바꿈 시 줄 사이의 간격 (기본값: 0, 값이 클수록 넓어짐)")]
    public float lineSpacing = 0f; 

    [Tooltip("문단(엔터) 사이의 간격 (기본값: 0)")]
    public float paragraphSpacing = 30f;

    [Header("Video/Image Settings")]
    [Tooltip("이미지/비디오 높이 또는 Spacer 높이")]
    public float sizeValue = 200f;  

    // ✅ [추가] 비디오 전용 설정
    [Header("Video Settings")]
    [Tooltip("체크하면 자동 재생, 끄면 플레이 버튼을 눌러야 재생됩니다.")]
    public bool autoPlay = true;

    [Tooltip("체크하면 소리가 납니다. 끄면 음소거(Mute) 됩니다.")]
    public bool enableAudio = false;

    // ✅ [추가] 초기 볼륨 설정 (0.0 ~ 1.0)
    [Range(0f, 1f)] // 슬라이더바 형태로 표시됨
    [Tooltip("비디오의 초기 볼륨입니다. (1.0 = 100%, 0.5 = 50%)")]
    public float initialVolume = 1.0f;


    // ✅ [추가] 이 블록 바로 뒤에 올 여백 (음수면 기본값 사용)
    [Header("Layout")]
    [Tooltip("이 블록 아래에 생길 간격입니다. -1로 두면 페이지 기본값을 따릅니다.")]
    public float spacingAfter = -1f;
}

[CreateAssetMenu(fileName = "Manual_Entry", menuName = "Manual/Entry")]
public class ManualEntrySO : ScriptableObject
{
// ✅ [추가] 정렬 순서 (ID)
    [Header("Sort Settings")]
    [Tooltip("메뉴 리스트에 표시될 순서입니다. 숫자가 작을수록 위에 표시됩니다. (예: 1, 2, 3...)")]
    public int entryID = 0;

    [Tooltip("목록에 표시될 제목의 Localization Key (예: 'MANUAL_TITLE_MOVE')")]
    // 🔥 [수정] entryTitle -> titleKey (키값임을 명시)
    public string titleKey;       
    
    [Tooltip("태그의 Localization Key 리스트 (예: 'TAG_BASIC')")]
    // 🔥 [수정] tags -> tagKeys (키값임을 명시)
    public List<string> tagKeys;       
    
    // ✅ [변경] 이름 변경 (헷갈림 방지)
    [Header("Layout Settings")]
    [Tooltip("이 페이지의 기본 줄 간격입니다. (각 블록에서 따로 설정하지 않았을 때 적용)")]
    public float defaultSpacing = 20f;

    [Header("Notion-style Blocks")]
    public List<ManualBlock> blocks; 
}