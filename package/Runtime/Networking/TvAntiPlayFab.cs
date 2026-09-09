using UnityEngine; using UnityEngine.Networking; using System.Collections;
namespace TvAnti.Networking {
 public sealed class TvAntiPlayFab:MonoBehaviour {
  [SerializeField] TvAntiConfig config;
  public void Report(string sessionId,TvAntiEvent e){if(config==null||string.IsNullOrEmpty(config.validationEndpoint)||config.validationEndpoint.StartsWith("PUT_"))return;StartCoroutine(Post(sessionId,e));}
  IEnumerator Post(string id,TvAntiEvent e){string json=JsonUtility.ToJson(new Payload{id=id,type=e.Type.ToString(),severity=e.Severity,detail=e.Detail});using(var r=new UnityWebRequest(config.validationEndpoint,"POST")){r.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));r.downloadHandler=new DownloadHandlerBuffer();r.SetRequestHeader("Content-Type","application/json");yield return r.SendWebRequest();}}
  [System.Serializable] class Payload{public string id,type,detail;public float severity;}
 }
}
