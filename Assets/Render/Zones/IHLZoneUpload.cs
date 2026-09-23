using System;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Upload side of the zone registry, tests give it a CPU only fake
    public interface IHLZoneUpload : IDisposable
    {
        GraphicsBuffer buffer { get; }

        void Upload(HLZone[] zones);

        void Bind();

        void PublishCount(int count);

        void Unbind();
    }
}
