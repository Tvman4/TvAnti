namespace TvAnti {
 public enum TvAntiViolation { None, ImpossibleSpeed, ImpossibleAcceleration, Teleport, VerticalAnomaly, UnexpectedAssembly, UnexpectedNativeModule, ClockAnomaly }
 public readonly struct TvAntiEvent {
  public readonly TvAntiViolation Type; public readonly float Severity; public readonly string Detail;
  public TvAntiEvent(TvAntiViolation type,float severity,string detail){Type=type;Severity=severity;Detail=detail;}
 }
}
