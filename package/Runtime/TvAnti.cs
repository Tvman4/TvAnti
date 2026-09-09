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

            if (config == null)
                config = ScriptableObject.CreateInstance<TvAntiConfig>();

            if (reporter == null)
                reporter = GetComponent<TvAntiReporter>();
            if (reporter == null)
                reporter = gameObject.AddComponent<TvAntiReporter>();

            if (GetComponent<TvAntiRuntime>() == null)
                gameObject.AddComponent<TvAntiRuntime>();
            if (GetComponent<TvAntiExtendedGuardRunner>() == null)
                gameObject.AddComponent<TvAntiExtendedGuardRunner>();

            reporter.Configure(config);
            banManager = new TvAntiBanManager(config, reporter);
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
