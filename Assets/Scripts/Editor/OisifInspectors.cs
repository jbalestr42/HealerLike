using Oisif.Editor;
using UnityEditor;
using UnityEngine;

// The Oisif Inspector draws every asset and component of the game that has no inspector of its own: more
// precise than the editors registered for every Object, less than the ones of a given type
[CustomEditor(typeof(ScriptableObject), true)]
[CanEditMultipleObjects]
public class DataInspector : AttributeEditor {}

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class BehaviourInspector : AttributeEditor {}
