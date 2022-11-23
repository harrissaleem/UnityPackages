using UnityEngine;

[CreateAssetMenu(fileName = "Text", menuName = "TextFile")]
public class Readme : ScriptableObject
{
    [TextArea(20, 20)]
    public string Text;
}
