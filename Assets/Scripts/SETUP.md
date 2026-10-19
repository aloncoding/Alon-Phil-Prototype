# Setup (Unity 6, URP 2D Renderer)

Packages: Input System, Cinemachine 3.x, 2D Animation/Sprite, Universal RP.
Set Project Settings > Player > Active Input Handling = "Input System Package (New)".

Layers: Player, Enemy, Ground. Collision matrix: Player x Enemy OFF (so pulled enemies don't shove the player).
Enemy projectiles use a trigger collider and hit Player + Ground masks.

Scene managers (one empty each): PlayerInputReader, AudioController, VFXController, CameraController, LevelSequenceController, ReleaseBuildCleanup.

Player: Rigidbody2D + CapsuleCollider2D + PlayerController + PlayerHealth + PlayerAnimationController + GrapplingHookController.
  Children: Visual (sprites + Animator), HookOrigin (empty at hand), TetherRenderer (LineRenderer object).
  Hook head: separate object with sprite + TrailRenderer (assign to hookHead; it is unparented at runtime).
  Hook masks: targetMask = Enemy, blockMask = Ground.

Enemies: Rigidbody2D + Collider2D (Enemy layer) + GroundedMeleeEnemy or FlyingRangedEnemy + GrappleTarget. Pivot at feet for grounded.
Flying enemy: assign projectile prefab (Rigidbody2D + trigger collider + ProjectileController).

Camera: CinemachineCamera (Follow = empty "CameraTarget"), add CinemachineImpulseListener, add CinemachineImpulseSource to CameraController object,
  optional CinemachineConfiner2D. Fade: black Sprite as child of camera, assign to LevelSequenceController.fadeRenderer.

Level: HookRelic (trigger), CameraFocusZone, AudioZone, CheckpointTrigger, KillZone, EndingTrigger, SurfaceMarker on ground, ParallaxLayer on backgrounds.
Optional Shader Graph float "_FlashAmount" on enemy sprites for a hit flash.
