using UnityEngine;
namespace TvAnti {
 public sealed class TvAntiMovement {
  readonly TvAntiConfig c; Vector3 p,v; float time,yaw; bool init;
  public TvAntiMovement(TvAntiConfig c){this.c=c;}
  public bool Sample(Vector3 pos,Quaternion rot,out Violation violation){
   float now=Time.unscaledTime;
   if(!Finite(pos)||!Finite(rot)){violation=new Violation(ViolationType.InvalidNumber,1,"non-finite transform");return false;}
   if(pos.magnitude>c.maxAllowedPositionRadius){violation=new Violation(ViolationType.OutOfBounds,1,$"r={pos.magnitude:F1}");return false;}
   if(!init){init=true;p=pos;v=Vector3.zero;time=now;yaw=rot.eulerAngles.y;violation=default;return true;}
   float dt=now-time;
   if(dt<.001f||dt>1.0f){violation=new Violation(ViolationType.SampleGap,.5f,$"dt={dt:F3}");time=now;p=pos;return false;}
   Vector3 vel=(pos-p)/dt; float hs=new Vector2(vel.x,vel.z).magnitude;
   float acc=(vel-v).magnitude/dt,dist=(pos-p).magnitude;
   float dy=Mathf.Abs(pos.y-p.y),turn=Mathf.Abs(Mathf.DeltaAngle(yaw,rot.eulerAngles.y))/dt;
   p=pos;v=vel;time=now;yaw=rot.eulerAngles.y;
   if(dist>c.maxTeleportDistance){violation=new Violation(ViolationType.Teleport,1,$"d={dist:F2}");return false;}
   if(hs>c.maxHorizontalSpeed){violation=new Violation(ViolationType.Speed,.9f,$"hs={hs:F2}");return false;}
   if(Mathf.Abs(vel.y)>c.maxVerticalSpeed){violation=new Violation(ViolationType.VerticalVelocity,.8f,$"vy={vel.y:F2}");return false;}
   if(acc>c.maxAcceleration){violation=new Violation(ViolationType.Acceleration,.8f,$"a={acc:F2}");return false;}
   if(dy>c.maxHeadHeightDelta){violation=new Violation(ViolationType.HeightDelta,.7f,$"dy={dy:F2}");return false;}
   if(turn>c.maxRotationRate){violation=new Violation(ViolationType.RotationRate,.5f,$"turn={turn:F1}");return false;}
   if(vel.sqrMagnitude>(c.maxHorizontalSpeed*c.maxHorizontalSpeed+c.maxVerticalSpeed*c.maxVerticalSpeed)*1.5f){violation=new Violation(ViolationType.ImpossibleVelocity,.8f,"velocity envelope");return false;}
   violation=default;return true;
  }
  static bool Finite(Vector3 x)=>float.IsFinite(x.x)&&float.IsFinite(x.y)&&float.IsFinite(x.z);
  static bool Finite(Quaternion q)=>float.IsFinite(q.x)&&float.IsFinite(q.y)&&float.IsFinite(q.z)&&float.IsFinite(q.w);
 }
}
