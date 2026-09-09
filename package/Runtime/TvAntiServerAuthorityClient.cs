using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TvAnti
{
    /// <summary>
    /// Sends local samples to the TvAnti authority process.
    /// Only the JSON action from the server can ban or mute.
    /// </summary>
    public sealed class TvAntiServerAuthorityClient : MonoBehaviour
    {
        [SerializeField] string authorityBaseUrl = "http://127.0.0.1:8787";
        [SerializeField] string ingestKey = "CHANGE_ME_INGEST_KEY";
        [SerializeField] string playerId;
        [SerializeField] string playerName;
        [SerializeField] string playFabTicket;
        [SerializeField] string manifestHash;
        [SerializeField] Transform head;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform rightHand;
        [SerializeField] float snapshotInterval = 0.2f;

        public bool ServerBanned { get; private set; }
        public bool ServerMuted { get; private set; }
        public long MuteUntilUnix { get; private set; }
        public string LastAction { get; private set; }

        public event Action BannedByServer;
        public event Action<long> MutedByServer;

        float nextSnap;

        void Update()
        {
            if (Time.unscaledTime < nextSnap) return;
            nextSnap = Time.unscaledTime + Mathf.Max(0.1f, snapshotInterval);
            StartCoroutine(PostSnapshot());
        }

        public void SetIdentity(string id, string name, string ticket)
        {
            playerId = id;
            playerName = name;
            playFabTicket = ticket;
        }

        public void SubmitVoiceTranscript(string text)
        {
            StartCoroutine(PostJson("/v1/voice", JsonUtility.ToJson(new VoiceBody
            {
                playerId = playerId,
                playerName = playerName,
                transcript = text ?? ""
            })));
        }

        public void SubmitChat(string text)
        {
            var snap = BaseSnapshot();
            snap.chat = text ?? "";
            StartCoroutine(PostJson("/v1/snapshot", JsonUtility.ToJson(snap)));
        }

        public void SubmitClientEvent(string type, string detail)
        {
            StartCoroutine(PostJson("/v1/event", JsonUtility.ToJson(new EventBody
            {
                playerId = playerId,
                playerName = playerName,
                type = type,
                detail = detail ?? ""
            })));
        }

        public IEnumerator RequestJoin(int roomPlayers, Action<bool, string> done)
        {
            string json = JsonUtility.ToJson(new JoinBody
            {
                playerId = playerId,
                playerName = playerName,
                sessionTicket = playFabTicket,
                roomPlayers = roomPlayers
            });
            yield return PostJson("/v1/join", json, done);
        }

        Snapshot BaseSnapshot()
        {
            var s = new Snapshot
            {
                playerId = playerId,
                playerName = playerName,
                timeScale = Time.timeScale,
                clientHash = Application.version,
                manifestHash = manifestHash,
                sessionTicket = playFabTicket
            };
            if (head != null)
            {
                s.position = head.position;
                s.rotation = head.rotation;
                s.head = head.position;
            }
            if (leftHand != null) s.hands.left = leftHand.position;
            if (rightHand != null) s.hands.right = rightHand.position;
            return s;
        }

        IEnumerator PostSnapshot()
        {
            if (string.IsNullOrEmpty(playerId)) yield break;
            yield return PostJson("/v1/snapshot", JsonUtility.ToJson(BaseSnapshot()));
        }

        IEnumerator PostJson(string path, string json, Action<bool, string> done = null)
        {
            string url = authorityBaseUrl.TrimEnd('/') + path;
            byte[] body = Encoding.UTF8.GetBytes(json);
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-TvAnti-Key", ingestKey);
                req.timeout = 8;
                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool err = req.result != UnityWebRequest.Result.Success;
#else
                bool err = req.isNetworkError || req.isHttpError;
#endif
                if (err)
                {
                    done?.Invoke(false, req.error);
                    yield break;
                }

                var resp = JsonUtility.FromJson<AuthorityResponse>(req.downloadHandler.text);
                Apply(resp);
                done?.Invoke(!(resp.banned || resp.action == "kick_banned"), resp.banReason);
            }
        }

        void Apply(AuthorityResponse resp)
        {
            if (resp == null) return;
            LastAction = resp.action;
            ServerBanned = resp.banned || resp.action == "kick_banned" || resp.action == "ban";
            MuteUntilUnix = resp.muteUntil > 0 ? resp.muteUntil : MuteUntilUnix;
            ServerMuted = resp.muted || resp.action == "mute" || MuteUntilUnix > DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (ServerBanned)
            {
                BannedByServer?.Invoke();
                Debug.LogWarning("[TvAnti] server ban: " + resp.banReason + " " + resp.type);
                // Host should disconnect the Photon player from *your* room hook.
                // This client only flags. Photon kick must be done by MasterClient / backend.
            }
            else if (resp.action == "mute")
            {
                MutedByServer?.Invoke(MuteUntilUnix);
            }
        }

        [Serializable] class Vec { public float x, y, z; public static implicit operator Vec(Vector3 v) { return new Vec { x = v.x, y = v.y, z = v.z }; } }
        [Serializable] class Quat { public float x, y, z, w; public static implicit operator Quat(Quaternion q) { return new Quat { x = q.x, y = q.y, z = q.z, w = q.w }; } }
        [Serializable] class Hands { public Vec left; public Vec right; }
        [Serializable]
        class Snapshot
        {
            public string playerId, playerName, sessionTicket, clientHash, manifestHash, transcript, chat;
            public float timeScale;
            public Vec position, head;
            public Quat rotation;
            public Hands hands = new Hands();
        }
        [Serializable] class VoiceBody { public string playerId, playerName, transcript; }
        [Serializable] class EventBody { public string playerId, playerName, type, detail; }
        [Serializable] class JoinBody { public string playerId, playerName, sessionTicket; public int roomPlayers; }
        [Serializable]
        class AuthorityResponse
        {
            public bool ok, accepted, banned, muted;
            public string action, type, detail, banReason, error;
            public long muteUntil;
            public int muteHours;
        }
    }
}
