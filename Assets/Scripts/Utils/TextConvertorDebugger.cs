using Sirenix.OdinInspector;
using UnityEngine;

public class TextConvertorDebugger : MonoBehaviour
{
    [SerializeField]
    [OnValueChanged("DebugEvaluateExpression")]
    string debugText;

    [SerializeField]
    [ReadOnly]
    string result;

    [Button]
    public void DebugEvaluateExpression()
    {
        result = TextConvertor.EvaluateExpression(debugText).ToString("F2");
    }

    [Button]
    public void Test()
    {
        TextConvertor.Test();
    }
}
