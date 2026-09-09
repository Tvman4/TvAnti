using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TvAnti {
    public sealed class TvAntiAdvancedDetectors {
        readonly TvAntiConfig config;
        readonly HashSet<string> baselineAssemblies = new HashSet<string>();
        readonly Dictionary<string,int> lastSequence = new Dictionary<string,int>();
        readonly Dictionary<string,double> lastTimestamp = new Dictionary<string,double>();

        public TvAntiAdvancedDetectors(TvAntiConfig config) {
            this.config = config;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) baselineAssemblies.Add(a.FullName);
        }

        public Violation? RuntimeCodeInjection() => UnexpectedAssembly();
        public Violation? UnexpectedNativePlugin(IEnumerable<string> modules) {
            foreach (var module in modules ?? Enumerable.Empty<string>()) {
                var n = Path.GetFileNameWithoutExtension(module);
                if (config.blockedSoNames.Any(x => string.Equals(x,n,StringComparison.OrdinalIgnoreCase)))
                    return new Violation(ViolationType.BlockedLibrary,1f,"blocked native plugin matched");
            }
            return null;
        }
        public Violation? ModifiedAssembly(string expected,string actual) =>
            FingerprintMismatch(expected,actual,"assembly fingerprint mismatch");
        public Violation? KnownCheatFingerprint(string expected,string actual) =>
            FingerprintMismatch(expected,actual,"known blocked fingerprint");
        public Violation? IntegrityTampering(bool passed) => passed ? (Violation?)null :
            new Violation(ViolationType.LocalStateDivergence,.7f,"integrity verification failed");
        public Violation? ClientValidationBypass(bool clientValid,bool serverValid) => clientValid==serverValid ? (Violation?)null :
            new Violation(ViolationType.RemoteStateDivergence,.65f,"client/server validation disagreement");
        public Violation? ImpossibleVRMovement(float speed) => speed<=config.maxHorizontalSpeed ? (Violation?)null :
            new Violation(ViolationType.Speed,.8f,"movement speed outlier");
        public Violation? ImpossibleLocomotionAcceleration(float acceleration) => acceleration<=config.maxAcceleration ? (Violation?)null :
            new Violation(ViolationType.Acceleration,.8f,"locomotion acceleration outlier");
        public Violation? PositionSpoof(Vector3 client,Vector3 server,float tolerance) => Vector3.Distance(client,server)<=Mathf.Max(.05f,tolerance) ? (Violation?)null :
            new Violation(ViolationType.RemoteStateDivergence,.75f,"position differs from authoritative state");
        public Violation? RotationSpoof(Quaternion client,Quaternion server,float tolerance) => Quaternion.Angle(client,server)<=Mathf.Max(1f,tolerance) ? (Violation?)null :
            new Violation(ViolationType.LocalStateDivergence,.55f,"rotation differs from authoritative state");
        public Violation? MovementPacketReplay(string session,int sequence) {
            if(lastSequence.TryGetValue(session,out var old) && sequence<=old)
                return new Violation(ViolationType.DuplicateSequence,.8f,"movement sequence replay/out of order");
            lastSequence[session]=sequence; return null;
        }
        public Violation? MovementPacketManipulation(Vector3 advertised,Vector3 measured,float tolerance) =>
            (advertised-measured).magnitude<=Mathf.Max(.25f,tolerance) ? (Violation?)null :
            new Violation(ViolationType.ImpossibleVelocity,.65f,"advertised velocity differs from measured velocity");
        public Violation? PacketFlood(float pps,float maxPps) => pps<=maxPps ? (Violation?)null :
            new Violation(ViolationType.PacketBurst,.6f,"packet rate exceeded ceiling");
        public Violation? InvalidNetworkSequence(int sequence) => sequence>=0 ? (Violation?)null :
            new Violation(ViolationType.DuplicateSequence,.7f,"invalid network sequence");
        public Violation? ImpossibleTimestamp(string session,double timestamp) {
            if(lastTimestamp.TryGetValue(session,out var old) && timestamp<=old)
                return new Violation(ViolationType.ClockDrift,.7f,"non-monotonic timestamp");
            lastTimestamp[session]=timestamp; return null;
        }
        public Violation? TamperedSession(string expected,string received) => expected==received ? (Violation?)null :
            new Violation(ViolationType.RemoteStateDivergence,.9f,"session identifier mismatch");
        public Violation? ModifiedPlayerState(string expectedHash,string actualHash) =>
            FingerprintMismatch(expectedHash,actualHash,"player-state integrity mismatch");
        public Violation? ServerAuthorityBypass(bool authorized,bool attemptedMutation) => (!authorized && attemptedMutation) ?
            new Violation(ViolationType.RemoteStateDivergence,1f,"unauthorized client state mutation") : (Violation?)null;
        public Violation? OutOfBounds(Vector3 position,Bounds area) => area.Contains(position) ? (Violation?)null :
            new Violation(ViolationType.OutOfBounds,.75f,"position outside authoritative play area");
        public Violation? UnexpectedAssembly() {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
                if(!baselineAssemblies.Contains(a.FullName)) return new Violation(ViolationType.UnexpectedAssembly,.35f,"runtime assembly appeared after baseline");
            return null;
        }
        public bool Correlated(IEnumerable<Violation> evidence,int independentSignals=3) =>
            evidence!=null && evidence.Select(x=>x.Type).Distinct().Count()>=independentSignals;
        static Violation? FingerprintMismatch(string expected,string actual,string detail) =>
            string.IsNullOrEmpty(expected)||string.IsNullOrEmpty(actual)||string.Equals(expected,actual,StringComparison.OrdinalIgnoreCase) ? (Violation?)null :
            new Violation(ViolationType.LibraryHashMismatch,.9f,detail);
    }
}
