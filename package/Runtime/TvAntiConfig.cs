using System.Collections.Generic;
using UnityEngine;

namespace TvAnti
{
    [CreateAssetMenu(
        menuName = "TvAnti/Config",
        fileName = "TvAntiConfig"
    )]
    public sealed class TvAntiConfig : ScriptableObject
    {
        [Header("PlayFab")]
        public string playFabTitleId = "";

        [Header("TvAnti Backend")]
        [Tooltip("Private HTTPS endpoint belonging to the game developer.")]
        public string moderationEndpoint = "";

        [Header("Detection")]
        public float maxHorizontalSpeed = 8.5f;
        public float maxVerticalSpeed = 10f;
        public float maxAcceleration = 45f;
        public float maxTeleportDistance = 3.25f;
        public float maxHeadHeightDelta = 2.5f;
        public float maxRotationRate = 1080f;

        [Header("Detection Scoring")]
        public float scoreDecayPerSecond = 2.5f;
        public float violationCooldown = 0.35f;
        public float restrictionScore = 100f;
        public float kickScore = 100f;
        public float reportScore = 75f;
        public float sampleInterval = 0.08f;

        [Header("Integrity")]
        public bool scanManagedAssemblies = true;
        public bool scanNativeModules = true;
        public bool scanSo = true;

        [Header("Gorilla Locomotion")]
        public bool preserveGorillaLocomotion = true;

        [Header("Endpoints")]
        public string validationEndpoint = "";
        public string photonAppId = "";

        [Header("Blocked Native Libraries")]
        [Tooltip("Exact native-library basenames only.")]
        public List<string> blockedSoNames = new List<string>
        {
            "flow.lol",
            "flow.lol22",
            "SPM",
            "FlyingCatsStupidMenuV4",
            "AntiCheatBypass2Ma",
            "AntiCheatBypass2022.2and2023.3",
            "NullHoldableV2",
            "RMH",
            "BebiteCheats",
            "cool-lib",
            "TvMenu"
        };

        [Header("Blocked SHA-256")]
        public List<string> blockedSoSha256 = new List<string>();
    }
}
