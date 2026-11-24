# Testing procedure — no equipment power-off by default

The experimental plugin is NOT validated for unattended operation.

1. Build on Windows and ensure N.I.N.A. loads the exported Imaging dockable.
2. Configure only a simulator mount and camera; do not connect real telescope or power controller.
3. Check the button confirms before issuing Stop.
4. Start an advanced sequence and verify Stop cancels the exposure and fully settles before park.
5. Test no active sequence, mount disconnected, mount already parked, park command failure, and stop timeout.
6. Check that failed/unknown park **never sends the REST request**.
7. Only after validating the state machine, set the REST URL in
   `%LOCALAPPDATA%\\NINA\\ShutdownButton\\settings.json`.
   Example: `{"DryRun":true,"EnableRestPowerOff":false,"RestUrl":"","RestMethod":"POST"}`.
8. Never set `DryRun:false` and `EnableRestPowerOff:true` on real hardware
   without controlled, attended tests and an independent safety interlock.

V1 does not close a manual roof, monitor weather, or promise an emergency stop.
