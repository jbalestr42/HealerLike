using UnityEngine;

namespace HealerLike.Render.Zones
{
    // The zone table on the GPU, bound as a global buffer for the grass and the rings
    public class ZoneGraphicsUpload : IZoneUpload
    {
        static readonly int zonesId = Shader.PropertyToID("_HL_Zones");
        static readonly int countId = Shader.PropertyToID("_HL_ZoneCount");

        GraphicsBuffer _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64, Zone.Stride);
        public GraphicsBuffer buffer { get { return _buffer; } }

        public void Upload(Zone[] zones)
        {
            _buffer.SetData(zones);
        }

        public void Bind()
        {
            Shader.SetGlobalBuffer(zonesId, _buffer);
        }

        public void PublishCount(int count)
        {
            Shader.SetGlobalInt(countId, count);
        }

        public void Unbind()
        {
            Shader.SetGlobalBuffer(zonesId, (GraphicsBuffer)null);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}
