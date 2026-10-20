# Hook Climber - fully procedural vertical climber (Unity 6)

No text, no UI, no hearts, no hints. Everything (art, audio, level) is generated at runtime.

## Setup (about 2 minutes)
1. New project from the Universal 2D (or Built-in 2D) template, Unity 6. Needs the Input System package (ships with the template).
2. Project Settings > Player > Active Input Handling = Input System Package (New) (or Both).
3. Drag this whole folder into Assets/ (the Editor/ subfolder must stay named Editor).
4. Open ANY scene (an empty one is fine) and press Play. GameManager starts itself.

## Controls
A/D move | Space/W jump (hold = higher, also higher stomp bounce) | Left click: grapple toward the mouse.
The OS mouse cursor is your aim; the game draws no UI of its own.

## Rules
- Lava rises on its own schedule (independent of the camera), faster the higher you climb, so you can climb ahead of it and build a lead.
  If your lead gets very large the lava speeds up to catch you. Touching it, or falling off the bottom of the screen, ends the run;
  3 seconds later the run restarts on its own.
- Platforms are one-way: jump up through them.
- Land on a Crawler or Floater head to kill it and bounce (chains are higher if you hold Space). Any other contact hurts.
  Health is shown only by flicker and a red tint on the character (3 hits).
- Grapple: click toward a platform (pulled on top of it) or an enemy (pulled to its head = stomp). Click again or Space to let go.
  Walls block the hook.
- Higher up you get enemy-only stretches: several rows of floaters and no platforms. Bounce or hook your way across.

## Customising
Press Play once; an asset appears at Assets/Resources/GameConfig.asset (or Tools > Hook Climber > Select Config).
Every gameplay number is in it, grouped: Player, Hook, Enemies, World, Cam, Lava, Flow, Audio, Visuals.
Most values apply live while the game runs. Exceptions (apply on next run/start): player max health + collider size,
tower width, music tempo. Delete the asset to restore defaults.
Useful ones: World.platformWidthStart/End/minPlatformWidth (platform size), World.stretch* (enemy-only stretches),
Flow.restartDelay, Lava.speedStart/speedEnd/leadSoftCap, Player.jumpVelocity / stompBounce, Hook.range / pullSpeed.
