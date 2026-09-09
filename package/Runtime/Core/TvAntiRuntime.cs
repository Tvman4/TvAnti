using System;
using System.Collections.Generic;
using UnityEngine;

namespace TvAnti
{
    public sealed class TvAntiRuntime : MonoBehaviour
    {
        public static TvAntiRuntime Instance { get; private set; }

        [SerializeField] private TvAntiConfig config;
        [SerializeField] private bool hardStopOnHighConfidenceThreat = true;
        [SerializeField] private float scanInterval = 2.0f;

        private TvAntiMovementValidator movement;
        private TvAntiIntegrityScanner integrity;
        private float score;
        private float lastViolationTime = -999f;
        private float nextScan;

        public event Action<TvAntiEvent> ViolationDetected;
        public float SuspicionScore => score;

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

            movement = new TvAntiMovementValidator(config);
            integrity = new TvAntiIntegrityScanner(config);
        }

        private void Update()
        {
            score = Mathf.Max(0f, score - config.scoreDecayPerSecond * Time.unscaledDeltaTime);

            if (movement != null && TryGetLocalRoot(out Vector3 position))
            {
                if (!movement.Sample(position, out TvAntiEvent violation))
                    Register(violation);
            }

            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + Mathf.Max(0.25f, scanInterval);
                RunIntegrityScan();
            }
        }

        private void RunIntegrityScan()
        {
            foreach (var evt in integrity.ScanLoadedAssemblies())
                RegisterHighConfidence(evt);

            foreach (var evt in integrity.ScanKnownPluginLocations())
                RegisterHighConfidence(evt);
        }

        private bool TryGetLocalRoot(out Vector3 position)
        {
            var root = Camera.main;
            if (root == null)
            {
                position = default;
                return false;
            }

            position = root.transform.position;
            return true;
        }

        public void Register(TvAntiEvent evt)
        {
            if (Time.unscaledTime - lastViolationTime < config.violationCooldown)
                return;

            lastViolationTime = Time.unscaledTime;
            score += Mathf.Clamp(evt.Severity * 25f, 1f, 30f);
            ViolationDetected?.Invoke(evt);
        }

        private void RegisterHighConfidence(TvAntiEvent evt)
        {
            // Exact identity match = high confidence. This avoids banning merely
            // because an unrelated library contains a similar word.
            score = Mathf.Max(score, config.kickScore);
            ViolationDetected?.Invoke(evt);

            if (hardStopOnHighConfidenceThreat)
                StartCoroutine(ControlledStop());
        }

        private System.Collections.IEnumerator ControlledStop()
        {
            // Give networking/UI a frame to flush telemetry before stopping.
            yield return null;
            enabled = false;
            Application.Quit();
        }

        public bool ShouldRestrictSession() => score >= config.kickScore;

        public void ReportServerDecision(bool allowed)
        {
            if (!allowed)
                score = Mathf.Max(score, config.reportScore);
        }
    }
}
