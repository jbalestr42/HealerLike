using System;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // A real EntityData, its production derivation and a private preview. It never edits the source asset.
    public class CreatureRosterRow : IDisposable
    {
        readonly CreatureStudioPreview _preview = new CreatureStudioPreview();
        EntityData _source;
        UnitChannels _channels;
        CreatureRecipe _recipe;
        Texture2D _image;

        public EntityData source { get { return _source; } }
        public UnitChannels channels { get { return _channels; } }
        public CreatureRecipe recipe { get { return _recipe; } }
        public CreatureStudioPreview preview { get { return _preview; } }
        public string path { get { return AssetDatabase.GetAssetPath(_source); } }

        public void Init(EntityData source)
        {
            _source = source;
            _preview.Init();
        }

        public void Rebuild(LookVocabulary vocabulary, Entity.EntityType side)
        {
            _channels = LookDerivation.Channels(_source, side);
            CreatureRecipe next = null;
            if (vocabulary && vocabulary.palette)
            {
                next = LookComposer.Compose(_channels, vocabulary);
            }
            if (_recipe)
            {
                UnityEngine.Object.DestroyImmediate(_recipe);
            }
            _recipe = next;
            if (_recipe)
            {
                _recipe.name = _source.name + " derived";
                _recipe.hideFlags = HideFlags.HideAndDontSave;
            }
            _preview.side = _channels.side;
            _preview.Refresh();
            if (_image)
            {
                UnityEngine.Object.DestroyImmediate(_image);
            }
            _image = null;
        }

        public Texture2D Image()
        {
            if (!_image && _recipe)
            {
                _image = _preview.Capture(_recipe, 1.25f, 360, 300);
                if (_image)
                {
                    _image.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return _image;
        }

        public string ChannelLabel()
        {
            string label = $"{_channels.head} / {_channels.count} / {_channels.stem}\n"
                + $"{_channels.mass} / {_channels.reach} / {_channels.accent}\n"
                + $"{_channels.accessory} ({_channels.accessoryHead})";
            if (LookDerivation.Primary(_source) == null)
            {
                label += "\nPassive unit: Bud fallback";
            }
            return label;
        }

        public void Dispose()
        {
            _preview.Dispose();
            if (_recipe)
            {
                UnityEngine.Object.DestroyImmediate(_recipe);
            }
            if (_image)
            {
                UnityEngine.Object.DestroyImmediate(_image);
            }
            _recipe = null;
            _image = null;
        }
    }
}
