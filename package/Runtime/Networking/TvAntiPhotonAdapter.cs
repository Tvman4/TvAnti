using UnityEngine;
namespace TvAnti.Networking {
 public sealed class TvAntiPhotonAdapter:MonoBehaviour {
  [SerializeField] TvAntiRuntime antiCheat;
  public void OnRemoteMovement(string actorId,Vector3 position,Quaternion rotation,Vector3 velocity,int sequence){
   // Forward the snapshot to your trusted backend. Do not trust client authority.
  }
 }
}
