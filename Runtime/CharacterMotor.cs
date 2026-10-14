using System;
using UnityEngine;

namespace ITKamianets.Engine.Character
{
    /// <summary>
    /// Moves a character: walking, sprinting, jumping, crouching, slopes and steps. Shared by
    /// every constructor; it has no camera and reads no input. Give it a
    /// <see cref="CharacterCommand"/> each frame with <see cref="SetCommand"/>.
    ///
    /// The transform's origin is at the feet. By default the motor steps itself in Update.
    /// Networked games turn <see cref="SimulateInUpdate"/> off and call <see cref="Simulate"/>
    /// themselves with a fixed tick, so the server and client prediction run the same steps.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class CharacterMotor : MonoBehaviour
    {
        // Small extra reach below the capsule when looking for ground, so walking down
        // slopes and small drops doesn't count as leaving the ground.
        private const float GroundProbeDistance = 0.1f;

        // Downward speed kept while grounded so the character hugs slopes and steps down.
        private const float GroundStickSpeed = 2f;

        // Timers in CharacterState are capped: anything older than this means "long ago".
        private const float MaxTimer = 1000f;

        [SerializeField] private CharacterSettings settings = new CharacterSettings();

        [Tooltip("Step in Update with the last command. Turn off to call Simulate yourself (networking, fixed ticks).")]
        [SerializeField] private bool simulateInUpdate = true;

        public event Action Jumped;

        /// <summary>Raised on touching the ground after being in the air, with the downward speed at impact.</summary>
        public event Action<float> Landed;

        public event Action LeftGround;
        public event Action<bool> CrouchChanged;

        public CharacterSettings Settings => settings;

        public bool SimulateInUpdate
        {
            get => simulateInUpdate;
            set => simulateInUpdate = value;
        }

        /// <summary>Velocity after the last step, in world space.</summary>
        public Vector3 Velocity { get; private set; }

        public bool IsGrounded { get; private set; }

        /// <summary>Standing on ground steeper than <see cref="CharacterSettings.SlopeLimit"/>, sliding down.</summary>
        public bool IsOnSteepSlope { get; private set; }

        public bool IsCrouching { get; private set; }

        public bool IsSprinting { get; private set; }

        /// <summary>Normal of the ground below, or up when there is none.</summary>
        public Vector3 GroundNormal { get; private set; } = Vector3.up;

        /// <summary>The collider below, e.g. to pick a footstep sound by surface. Null in the air.</summary>
        public Collider GroundCollider { get; private set; }

        public float Yaw { get; private set; }

        /// <summary>Eye height above the feet, eased between standing and crouching.</summary>
        public float EyeHeight { get; private set; }

        /// <summary>Where a first-person camera sits, in world space.</summary>
        public Vector3 EyePosition => transform.position + Vector3.up * EyeHeight;

        private CharacterController _controller;
        private CharacterCommand _command;
        private float _verticalSpeed;
        private float _time;
        private float _lastGroundedTime = float.NegativeInfinity;
        private float _jumpRequestedTime = float.NegativeInfinity;
        private readonly Collider[] _overlaps = new Collider[8];

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            Yaw = transform.eulerAngles.y;
            EyeHeight = settings.StandingEyeHeight;
            ApplyBody(settings.StandingHeight);
        }

        private void OnValidate()
        {
            if (_controller == null)
            {
                _controller = GetComponent<CharacterController>();
            }

            if (_controller != null && !Application.isPlaying)
            {
                ApplyBody(settings.StandingHeight);
            }
        }

        private void Update()
        {
            if (!simulateInUpdate)
            {
                return;
            }

            Simulate(_command, Time.deltaTime);

            // Jump is a press, not a hold: it stays buffered inside the motor, not in the command.
            _command.Jump = false;
        }

        /// <summary>The command used by the next Update step. Ignored when stepping manually.</summary>
        public void SetCommand(in CharacterCommand command)
        {
            var jump = _command.Jump || command.Jump;
            _command = command;
            _command.Jump = jump;
        }

        /// <summary>
        /// Returns the command set since the last call and clears its jump press. For stepping
        /// manually (e.g. the network's fixed tick): a jump pressed between two steps is kept until
        /// the next one takes it.
        /// </summary>
        public CharacterCommand ConsumeCommand()
        {
            var command = _command;
            _command.Jump = false;
            return command;
        }

