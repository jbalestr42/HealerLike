using UnityEngine;
namespace HealerLike.Render.Stones
{
    public readonly struct HLStoneImpact
    {
        public readonly Vector3 PointWS, NormalWS, IncomingVelocityWS;
        public readonly bool Estimated;
        public HLStoneImpact(Vector3 pointWS,Vector3 normalWS,Vector3 incomingVelocityWS,bool estimated)
        { PointWS=pointWS; NormalWS=normalWS; IncomingVelocityWS=incomingVelocityWS; Estimated=estimated; }
    }
    public static class HLStoneImpactLocator
    {
        public static bool TryClosestPoint(in HLStoneMeshData mesh,in Matrix4x4 localToWorld,Vector3 queryWS,out Vector3 pointWS,out Vector3 normalWS)
        {
            pointWS=default; normalWS=Vector3.up; float best=float.PositiveInfinity;
            if(mesh.Indices==null) return false;
            for(int i=0;i<mesh.Indices.Length;i+=3)
            {
                Vector3 a=localToWorld.MultiplyPoint3x4(mesh.Vertices[mesh.Indices[i]]);
                Vector3 b=localToWorld.MultiplyPoint3x4(mesh.Vertices[mesh.Indices[i+1]]);
                Vector3 c=localToWorld.MultiplyPoint3x4(mesh.Vertices[mesh.Indices[i+2]]);
                Vector3 cross=Vector3.Cross(b-a,c-a); if(cross.sqrMagnitude<=0) continue;
                Vector3 point=Closest(queryWS,a,b,c); float distance=(point-queryWS).sqrMagnitude;
                if(distance>=best) continue;
                best=distance; pointWS=point; normalWS=cross/Mathf.Sqrt(cross.sqrMagnitude);
            }
            return float.IsFinite(best);
        }
        // Voronoi regions of a triangle, evaluated in world space for nonuniform scale.
        static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            Vector3 ab=b-a, ac=c-a, ap=p-a; float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
            if(d1<=0 && d2<=0) return a;
            Vector3 bp=p-b; float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);
            if(d3>=0 && d4<=d3) return b;
            float vc=d1*d4-d3*d2; if(vc<=0 && d1>=0 && d3<=0) return a+ab*(d1/(d1-d3));
            Vector3 cp=p-c; float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);
            if(d6>=0 && d5<=d6) return c;
            float vb=d5*d2-d1*d6; if(vb<=0 && d2>=0 && d6<=0) return a+ac*(d2/(d2-d6));
            float va=d3*d6-d5*d4; if(va<=0 && d4-d3>=0 && d5-d6>=0) return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
            float inv=1/(va+vb+vc); return a+ab*(vb*inv)+ac*(vc*inv);
        }
    }
}
