using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // Recipients collected in one frame share cast links; later frames begin a fresh group.
    public class GroupImpactLinks
    {
        struct Recipient
        {
            public GameObject target;
            public EffectFamily family;
        }

        readonly Dictionary<GameObject, List<Recipient>> _groups = new Dictionary<GameObject, List<Recipient>>();

        public void Add(GameObject source, GameObject target, EffectFamily family)
        {
            if (!_groups.TryGetValue(source, out List<Recipient> recipients))
            {
                recipients = new List<Recipient>();
                _groups[source] = recipients;
            }
            foreach (Recipient recipient in recipients)
            {
                if (recipient.target == target)
                {
                    return;
                }
            }
            recipients.Add(new Recipient { target = target, family = family });
        }

        public void Flush(ImpactPool impacts, bool isShown)
        {
            if (isShown)
            {
                foreach (KeyValuePair<GameObject, List<Recipient>> group in _groups)
                {
                    if (group.Key != null)
                    {
                        Link(impacts, group.Key, group.Value);
                    }
                }
            }
            Clear();
        }

        public void Clear()
        {
            _groups.Clear();
        }

        static void Link(ImpactPool impacts, GameObject caster, List<Recipient> recipients)
        {
            bool fromScreen = CharacterView.ScreenSource(caster);
            Vector3 start = EffectPlacement.Anchors(caster).castPoint;
            foreach (Recipient recipient in recipients)
            {
                if (recipient.target != null && (fromScreen || Count(recipients, recipient.family) >= 2))
                {
                    SpellEffect link = impacts.ShowLink(start, EffectPlacement.Anchors(recipient.target).bodyCentre,
                        recipient.family, false, fromScreen);
                    if (link)
                    {
                        link.SetCastSource(caster);
                    }
                }
            }
        }

        static int Count(List<Recipient> recipients, EffectFamily family)
        {
            int count = 0;
            foreach (Recipient recipient in recipients)
            {
                if (recipient.family == family)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
