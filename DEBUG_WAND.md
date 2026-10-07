# Debugging wand v0.4.0

Craft Spell Debug Wand with one dirt block at a workbench. Left-click tests the
next spell; right-click opens scan controls. Start auto cycles every 1/2/5/10
seconds; Previous/Next and Test current allow repeated inspection. Choose With
sparks (default), Solo, Always cast or All three. Restart keeps previous results
in the report. Clear shots removes only this wand's most recent test projectiles.

Switching items stops auto. Inventory/chat pauses it. The run ends rather than
wrapping; automatic completion saves after the final observation interval.
Reports contain real root spawn counts and collision/trigger events for demo
projectiles. Unsupported entity trees are skipped atomically, including unknown
trigger payloads. Native API errors are logged without poisoning later cases.

Each case uses a fresh Lua state, unlimited card uses and 10000 test mana.
Only implemented Terraria entity adapters can appear in-game. Ignored Noita
configuration/effect fields are recorded. Rendering correctness is not inferred
from an emitted projectile. Cleanup suppresses payload callbacks, so long-lived
and delayed behavior can be cut short by the test interval. Use longer intervals
or a manual test for those cases.

The last 2048 test results are retained per item; the report records any dropped
older cases. A 422-spell scan has 422 cases, or 1266 in All three mode. Definitions
are loaded from the user's Noita extraction. XML components, original sprites,
terrain/materials and the native engine are not imported by the current demo.

## Version troubleshooting

The earlier downloadable package was independently downloaded and parsed. Its
internal version is 0.3.0 and its SHA256 is
`d30f074a0916308db55ba7a89d4e4f1714b376d3624404cd9c72b567bfae88ab`.
This verifies the saved artifact, not the user's installed file. Seeing 0.2.0
in-game indicates that an older package is loaded; the reason needs the actual
loaded file path to establish.

The new package is 0.4.0, build `spell-debug-wand-1`. Tooltips/editor/HUD expose
the loaded version. The Loaded file button and startup log show its active path.
Use the versioned download ZIP, copy the enclosed `terrarianoita.tmod` to the
folder opened by Mods → Open Mods Folder with tModLoader closed, then restart.
Move older copies of this same mod outside that folder. Update an old source
checkout before rebuilding it.

## Validation

101 standalone checks cover the original script behavior, finite 422/1266-case
scan order, follow-up/always-cast recipes, error recovery and late live-event
serialization. Eight packaged-mod checks cover item persistence, cloning,
chainsaw hitboxes, texture-independent geometry, build stamp consistency,
debug-wand settings, independent scan reports and cleanup payload suppression.
The package builds with tModLoader 2026.08.3.0 without errors or warnings. Live
rendering/UI still require a graphical client playtest.