        /// <summary>Advances the character by one step.</summary>
        public void Simulate(in CharacterCommand command, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            _time += deltaTime;

            Yaw = command.Yaw;
            transform.rotation = Quaternion.Euler(0f, Yaw, 0f);

            UpdateGround();
            UpdateCrouch(command.Crouch);

            var horizontal = MoveHorizontally(command, deltaTime);
            MoveVertically(command, deltaTime);

            var displacement = horizontal * deltaTime;
            if (IsGrounded && _verticalSpeed <= 0f)
            {
                // Follow the ground's tilt so walking downhill doesn't bounce.
                displacement = Vector3.ProjectOnPlane(displacement, GroundNormal);
            }

            if (IsOnSteepSlope && _verticalSpeed <= 0f)
            {
                displacement += Vector3.ProjectOnPlane(Vector3.down, GroundNormal).normalized * (settings.SlideSpeed * deltaTime);
            }

            displacement += Vector3.up * (_verticalSpeed * deltaTime);

            var before = transform.position;
            var flags = _controller.Move(displacement);

            if ((flags & CollisionFlags.Above) != 0 && _verticalSpeed > 0f)
            {
                _verticalSpeed = 0f;
            }

            // Real movement, not the wanted one: speed is lost against walls instead of building up.
            var moved = (transform.position - before) / deltaTime;
            Velocity = new Vector3(moved.x, _verticalSpeed, moved.z);

            var wasGrounded = IsGrounded;
            var impactSpeed = -_verticalSpeed;
            UpdateGround();

            if (!wasGrounded && IsGrounded)
            {
                _verticalSpeed = -GroundStickSpeed;
                Landed?.Invoke(Mathf.Max(0f, impactSpeed));
            }
            else if (wasGrounded && !IsGrounded)
            {
                LeftGround?.Invoke();
            }

            var targetEye = IsCrouching ? settings.CrouchingEyeHeight : settings.StandingEyeHeight;
            EyeHeight = Mathf.MoveTowards(EyeHeight, targetEye, settings.EyeHeightSpeed * deltaTime);
        }

        /// <summary>Moves the character instantly, e.g. to a spawn point. Clears its speed.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _controller.enabled = true;

