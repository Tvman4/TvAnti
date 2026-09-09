using UnityEngine;
using System.Collections.Generic;
namespace TvAnti {
 [System.Serializable] public struct MovementSnapshot { public string sessionId; public int sequence; public double serverTime; public Vector3 position; public Quaternion rotation; public Vector3 velocity; }
 [System.Serializable] public struct ServerDecision { public bool accepted; public float suspicion; public string reason; }
 public sealed class TvAntiServer {
  readonly TvAntiConfig c; readonly Dictionary<string,MovementSnapshot> last=new Dictionary<string,MovementSnapshot>();
  public TvAntiServer(TvAntiConfig c){this.c=c;}
  public ServerDecision Validate(MovementSnapshot s){
   if(!Finite(s.position)||!Finite(s.velocity))return Reject(1,"invalid_number");
   if(s.position.magnitude>c.maxAllowedPositionRadius)return Reject(.8f,"out_of_bounds");
   if(last.TryGetValue(s.sessionId,out var p)){
    double raw=s.serverTime-p.serverTime; if(raw<0||raw>2)return Reject(.5f,"clock_drift");
    float dt=Mathf.Clamp((float)raw,.02f,.25f); float d=Vector3.Distance(s.position,p.position);
    if(s.sequence<=p.sequence)return Reject(.6f,"sequence_replay");
    if(d>c.maxHorizontalSpeed*dt+c.maxTeleportDistance)return Reject(.9f,"movement_outlier");
    Vector3 implied=(s.position-p.position)/dt;
    if((implied-s.velocity).magnitude>c.maxAcceleration*1.5f)return Reject(.7f,"velocity_divergence");
   }
   last[s.sessionId]=s; return new ServerDecision{accepted=true,suspicion=0,reason="ok"};
  }
  ServerDecision Reject(float s,string r)=>new ServerDecision{accepted=false,suspicion=s,reason=r};
  static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
 }
}
