using System; using UnityEngine;
namespace TvAnti.Networking {
 [Serializable] public struct TvAntiMovementSnapshot { public string sessionId; public double serverSequenceTime; public Vector3 position; public Quaternion rotation; public Vector3 velocity; public int sequence; }
 [Serializable] public struct TvAntiServerDecision { public bool accepted; public float suspicion; public string reason; }
 public interface ITvAntiServerValidator { TvAntiServerDecision Validate(TvAntiMovementSnapshot snapshot); }
}
