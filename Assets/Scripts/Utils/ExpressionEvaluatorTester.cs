using Sirenix.OdinInspector;
using UnityEngine;

public class ExpressionEvaluatorTester : MonoBehaviour
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
        result = ExpressionEvaluator.Evaluate(debugText).ToString("F2");
    }

    [Button]
    public void Test()
    {
        ExpressionEvaluator.Test();
    }
}
