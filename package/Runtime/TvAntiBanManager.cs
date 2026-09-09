using System;
using System.Collections.Generic;
using UnityEngine;

namespace TvAnti
{
    public sealed class TvAntiBanManager
    {
        private readonly TvAntiConfig config;
        private readonly TvAntiReporter reporter;

        private readonly Dictionary<string, float> scores =
            new Dictionary<string, float>();

        private readonly Dictionary<string, float> lastViolation =
            new Dictionary<string, float>();

        public TvAntiBanManager(
            TvAntiConfig config,
            TvAntiReporter reporter)
        {
            this.config = config;
            this.reporter = reporter;
        }

        public float GetScore(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return 0f;

            float value;

            if (!scores.TryGetValue(playerId, out value))
                return 0f;

            return value;
        }

        public bool AddViolation(
            string playerId,
            string playerName,
            Violation violation,
            string roomCode)
        {
            if (string.IsNullOrEmpty(playerId))
                return false;

            if (!ShouldAcceptViolation(playerId, violation.Type))
                return false;

            float score = GetScore(playerId);

            score += Mathf.Clamp01(violation.Severity) * 25f;

            scores[playerId] = score;

            if (reporter != null)
            {
                reporter.ReportDetection(
                    playerId,
                    playerName,
                    violation.Type.ToString(),
                    violation.Detail,
                    roomCode
                );
            }

            return score >= config.restrictionScore;
        }

        private bool ShouldAcceptViolation(
            string playerId,
            ViolationType type)
        {
            string key = playerId + ":" + type;

            float now = Time.unscaledTime;

            float old;

            if (lastViolation.TryGetValue(key, out old))
            {
                if (now - old < config.violationCooldown)
                    return false;
            }

            lastViolation[key] = now;

            return true;
        }

        public void Decay(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            var keys = new List<string>(scores.Keys);

            foreach (string key in keys)
            {
                scores[key] -=
                    config.scoreDecayPerSecond * deltaTime;

                if (scores[key] <= 0f)
                    scores.Remove(key);
            }
        }

        public void ResetPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            scores.Remove(playerId);
        }
    }
}
