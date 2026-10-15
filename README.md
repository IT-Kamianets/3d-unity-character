# IT-Kamianets 3D Engine: Unity Character

`3d-unity-character` provides a reusable player character for the **IT-Kamianets 3D Engine** in Unity.

It covers walking, running, jumping, crouching, and ground handling, and works under any camera style.

### Logic

The character does not read input and does not own a camera. Each step it is given a **movement command**: a world-space move direction, a facing (yaw), and sprint, jump and crouch flags. Whatever controls it builds that command:

* a constructor's controls package from player input (`3d-constructor-character-view-controls` decides that forward is where the camera looks),
* an AI,
* the network replaying another player's input.

That keeps one character implementation shared by every constructor (first/third person, top-down, isometric, side view, ...), and makes it network-ready: the server and client prediction run the same `Simulate(command, deltaTime)` steps, and `GetState()`/`SetState()` let the network correct a predicted character and replay newer commands.

It exposes what other packages need from a character without knowing them: `EyePosition` for a first-person camera, `Jumped`/`Landed`/`LeftGround`/`CrouchChanged` events and `GroundCollider` for footstep and landing sounds, `Velocity` and `IsSprinting` for animation.

### Status

Version 0.1: `CharacterMotor` on Unity's `CharacterController`.

* Walk, sprint, crouch speeds with ground acceleration/deceleration and limited air control
* Jump to a set height, with coyote time and jump buffering
* Ground detection that treats step and ledge edges as walkable
* Slopes up to a limit; steeper ground slides the character down
* Steps up to a set height
* Crouching that stays crouched until there is room to stand; eye height eases between the two
* `Teleport` for spawns; `GetState`/`SetState` (including jump timers) for network correction; `ConsumeCommand` for fixed-tick stepping
* `CharacterAnimator`: feeds the movement to the model's Animator as `Forward`, `Right`, `Speed`, `Grounded`, `Crouching`, `VerticalSpeed` (skips parameters the controller lacks); reads the motor's state, so network copies animate too; no root motion
* Steps itself in Update by default; turn `SimulateInUpdate` off to drive it with a fixed tick
* `IAimSource`: where a character aims (a VR hand, a turret barrel); gameplay asks the enabled one and falls back to its own default (e.g. the screen centre)

Checked in Play mode on test geometry: 1.26 m jump for a 1.2 m setting, walks onto a 0.3 m step at full speed, stays crouched under a 1.4 m ceiling and stands after leaving it, slides off a 60° ramp.

```csharp
var motor = GetComponent<CharacterMotor>();   // origin at the feet
motor.SetCommand(new CharacterCommand
{
    Move = cameraForwardFlat * input.y + cameraRightFlat * input.x,
    Yaw = cameraYaw,
    Sprint = sprintHeld,
    Jump = jumpPressedThisFrame,
    Crouch = crouchHeld,
});
motor.Landed += impactSpeed => { /* play landing sound */ };
```

### Roadmap

* Walking
* Running
* Jumping
* Ground detection
* Slopes and steps
* Crouching
* Movement settings
* Movement events
* Moving platforms
* Pushing physics objects
* Animation parameters
* Stable v1 character API
