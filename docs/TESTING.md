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

## Select equipment / power outlets

The Imaging dockable contains expandable checkboxes for which N.I.N.A. devices
should **disconnect**: Camera, FilterWheel, Focuser, Rotator, Guider, Telescope.
Saving writes the choices to `%LOCALAPPDATA%\\NINA\\ShutdownButton\\settings.json`.
Parking the mount is mandatory regardless of its Disconnect selection. Stop
sequencer and guiding remain mandatory; the camera warmup follows the camera
disconnect selection.

**Physical power switching is separate from N.I.N.A. disconnection.**
A single REST command that cuts a whole power distribution board cannot
selectively switch off individual outlets. Each independently switchable output
needs its own defined API request and a clear name.

Example, with intentionally non-routable placeholders:
```json
{
  "DryRun": true,
  "EnableRestPowerOff": false,
  "Disconnect": {
    "Camera": true,
    "FilterWheel": true,
    "Focuser": true,
    "Rotator": false,
    "Guider": true,
    "Telescope": true
  },
  "PowerOutputs": [
    {
      "Name": "Camera 12V",
      "Enabled": true,
      "Url": "http://192.0.2.1/api/outlet/camera/off",
      "Method": "POST"
    },
    {
      "Name": "Mount 48V",
      "Enabled": false,
      "Url": "http://192.0.2.1/api/outlet/mount/off",
      "Method": "POST"
    }
  ]
}
```

REST outlets are executed in configured list order, only if both
`DryRun = false` and `EnableRestPowerOff = true`.
A selected outlet must have a name and a valid HTTP endpoint and method.
No blanket fallback to the legacy `RestUrl`. Configuring REST outputs is
currently JSON-only; a graphical outlet editor remains future work.

Do **not** put the power controller itself or safety interlock on an outlet
being cut before all off commands complete. A REST endpoint cannot verify
that the hardware actually switched off without a corresponding readback.
