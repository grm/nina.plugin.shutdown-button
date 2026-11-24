# AGENTS.md

Read README.md. Build a small N.I.N.A. dockable Imaging shutdown plugin.

## Safety first
- Stop the sequencer and **wait for confirmed stopped state** before any shutdown hardware actions.
- Never assume Sequence End runs after Stop.
- Mount must report parked before disconnect or REST power-off; unknown/unconnected is not proof of parking.
- On timeout or failure, fail closed (no power-off). Never fake success.
- One coordinator operation at a time; no automatic REST retries.
- No automatic restart, weather sensing, or roof control in V1.
- Do not log REST credentials or store secrets in repository.
- Test against N.I.N.A. simulators/mock HTTP before live hardware.

## UX
- One N.I.N.A. Imaging dockable panel, one prominent button, confirmation, progress and failure state.
- Keep configuration in standard plugin settings.
- Keep adapter integration independent of Target Scheduler internals.

## Quality
- Pin and verify N.I.N.A. SDK API and sequence stop semantics before writing production operations.
- Add Windows build and unit tests, including failed park and stop timeout.
- Never publish a release without dry-run and fault-path validation.