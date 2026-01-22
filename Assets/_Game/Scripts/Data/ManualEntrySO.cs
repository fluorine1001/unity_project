using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

// 블록의 종류 (Notion의 "/" 명령어와 유사)
public enum ManualBlockType 
{ 
    Heading1,       // 큰 제목
    Heading2,       // 중간 제목
    BodyText,       // 일반 본문
    Image,          // 이미지
    Video,          // 동영상
    Spacer          // 여백 (줄바꿈/간격 조절용)
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
    public string textContent;      // 텍스트 내용
    public Sprite imageContent;     // 이미지 파일
    public VideoClip videoContent;  // 비디오 파일

    [Header("Style Options")]
    public BlockAlignment alignment = BlockAlignment.Left; // 정렬
    
    [Tooltip("이미지/비디오의 높이, 또는 여백의 높이를 지정합니다.")]
    public float sizeValue = 200f;  // 높이 값 (텍스트는 폰트 크기로 자동 조절)
}

// 매뉴얼 항목 하나 (검색과 태그의 대상이 되는 파일)
[CreateAssetMenu(fileName = "Manual_Entry", menuName = "Manual/Entry")]
public class ManualEntrySO : ScriptableObject
{
    public string entryTitle;       // 항목 이름 (검색용)
    public List<string> tags;       // 태그 리스트 (필터용)
    
    [Header("Notion-style Blocks")]
    public List<ManualBlock> blocks; // 블록들을 순서대로 쌓음
}