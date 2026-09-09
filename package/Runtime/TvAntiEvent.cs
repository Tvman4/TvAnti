namespace TvAnti {
 public enum ViolationType {
  None,BlockedLibrary,LibraryHashMatch,UnexpectedNativeModule,UnexpectedAssembly,
  Speed,Acceleration,Teleport,VerticalVelocity,HeightDelta,RotationRate,SampleGap,
  ClockDrift,DuplicateSequence,InvalidNumber,OutOfBounds,ImpossibleVelocity,
  InputRate,PacketBurst,RemoteStateDivergence,LocalStateDivergence,CorrelatedAnomaly
 }
 public readonly struct Violation {
  public readonly ViolationType Type; public readonly float Severity; public readonly string Detail;
  public Violation(ViolationType t,float s,string d){Type=t;Severity=s;Detail=d;}
 }
}
