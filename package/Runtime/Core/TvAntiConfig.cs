using UnityEngine;
namespace TvAnti {
 [CreateAssetMenu(menuName="TvAnti/Config",fileName="TvAntiConfig")]
 public sealed class TvAntiConfig:ScriptableObject {
  [Header("Movement")][Min(.1f)] public float maxHorizontalSpeed=8.5f;
  [Min(.1f)] public float maxVerticalSpeed=10f;
  [Min(.1f)] public float maxAcceleration=45f;
  [Min(.1f)] public float maxTeleportDistance=3.25f;
  [Min(.01f)] public float sampleInterval=.08f;
  [Header("Scoring")][Min(1)] public int restrictScore=100;
  [Min(0)] public float scoreDecayPerSecond=2.5f;
  [Min(0)] public float violationCooldown=.35f;
  [Header("Integrity")]
  public bool scanManagedAssemblies=true;
  public bool scanNativeModules=true;
  [Header("Backend")]
  public string validationEndpoint="PUT_PLAYFAB_FUNCTION_URL_HERE";
  public string playFabTitleId="PUT_PLAYFAB_TITLE_ID_HERE";
  public string photonAppId="PUT_PHOTON_APP_ID_HERE";
 }
}
