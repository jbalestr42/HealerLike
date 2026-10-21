using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Sits on the source of a creature presentation and releases the presentation with it. ExecuteAlways so the
    // release also happens in edit mode, where a host's OnDestroy does not run. Never saved with the source.
    [ExecuteAlways]
    [AddComponentMenu("")]
    public class CreatureAttachmentAnchor : MonoBehaviour
    {
        readonly List<CreatureAttachment> _attachments = new List<CreatureAttachment>();

        public int count { get { return _attachments.Count; } }

        public static CreatureAttachmentAnchor On(GameObject source)
        {
            CreatureAttachmentAnchor anchor = source.GetComponent<CreatureAttachmentAnchor>();
            if (anchor == null)
            {
                anchor = source.AddComponent<CreatureAttachmentAnchor>();
                anchor.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            }

            return anchor;
        }

        public void Add(CreatureAttachment attachment)
        {
            if (!_attachments.Contains(attachment))
            {
                _attachments.Add(attachment);
            }
        }

        void OnDestroy()
        {
            foreach (CreatureAttachment attachment in _attachments)
            {
                attachment.Dispose();
            }

            _attachments.Clear();
        }
    }
}
