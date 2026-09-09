using UnityEngine;

namespace TvAnti
{
    public sealed class TvAnti : MonoBehaviour
    {
        public static TvAnti Instance { get; private set; }

        [SerializeField]
        private TvAntiConfig config;

        [SerializeField]
        private TvAntiReporter reporter;

        private TvAntiBanManager banManager;

        public TvAntiConfig Config => config;
        public TvAntiBanManager BanManager => banManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (reporter == null)
                reporter = GetComponent<TvAntiReporter>();

            if (reporter != null)
                reporter.Configure(config);

            if (config != null)
                banManager = new TvAntiBanManager(
                    config,
                    reporter
                );
        }

        private void Update()
        {
            if (banManager != null)
                banManager.Decay(Time.unscaledDeltaTime);
        }

        public bool ProcessViolation(
            string playerId,
            string playerName,
            Violation violation,
            string roomCode)
        {
            if (banManager == null)
                return false;

            return banManager.AddViolation(
                playerId,
                playerName,
                violation,
                roomCode
            );
        }

        public void ReportPlayer(
            string targetPlayerId,
            string targetPlayerName,
            string reason,
            string reporterPlayerId,
            string reporterPlayerName,
            string roomCode)
        {
            if (reporter != null)
            {
                reporter.ReportPlayer(
                    targetPlayerId,
                    targetPlayerName,
                    reason,
                    reporterPlayerId,
                    reporterPlayerName,
                    roomCode
                );
            }
        }
    }
}
