using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Tracks whether the application (or play mode, in the Editor) is shutting down, shared by every
/// Singleton<T> so that none of them gets lazily re-created while scene objects are being destroyed.
/// </summary>
static class SingletonShutdown
{
    public static bool isQuitting { get; private set; }

    // Also runs when entering play mode with domain reload disabled, so the flag never leaks from a
    // previous session.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        isQuitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    static void OnQuitting()
    {
        isQuitting = true;
    }
}

/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// 
/// As a note, this is made as MonoBehaviour because we need Coroutines.
/// </summary>
public class Singleton<T> : SerializedMonoBehaviour where T : SerializedMonoBehaviour
{
    private static T _instance;
    private static object _lock = new object();

    public static T instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindAnyObjectByType(typeof(T));

                    if (FindObjectsByType(typeof(T)).Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong " +
                            " - there should never be more than 1 singleton!" +
                            " Reopening the scene might fix it.");
                        return _instance;
                    }

                    // Never create a new instance while shutting down (it would survive as a ghost
                    // object in the Editor scene) nor in edit mode (e.g. from tests or editor code).
                    if (_instance == null && (SingletonShutdown.isQuitting || !Application.isPlaying))
                    {
                        return null;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();

                        DontDestroyOnLoad(singleton);

                        Debug.Log("[Singleton] An instance of " + typeof(T) +
                            " is needed in the scene, so '" + singleton +
                            "' was created with DontDestroyOnLoad.");
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// The instance if one exists, never creating it - for code that may run while the scene is being
    /// torn down (e.g. OnDestroy), where the singleton may already be gone.
    /// </summary>
    public static T existingInstance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (T)FindAnyObjectByType(typeof(T));
            }
            return _instance;
        }
    }

    /// <summary>
    /// When Unity quits, it destroys objects in a random order, so another object's OnDestroy may
    /// still access `instance` after this one is gone: `instance` then returns null (see
    /// SingletonShutdown) instead of creating a ghost object that would stay in the Editor scene.
    /// </summary>
    public void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}