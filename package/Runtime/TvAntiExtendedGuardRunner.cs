using UnityEngine;

namespace TvAnti
{
    /// <summary>
    /// Attach next to the existing TvAnti / TvAntiRuntime component.
    /// Samples local VR rig transforms each frame and feeds the extended guards.
    /// Photon and PlayFab checks are invoked from your network callbacks, not here.
    /// </summary>
    public sealed class TvAntiExtendedGuardRunner : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform rightHand;
        [SerializeField] string localPlayerId = "local";
        [SerializeField] string localPlayerName = "local";
        [SerializeField] string roomCode = "";
        [SerializeField] float maxArmMeters = 1.15f;
        [SerializeField] LayerMask solidMask = ~0;

        TvAntiExtendedGuards guards;
        TvAntiConfig config;
        Vector3 lastPos;
        Quaternion lastRot;
        bool primed;

        void Awake()
        {
            var host = TvAnti.Instance;
            config = host != null ? host.Config : ScriptableObject.CreateInstance<TvAntiConfig>();
            guards = new TvAntiExtendedGuards(config);
        }

        void Update()
        {
            if (guards == null) return;

            Report(guards.TimeScaleIntegrityCheck());
            Report(guards.IntegrityHeartbeatMonitor());

            if (head == null)
            {
                if (Camera.main != null) head = Camera.main.transform;
                else return;
            }

            Vector3 pos = head.position;
            Quaternion rot = head.rotation;

            if (!primed)
            {
                lastPos = pos;
                lastRot = rot;
                primed = true;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            Report(guards.SpeedLimitCheck(lastPos, pos, dt));
            Report(guards.PositionSanityFilter(localPlayerId, pos, config != null ? config.maxTeleportDistance : 3.25f));
            Report(guards.RotationSpeedLimiter(lastRot, rot, dt, config != null ? config.maxRotationRate : 1080f));
            Report(guards.HeadRollPitchLimitChecker(rot));
            Report(guards.GravityAndGroundCheck(pos, (pos - lastPos) / Mathf.Max(dt, 0.0001f)));
            Report(guards.NoClipCollisionEnforcer(lastPos, pos, solidMask));

            if (leftHand != null && rightHand != null)
                Report(guards.ArmLengthValidator(pos, leftHand.position, rightHand.position, maxArmMeters));

            lastPos = pos;
            lastRot = rot;
        }

        public TvAntiExtendedGuards Guards => guards;

        public void NotifyRpc(string rpcName, object[] payload)
        {
            Report(guards.RPCValidationAndRateLimiter(localPlayerId, rpcName, payload));
        }

        public void NotifyTag()
        {
            Report(guards.TagCooldownEnforcer(localPlayerId));
        }

        public void NotifyVoiceTranscript(string text)
        {
            Report(guards.VoiceChecker(text));
        }

        public void NotifyPlayFabTicket(string ticket)
        {
            Report(guards.PlayFabAuthenticationHook(ticket));
        }

        void Report(Violation? v)
        {
            if (!v.HasValue) return;
            if (TvAnti.Instance == null) return;
            TvAnti.Instance.ProcessViolation(localPlayerId, localPlayerName, v.Value, roomCode);
        }
    }
}
