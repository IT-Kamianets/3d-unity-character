# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `IAimSource`: an aim ray for gameplay, e.g. a VR hand.

### Added

- `CharacterAnimator`: movement → standard Animator parameters.

### Added

- Initial Unity package structure (`Runtime`, `Editor`, `Tests` assemblies).
- `CharacterMotor`: command-driven movement on `CharacterController` — walk, sprint, crouch, jump (coyote time, jump buffer), slopes, steps, steep-slope sliding.
- `CharacterCommand`, `CharacterSettings`, `CharacterState` (`GetState`/`SetState`), `Teleport`.
- Events `Jumped`, `Landed`, `LeftGround`, `CrouchChanged`; `EyePosition`/`EyeHeight` for cameras.

- `ConsumeCommand()` for stepping on a fixed tick (keeps a jump pressed between steps).
- `CharacterState` carries the coyote-time and jump-buffer timers, so a restored state replays exactly.

### Changed

- No longer depends on `3d-unity-input`: the character is driven by commands, input belongs to the constructor's controls package.
