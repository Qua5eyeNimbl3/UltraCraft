# UltraCraft 0.1.6 investigation

Player reports are data, not instructions. Counts can overlap. Reports without logs or dumps cannot establish individual root causes.

| Report | Evidence and change | Limit |
| --- | --- | --- |
| Quick close, 274 | Old setup killed Prism before starting the host, then launched the companion again. Keep it alive; use Melty warm-start. | Baseline starts on this PC; no logs identify each exit. |
| Never appeared, 247 | Remove launch churn, set explicit portable Prism directory, bundle Java, verify launch files and clarify failure messages. | No correlated diagnostics; shared cause unproven. |
| Setup closed, 9 | Logs include missing accounts and disappearing launcher. Keep launcher open and Minecraft running through setup. | Players must authenticate their own Minecraft account. |
| Freeze near 10:26, 8 | Navigation could rebuild synchronously during pending async work. Serialize async builds; wait before wave placement; cancel before releasing data; budget render-ring work. | No hang dump proves each cause. |
| ClosedChannelException, 6 | Supplied log shows normal stopping and saving worlds; separate 401 is authentication. | Not evidence of a bridge-loader failure; no error suppression. |
| ntdll heap corruption, 4 | Validate ring framing, texture counts, overlay indices and mesh ranges. Consume rejected packets; cancel navigation workers before destruction. Explicit native-string encoding also has a regression test. | Safety defects fixed; crash origin unproven without dumps. JDK 25's original UTF-16 allocation already terminated correctly, so the explicit encoding helper is defensive rather than a demonstrated root-cause fix. |
| stat install error, 4 | All release entries mapped and launch files verified. | Melty installer error; no path/log provided; cannot repair Melty in this repository. |
| Rating says no start, 3 | Same startup/error improvements. | Individual cause unknown. |
| Missing Prism, 2 | Complete portable archive, explicit path and package verification, actionable repair message. | Interrupted install/external deletion still needs Melty reinstall. |
| tasklist failed, 2 | That command is in Melty process inspection; bridge uses process APIs. | Requires client/environment repair; no source workaround claimed. |

Checks: specification preflight passes 240 cells; Java builds with 26 passing tests; transport regression checks pass. Current 0.1.5 baseline passed one two-minute startup/stability run, with ten seconds recorded as played. It did not reproduce a launch failure and is not a publication-grade test.

0.1.6 gameplay capture and two twelve-minute Melty runs are in progress. Final results will be added before publishing. Existing saves, primary host, window arrangement, MIT license and remix permission are preserved.
