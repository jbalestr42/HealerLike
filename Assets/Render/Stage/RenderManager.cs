using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Stage
{
    // Attaches the render layer to the game scene once it is loaded
    public class RenderManager : MonoBehaviour
    {
        [SerializeField] CreatureLooks _creatureLooks;
        [SerializeField] SpellLooks _spellLooks;
        [SerializeField] HLPrimitiveMeshes _meshes;
        [SerializeField] RenderPipelineAsset _pipeline;
        [SerializeField] Material _groundMaterial;
        [SerializeField] Light _keyLight;
        [SerializeField] List<string> _hiddenObjectNames = new List<string>();

        EntityManager _entityManager;
        public EntityManager entityManager { get { return _entityManager; } }

        PlayerBehaviour _player;
        public PlayerBehaviour player { get { return _player; } }

        public CreatureLooks creatureLooks { get { return _creatureLooks; } }
        public SpellLooks spellLooks { get { return _spellLooks; } }
        public HLPrimitiveMeshes meshes { get { return _meshes; } }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void Init(EntityManager entityManager, PlayerBehaviour player)
        {
            if (entityManager == null)
            {
                Debug.LogError("[RenderManager] Init needs the EntityManager of the loaded scene.");
                return;
            }

            if (entityManager == _entityManager)
            {
                return;
            }

            _entityManager = entityManager;
            _player = player;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EntityManager entityManager = FindAnyObjectByType<EntityManager>();
            if (entityManager == null)
            {
                return;
            }

            Init(entityManager, FindAnyObjectByType<PlayerBehaviour>());
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
