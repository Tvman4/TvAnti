using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TvAnti
{
    public sealed class TvAntiReporter : MonoBehaviour
    {
        public static TvAntiReporter Instance { get; private set; }

        [SerializeField]
        private TvAntiConfig config;

        public TvAntiConfig Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Configure(TvAntiConfig newConfig)
        {
            config = newConfig;
        }

        public void ReportPlayer(
            string targetPlayerId,
            string targetPlayerName,
            string reportReason,
            string reporterPlayerId,
            string reporterPlayerName,
            string roomCode)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.moderationEndpoint))
                return;

            var report = new TvAntiPlayerReport
            {
                titleId = config.playFabTitleId,

                targetPlayerId = targetPlayerId,
                targetPlayerName = targetPlayerName,

                reportReason = reportReason,

                reporterPlayerId = reporterPlayerId,
                reporterPlayerName = reporterPlayerName,

                roomCode = roomCode,

                timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            StartCoroutine(PostJson(
                "/report",
                JsonUtility.ToJson(report)
            ));
        }

        public void ReportDetection(
            string playerId,
            string playerName,
            string detection,
            string reason,
            string roomCode)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.moderationEndpoint))
                return;

            var request = new TvAntiBanRequest
            {
                titleId = config.playFabTitleId,

                playerId = playerId,
                playerName = playerName,

                detection = detection,
                reason = reason,

                roomCode = roomCode,

                permanent = true,

                clientVersion = Application.version,
                packageVersion = "TvAnti",

                timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            StartCoroutine(PostJson(
                "/detection",
                JsonUtility.ToJson(request)
            ));
        }

        private IEnumerator PostJson(string path, string json)
        {
            string baseUrl = config.moderationEndpoint.TrimEnd('/');
            string url = baseUrl + "/" + path.TrimStart('/');

            byte[] body = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request =
                   new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler =
                    new UploadHandlerRaw(body);

                request.downloadHandler =
                    new DownloadHandlerBuffer();

                request.SetRequestHeader(
                    "Content-Type",
                    "application/json"
                );

                request.timeout = 10;

                yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    Debug.LogWarning(
                        "[TvAnti] Moderation request failed: " +
                        request.error
                    );
                }
            }
        }
    }
}
