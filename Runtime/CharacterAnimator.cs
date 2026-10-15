using UnityEngine;

namespace ITKamianets.Engine.Character
{
    /// <summary>
    /// Feeds a <see cref="CharacterMotor"/>'s movement to an Animator on the character's model, as
    /// standard parameters any controller can use:
    /// <c>Forward</c> and <c>Right</c> (m/s relative to where the character faces), <c>Speed</c>
    /// (m/s), <c>Grounded</c>, <c>Crouching</c> and <c>VerticalSpeed</c> (m/s, up is positive).
    /// Parameters the controller does not have are skipped.
    ///
    /// Reads the motor's state, not its events, so characters shown by the network animate too.
    /// The model's clips should not move the character themselves (no root motion): the motor does.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public class CharacterAnimator : MonoBehaviour
    {
        public static readonly int Forward = Animator.StringToHash("Forward");
        public static readonly int Right = Animator.StringToHash("Right");
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int Grounded = Animator.StringToHash("Grounded");
        public static readonly int Crouching = Animator.StringToHash("Crouching");
        public static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");

        [Tooltip("Empty: the first Animator on this object or its children.")]
        [SerializeField] private Animator animator;

        [Tooltip("Seconds to ease speed values, so animations blend instead of snapping.")]
        [SerializeField] private float damping = 0.1f;

        private CharacterMotor _motor;
        private bool _hasForward, _hasRight, _hasSpeed, _hasGrounded, _hasCrouching, _hasVertical;

        public Animator Animator => animator;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                // The motor moves the character; clips must not.
                animator.applyRootMotion = false;
                foreach (var parameter in animator.parameters)
                {
                    _hasForward |= parameter.nameHash == Forward;
                    _hasRight |= parameter.nameHash == Right;
                    _hasSpeed |= parameter.nameHash == Speed;
                    _hasGrounded |= parameter.nameHash == Grounded;
                    _hasCrouching |= parameter.nameHash == Crouching;
                    _hasVertical |= parameter.nameHash == VerticalSpeed;
                }
            }
        }

        private void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled)
            {
                return;
            }

            var velocity = _motor.Velocity;
            var local = Quaternion.Euler(0f, -_motor.Yaw, 0f) * new Vector3(velocity.x, 0f, velocity.z);
            var dt = Time.deltaTime;

            if (_hasForward) animator.SetFloat(Forward, local.z, damping, dt);
            if (_hasRight) animator.SetFloat(Right, local.x, damping, dt);
            if (_hasSpeed) animator.SetFloat(Speed, local.magnitude, damping, dt);
            if (_hasGrounded) animator.SetBool(Grounded, _motor.IsGrounded);
            if (_hasCrouching) animator.SetBool(Crouching, _motor.IsCrouching);
            if (_hasVertical) animator.SetFloat(VerticalSpeed, velocity.y);
        }
    }
}
