# UltraCraft first play test

The prototype is running in the ULTRAKILL window. Bring that window to the front yourself. It should show a Minecraft Java landscape, with a build-mode message at the top left. The separate Java companion stays hidden.

This is an early integration test. Mining, crafting, movement feel, the gun terminal's controls, and arena combat still need hands-on verification. Nothing is published.

For the latest corrective build, begin with three short checks: walk using your existing ULTRAKILL bindings, look around at nearby grass, and inspect the shaded ground underneath a tree. Then open **E**, close it, and confirm you can still walk. The engine checks verified active movement, screen-state restoration, stable camera projection, Java lighting and visible cast shadows without injecting input.

1. **Move:** use your normal ULTRAKILL controls. Walk, jump, dash, slide, and try a wall jump against blocks. Check that you stand on the ground and cannot pass through solid blocks.
   In this revision, solid blocks and mob bodies should be visible, and mouse look should work immediately when the game gains focus.
2. **Mine:** begin in build mode. Look at a tree and hold the left mouse button until a log breaks. Walk over the drop. If the screen says combat mode, press **V** to return to build mode.
3. **Craft:** press **E**. Turn the log into planks, then make a crafting table. Close the inventory with **E**. Select the table in the hotbar and use right mouse to place it, then right mouse to open it. Try making a wooden pickaxe.
4. **Build:** select a block using the number keys. Place it with right mouse, walk against it, and break it again. The picture and collision should both update.
5. **Choose a gun:** the native gun terminal is a few blocks from spawn. Approach it and press **G**, or use its usual ULTRAKILL interaction. Try changing a gun or variant. Press **V** to enter gun mode.
6. **Combat (night waves):** wait for night or use `/time set 13000`; waves appear around you (wave 1 is four Filth; later waves add Strays, Schisms, Soldiers, Drones and bigger enemies). They should stand and walk on Minecraft ground, not fall through. Check that your guns hurt them and their attacks hurt you. The top-left label shows the night, wave and enemies left; when a wave is cleared the next starts after a few seconds, until dawn. Press **H** to return to camp.
7. **Cross-game damage:** shoot a cow or pig with the revolver (it should take damage and get knocked back). Switch to build mode (**V**) and hit a Filth with a Minecraft sword (it should take damage).
8. **Night enemies:** wait for night (or use `/time set night`) or go into a dark cave: Filth, Strays and others should appear instead of zombies and skeletons; animals still spawn.
7. **Save:** place a distinctive block and keep an item in your inventory. Quit ULTRAKILL normally, then wait for the Java companion to finish saving. A subsequent launch should preserve the edits and inventory. Let Codex know before the restart so it can prepare both prototype components.

If something fails, report the step and what you saw. If comfortable, attach a screenshot yourself; Codex will not use computer control.

Two extra checks for this revision:
- **Commands:** press **/** and enter `/time query daytime`. Press Enter for the result. **T** opens chat. Escape should close Java screens without opening ULTRAKILL's pause menu.
- **Water:** enter a pool, try jumping/swimming up, moving sideways, dashing, and leaving the water. Real native water volumes now follow Java's fluid blocks.

Compare mining while standing still on dry ground with mining in the air. The first should use normal Java speed for the selected tool. Minecraft now receives the native grounded state instead of always treating the player as airborne.

The background checks already confirmed a live connection, fresh Java world creation and reopening, streamed terrain and collision sections, successful native-player placement, a native terminal near spawn, and native weapon objects. These checks do not prove the hands-on steps above.

This lab uses its own UltraCraft world and leaves the Minecraft Forge game that was already running alone. Full Minecraft feature parity and automatic Melty installation remain unverified.
