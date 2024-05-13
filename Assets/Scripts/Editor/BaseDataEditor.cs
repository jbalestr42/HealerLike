using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

public abstract class ABaseDataEditor
{
    public abstract ScriptableObject data { get; }
    public abstract void AddTree(OdinMenuTree tree);
}

public class BaseDataEditor<DataType> : ABaseDataEditor where DataType : ScriptableObject
{
    public delegate string GetDataName(DataType data);

    [ShowIf("_canCreate")]
    [SerializeField]
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    DataType _data;
    public override ScriptableObject data => _data;

    string _path;
    public string path { get { return _path; } set { _path = value; } }

    bool _canCreate = true;
    public bool canCreate { get { return _canCreate; } set { _canCreate = value; } }

    bool _isRecursive = true;
    public bool isRecursive { get { return _isRecursive; } set { _isRecursive = value; } }

    bool _createFolder = true;
    public bool createFolder { get { return _createFolder; } set { _createFolder = value; } }

    string _menuName;

    GetDataName _getDataName = (DataType data) => typeof(DataType).GetNiceName();
    public GetDataName getDataName { get { return _getDataName; } set { _getDataName = value; } }

    public BaseDataEditor(string menuName, string dataPath)
    {
        _path = dataPath;
        _menuName = menuName;
        CreateData();
    }

    [ShowIf("_canCreate")]
    [Button("Create")]
    private void CreateNewData()
    {
        string path = GetSavePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.CreateAsset(_data, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = _data;
        EditorGUIUtility.PingObject(_data);
        CreateData();
    }

    public string GetSavePath()
    {
        string name = _getDataName(_data);
        return Path.Combine(this.path, _createFolder ? name : "", name.Replace("/", " ") + ".asset");
    }

    public void CreateData()
    {
        if (_canCreate)
        {
            _data = ScriptableObject.CreateInstance<DataType>();
            _data.name = AssetDatabase.GenerateUniqueAssetPath(typeof(DataType).GetNiceName());
        }
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