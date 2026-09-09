using UnityEngine;
namespace TvAnti {
 public sealed class TvAntiMovementValidator {
  readonly TvAntiConfig c; bool ready; Vector3 p,v; float t;
  public TvAntiMovementValidator(TvAntiConfig config){c=config;}
  public bool Sample(Vector3 position,out TvAntiEvent evt){
   float now=Time.unscaledTime;
   if(!ready){ready=true;p=position;v=Vector3.zero;t=now;evt=default;return true;}
   float dt=Mathf.Max(.001f,now-t); Vector3 d=position-p; Vector3 nv=d/dt;
   float hs=new Vector2(nv.x,nv.z).magnitude, vs=Mathf.Abs(nv.y), a=(nv-v).magnitude/dt;
   p=position;v=nv;t=now;
   if(d.magnitude>c.maxTeleportDistance){evt=new TvAntiEvent(TvAntiViolation.Teleport,Mathf.Clamp01(d.magnitude/c.maxTeleportDistance),$"distance={d.magnitude:F2}");return false;}
   if(hs>c.maxHorizontalSpeed){evt=new TvAntiEvent(TvAntiViolation.ImpossibleSpeed,Mathf.Clamp01(hs/c.maxHorizontalSpeed),$"horizontalSpeed={hs:F2}");return false;}
   if(vs>c.maxVerticalSpeed){evt=new TvAntiEvent(TvAntiViolation.VerticalAnomaly,Mathf.Clamp01(vs/c.maxVerticalSpeed),$"verticalSpeed={vs:F2}");return false;}
   if(a>c.maxAcceleration){evt=new TvAntiEvent(TvAntiViolation.ImpossibleAcceleration,Mathf.Clamp01(a/c.maxAcceleration),$"acceleration={a:F2}");return false;}
   evt=default; return true;
  }
 }
}
