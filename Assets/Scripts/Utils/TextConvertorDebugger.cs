using Sirenix.OdinInspector;
using UnityEngine;

public class TextConvertorDebugger : MonoBehaviour
{
    [SerializeField] string debugText;
    [SerializeField, ReadOnly] string result;

    [Button]
    public void DebugEvaluateExpression()
    {
        result = TextConvertor.EvaluateExpression(debugText).ToString("F2");
    }
}
