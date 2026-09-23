using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    [CreateAssetMenu(menuName = "HealerLike/Render/Spell Style Table")]
    public class HLSpellStyleTable : ScriptableObject
    {
        [Serializable]
        public struct HLEntry
        {
            public HLSpellSignature signature;
            public GameObject prefab;
        }

        public List<HLEntry> entries = new List<HLEntry>();
        public GameObject buff,
            shield,
            heal,
            impact,
            chain;

        public bool TryGet(HLSpellSignature signature, out GameObject prefab)
        {
            prefab = null;
            if (entries == null)
            {
                return false;
            }
            bool found = false;
            foreach (HLEntry entry in entries)
            {
                if (entry.signature.Equals(signature))
                {
                    if (found)
                    {
                        prefab = null;
                        return false;
                    }
                    found = true;
                    prefab = entry.prefab;
                }
            }
            return found && prefab;
        }

        public IReadOnlyList<int> FindCollisions()
        {
            HashSet<HLSpellSignature> seen = new HashSet<HLSpellSignature>();
            List<int> indices = new List<int>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (!seen.Add(entries[i].signature))
                    {
                        indices.Add(i);
                    }
                }
            }
            return indices;
        }

        public GameObject StatusPrefab(HLSpellSignature signature)
        {
            if (TryGet(signature, out GameObject prefab))
            {
                return prefab;
            }
            return null;
        }
    }
}
