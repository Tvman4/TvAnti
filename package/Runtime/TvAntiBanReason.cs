namespace TvAnti
{
    public enum TvAntiBanReason
    {
        BlockedNativeLibrary,
        KnownCheatFingerprint,
        RuntimeCodeInjection,
        ModifiedAssembly,
        IntegrityTampering,
        ClientValidationBypass,
        ImpossibleVRMovement,
        ImpossibleLocomotionAcceleration,
        PositionSpoofing,
        RotationSpoofing,
        MovementPacketReplay,
        MovementPacketManipulation,
        PacketFlood,
        InvalidNetworkSequence,
        ImpossibleTimestamp,
        TamperedSession,
        ModifiedPlayerState,
        ServerAuthorityBypass,
        OutOfBounds,
        MultipleDetections
    }
}
