using System.Collections.Generic;
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
    // Lists only the data matching the tags, when the data has tags (e.g. items, units); nothing otherwise
    public virtual void SetTagFilter(List<GameplayTag> includedTags, List<GameplayTag> excludedTags) {}
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

    // Only the data it accepts are listed in the menu (e.g. the items having some tags), all of them when null
    System.Func<DataType, bool> _filter;
    public System.Func<DataType, bool> filter { get { return _filter; } set { _filter = value; } }

    // Fills each new data before it's edited (e.g. the tags every data of the section has)
    System.Action<DataType> _initData;
    public System.Action<DataType> initData
    {
        get { return _initData; }
        set
        {
            _initData = value;
            if (_data != null && _initData != null)
            {
                _initData(_data);
            }
        }
    }

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
            _initData?.Invoke(_data);
        }
    }

    public override void SetTagFilter(List<GameplayTag> includedTags, List<GameplayTag> excludedTags)
    {
        if (typeof(ITaggable).IsAssignableFrom(typeof(DataType)))
        {
            _filter = data => TagFilter.Matches(data as ITaggable, includedTags, excludedTags);
        }
    }

    public override void AddTree(OdinMenuTree tree)
    {
        if (!string.IsNullOrEmpty(_menuName))
        {
            tree.Add(_menuName, this);
        }
        if (_filter == null)
        {
            tree.AddAllAssetsAtPath(_menuName, _path, typeof(DataType), _isRecursive, true);
            return;
        }

        // Same listing as AddAllAssetsAtPath (flattened, by file name), the data the filter refuses left out
        string folder = _path.TrimEnd('/');
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(DataType).Name}", new[] { folder }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!_isRecursive && Path.GetDirectoryName(assetPath).Replace('\\', '/') != folder)
            {
                continue;
            }

            DataType asset = AssetDatabase.LoadAssetAtPath<DataType>(assetPath);
            if (asset != null && _filter(asset))
            {
                string name = Path.GetFileNameWithoutExtension(assetPath);
                tree.Add(string.IsNullOrEmpty(_menuName) ? name : $"{_menuName}/{name}", asset);
            }
        }
    }
}