            Yaw = yaw;
            Velocity = Vector3.zero;
            _verticalSpeed = 0f;
            _jumpRequestedTime = float.NegativeInfinity;
            UpdateGround();
        }

        public CharacterState GetState()
        {
            return new CharacterState
            {
                Position = transform.position,
                Yaw = Yaw,
                Velocity = Velocity,
                IsGrounded = IsGrounded,
                IsCrouching = IsCrouching,
                SinceGrounded = Mathf.Min(_time - _lastGroundedTime, MaxTimer),
                SinceJumpRequested = Mathf.Min(_time - _jumpRequestedTime, MaxTimer),
            };
        }

        /// <summary>Puts the character back into a saved state, without raising events.</summary>
        public void SetState(in CharacterState state)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(state.Position, Quaternion.Euler(0f, state.Yaw, 0f));
            _controller.enabled = true;

            Yaw = state.Yaw;
            Velocity = state.Velocity;
            _verticalSpeed = state.Velocity.y;
            IsCrouching = state.IsCrouching;
            ApplyBody(IsCrouching ? settings.CrouchingHeight : settings.StandingHeight);
            UpdateGround();
            IsGrounded = state.IsGrounded;
            _lastGroundedTime = _time - state.SinceGrounded;
            _jumpRequestedTime = _time - state.SinceJumpRequested;
        }

        private Vector3 MoveHorizontally(in CharacterCommand command, float deltaTime)
        {
            var wish = new Vector3(command.Move.x, 0f, command.Move.z);
            if (wish.sqrMagnitude > 1f)
            {
                wish.Normalize();
            }

            IsSprinting = command.Sprint && !IsCrouching && wish.sqrMagnitude > 0.01f;
            var speed = IsCrouching ? settings.CrouchSpeed : IsSprinting ? settings.SprintSpeed : settings.WalkSpeed;
            var target = wish * speed;

            float rate;
            if (!IsGrounded)
            {
                rate = settings.AirAcceleration;
            }
            else
            {
                rate = wish.sqrMagnitude > 0.0001f ? settings.Acceleration : settings.Deceleration;
            }

            var current = new Vector3(Velocity.x, 0f, Velocity.z);
            return Vector3.MoveTowards(current, target, rate * deltaTime);
        }

        private void MoveVertically(in CharacterCommand command, float deltaTime)
        {
            if (command.Jump)
            {
                _jumpRequestedTime = _time;
            }

            var jumpWanted = _time - _jumpRequestedTime <= settings.JumpBuffer;
            var canJump = _time - _lastGroundedTime <= settings.CoyoteTime && !IsCrouching;

            if (jumpWanted && canJump)
            {
                _verticalSpeed = Mathf.Sqrt(2f * settings.Gravity * settings.JumpHeight);
                _jumpRequestedTime = float.NegativeInfinity;
                _lastGroundedTime = float.NegativeInfinity;
                IsGrounded = false;
                Jumped?.Invoke();
                return;
            }

            if (IsGrounded && _verticalSpeed <= 0f)
            {
                _verticalSpeed = -GroundStickSpeed;
                return;
            }

            _verticalSpeed = Mathf.Max(_verticalSpeed - settings.Gravity * deltaTime, -settings.MaxFallSpeed);
        }

        private void UpdateGround()
        {
            IsGrounded = false;
            IsOnSteepSlope = false;
            GroundNormal = Vector3.up;
            GroundCollider = null;

            // Rising from a jump: ignore the ground just left.
            if (_verticalSpeed > 0f)
            {
                return;
            }

            var radius = _controller.radius * 0.95f;
            var origin = transform.position + Vector3.up * (_controller.radius + _controller.skinWidth);
            var distance = _controller.skinWidth * 2f + GroundProbeDistance;

            // The cast starts inside the character's own capsule, which casts ignore.
            if (!Physics.SphereCast(origin, radius, Vector3.down, out var hit, distance, settings.GroundLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            var normal = hit.normal;
            if (Vector3.Angle(normal, Vector3.up) > settings.SlopeLimit)
            {
                normal = SurfaceNormalAt(hit, origin);
            }

            GroundNormal = normal;
            GroundCollider = hit.collider;

            if (Vector3.Angle(normal, Vector3.up) <= settings.SlopeLimit)
            {
                IsGrounded = true;
                _lastGroundedTime = _time;
            }
            else
            {
                IsOnSteepSlope = true;
            }
        }

        // A sphere touching an edge (a step, a ledge) reports the edge's rounded normal, which
        // looks steep. Look straight down just inside the contact point for the real surface.
        private Vector3 SurfaceNormalAt(RaycastHit hit, Vector3 castOrigin)
        {
            var inward = hit.point - castOrigin;
            inward.y = 0f;
            var rayOrigin = hit.point + inward.normalized * 0.01f + Vector3.up * 0.1f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out var surface, 0.2f, settings.GroundLayers, QueryTriggerInteraction.Ignore))
            {
                return surface.normal;
            }

            return hit.normal;
        }

        private void UpdateCrouch(bool wantsCrouch)
        {
            if (wantsCrouch == IsCrouching)
            {
                return;
            }

            if (!wantsCrouch && !HasRoomToStand())
            {
                return;
            }

            IsCrouching = wantsCrouch;
            ApplyBody(IsCrouching ? settings.CrouchingHeight : settings.StandingHeight);
            CrouchChanged?.Invoke(IsCrouching);
        }

        private bool HasRoomToStand()
        {
            var radius = _controller.radius * 0.95f;
            var bottom = transform.position + Vector3.up * (_controller.radius + _controller.skinWidth);
            var top = transform.position + Vector3.up * (settings.StandingHeight - _controller.radius);
            var count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _overlaps, settings.GroundLayers, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                if (_overlaps[i] != _controller)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyBody(float height)
        {
            _controller.radius = settings.Radius;
            _controller.height = height;
            _controller.center = Vector3.up * (height * 0.5f);
            _controller.slopeLimit = settings.SlopeLimit;
            _controller.stepOffset = Mathf.Min(settings.StepHeight, height - settings.Radius * 2f);
        }
    }
}
