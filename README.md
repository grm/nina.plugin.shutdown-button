# N.I.N.A. Shutdown Button

Minimal manual shutdown plugin for N.I.N.A.

> STATUS: design only. This repository does not yet contain an installable or hardware-tested plugin.

## Button location

Use an exported dockable panel in the **Imaging** workspace, titled **Shutdown Telescope**. The panel contains one Shutdown Telescope button, a confirmation dialog, a live step/status label and errors. Avoid patching the N.I.N.A. main toolbar.

## V1 workflow

1. Confirm the user intent and lock out repeated clicks.
2. Request **Stop** of the N.I.N.A. sequencer, cancel exposure and wait for the sequencer to report fully stopped. This includes Target Scheduler.
3. Stop guiding and tracking as appropriate.
4. Park the telescope and **verify the mount reports parked**.
5. Optionally warm camera in a controlled way before shutdown.
6. Disconnect configured equipment.
7. Invoke a configured authenticated REST request to switch off power.
8. Report success or the exact failed step.

Stop must occur before shutdown. Never rely on the N.I.N.A. Sequence End actions running after Stop: shutdown must be orchestrated outside the stopped sequence.

## Safety invariants

- Never send REST power-off when stopping the sequence has timed out, mount is still moving, parking failed, mount state is unknown, or it is merely disconnected without confirmed parking.
- Do not mistake this for emergency-stop hardware or weather safety equipment.
- For an installation with a manually operated roof, this plugin cannot close or verify the roof.
- Keep the sequence stopped; do not automatically restart on improving weather.
- Do not automatically retry a failed power-off, and prevent simultaneous shutdown runs.
- Use redacted error messages and keep credentials out of source control.
- First tests use equipment simulators, REST mock and dry-run mode, not live power cutting.

## Proposed components

- N.I.N.A. plugin manifest and persisted settings.
- Imaging dockable view model and view.
- Shutdown coordinator state machine independent from the sequencer lifetime.
- N.I.N.A. stop, guide, mount, camera and equipment adapters.
- Configurable REST power controller with timeouts.
- Automated tests for failure and timeout branches.

## Acceptance tests

- Running Target Scheduler or ordinary sequence: exposure interrupted, sequencer actually stopped, park verified, rest of shutdown executed.
- No running sequence: shutdown still operates.
- Already parked mount: shutdown may continue after confirmation.
- Unknown mount state, failed park, stop timeout: **REST request blocked**.
- Duplicate button clicks: only one operation.
- HTTP error: correctly reported; no false success.
- Restart after interruption: does not automatically resume shutdown.

## Next development step

Verify the exact N.I.N.A. SDK stop/cancellation interface and compile a real plugin against a pinned N.I.N.A. release, with Windows CI. No release before simulated fault tests and controlled hardware validation.

## Configure inside N.I.N.A.

Open **Options > Plugins > Shutdown Button** (the N.I.N.A. plugin options page).
Choose which devices N.I.N.A. disconnects. This is not the same as cutting
power to physical outlets.

Under **Physical power outlets**, add one entry per power controller output:
give it a name, toggle Enabled, enter the HTTP URL, method (POST, PUT or PATCH),
optional Authorization header and JSON body. The list executes in order. Select
an outlet and click Remove to remove it. Click **Save shutdown configuration**.
Changes only apply to later shutdown runs, not an in-flight operation.

For safety, **Dry run** is checked and **Enable physical REST power-off** is
unchecked on first installation. To enable real switching both settings must be
explicitly changed and saved. The saved file is local:
`%LOCALAPPDATA%\\NINA\\ShutdownButton\\settings.json`. Its optional Authorization
headers are stored in plain text: use a dedicated low-privilege token, localhost
or a protected private network, and restrict file access. Do not expose the
controller over the internet.

The **Imaging > Shutdown Telescope** dockable deliberately contains only the
button, confirmation and status; equipment and REST settings live in Options.
The workflow always stops the sequencer, stops guiding and parks the mount before
the optional disconnections/power commands.

## CI builds / installation

Every change is built and tested by GitHub Actions on Windows. After a
successful run, download the **nina-shutdown-button-windows** artifact, extract
`NINA.Plugin.ShutdownButton.dll`, and install it using N.I.N.A.'s manual
plugin installation workflow for the supported 3.2 plugin directory. Restart
N.I.N.A., open Plugins > Shutdown Button, then enable the dockable in Imaging.

**Experimental build:** an automated build and tests do not certify safe use on
a telescope. First validate all steps with equipment simulators and a mock
power endpoint. There is no weather/roof interlock, and this is not an
emergency motor stop.

## GET power-controller calls

REST outlet actions support GET, POST, PUT and PATCH. For a GET action, put the controller command and parameters in the URL (including the query string) and **leave JSON Body empty**. GET can change device state on certain controllers even though HTTP normally defines GET as read-only; use it only for a trusted local device endpoint and do not expose it to caches, crawlers or public networks. GET with a non-empty body is rejected before any power command is sent.
