# TvAnti compile fixes

- Removed duplicate TvAntiConfig in Core (same type defined twice).
- Merged missing config fields: kickScore, reportScore, sampleInterval, scan flags, validationEndpoint, photonAppId, scanSo.
- BanManager now uses Violation.Severity instead of missing Confidence.
- Added LibraryHashMismatch to ViolationType.
- Added UnexpectedNativeModule to TvAntiViolation.
- Added missing TvAntiPlayerReport DTO used by TvAntiReporter.

