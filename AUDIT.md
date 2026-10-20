# Audit — 2026-10-20

Version 1.0.1 fixes incomplete initialization. Previously, an exception while installing a later Harmony patch could leave earlier patches active. The plugin now validates the exact method signatures before patching and removes only its own patches if initialization fails. The log explicitly says that microphone isolation is not active in this case.

The in-game microphone option, difficulty settings, Windows permissions and other applications are unchanged. On a supported build all four original recording entry points are blocked; the game receives a zero level and silent audio samples.

Validation: production build against the local Unity 2017.3 / BepInEx 5.4.23.5 libraries passed. `tests/run.ps1` passes 22 assertions using test doubles, including a failure at each of six patch steps, foreign-patch preservation and an incompatible-signature failure before patching. The actual Unity process was also exercised separately; these synthetic tests alone do not prove capture isolation inside Unity.

The used game class signatures match the local pre-localization English backup. This is not an independently verified Steam installation. Full minigames, physical hotkeys and an actual Discord stream have not been tested by this audit. Other plugins that start their own recording paths are outside this plugin's scope.

The source remains MIT licensed. No signing certificate is included or required by this BepInEx plugin; an Authenticode signature would establish publisher identity, not game-version compatibility. No third-party or original game assemblies are distributed.

## Combined Unity verification

2026-10-20: 48/48 checks passed in a separate real Unity process with the neighboring plugins installed. The fixture waited for natural additive-world staging and used a redirected test profile. All six real Harmony hooks were present; all four recording entry points were blocked, Mic stayed enabled, levels/samples were zero and the test process was not recording a physical device. This is targeted runtime evidence, not a full gameplay session or a retail Steam compatibility claim.
