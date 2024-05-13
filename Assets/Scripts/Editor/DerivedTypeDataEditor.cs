using System;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

public class DerivedTypeDataEditor<DataType> : ABaseDataEditor where DataType : ScriptableObject
{
    [SerializeField] string _name = "New";

    DataType _data;
    public override ScriptableObject data => _data;

    string _path;
    public string path { get { return _path; } set { _path = value; } }

    bool _isRecursive = true;
    public bool isRecursive { get { return _isRecursive; } set { _isRecursive = value; } }

    string _menuName;

    private GenericSelector<Type> _selector;

    public DerivedTypeDataEditor(string menuName, string dataPath)
    {
        _path = dataPath;
        _menuName = menuName;

        var types = TypeCache.GetTypesDerivedFrom<DataType>();
        _selector = new GenericSelector<Type>(types.Where(type => !type.IsGenericType));
        _selector.EnableSingleClickToSelect();
        _selector.SelectionConfirmed += selections =>
        {
            string directory = Path.Combine(_path, _name);
            string path = Path.Combine(directory, _name.Replace("/", " ") + ".asset").Replace("\\", "/");
            if (!Directory.Exists(directory))
            {
                var selected = selections.First();
                Directory.CreateDirectory(directory);
                var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
                _data = (DataType)ScriptableObject.CreateInstance(selected);
                AssetDatabase.CreateAsset(_data, uniquePath);
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = _data;
                EditorGUIUtility.PingObject(_data);
            }
            else
            {
                Debug.LogError("[DEBUG] Asset already exists at path.");
            }
        };
    }

    [Button("Create")]
    private void CreateNewData()
    {
        _selector.ShowInPopup();
    }

    public override void AddTree(OdinMenuTree tree)
    {
        if (!string.IsNullOrEmpty(_menuName))
        {
            tree.Add(_menuName, this);
        }
        tree.AddAllAssetsAtPath(_menuName, _path, typeof(DataType), _isRecursive, true);
    }
}