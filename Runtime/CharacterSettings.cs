using System;
using UnityEngine;

namespace ITKamianets.Engine.Character
{
    /// <summary>How a character moves. Distances in meters, speeds in meters per second.</summary>
    [Serializable]
    public class CharacterSettings
    {
        [Header("Speed")]
        public float WalkSpeed = 4f;
        public float SprintSpeed = 6.5f;
        public float CrouchSpeed = 2f;

        [Tooltip("How fast the character reaches its target speed on the ground.")]
        public float Acceleration = 40f;

        [Tooltip("How fast the character stops on the ground with no move input.")]
        public float Deceleration = 50f;

        [Tooltip("How much the player can steer in the air. Lower is less control.")]
        public float AirAcceleration = 10f;

        [Header("Jump and fall")]
        public float JumpHeight = 1.2f;

        [Tooltip("Stronger than real gravity: games feel floaty at 9.81.")]
        public float Gravity = 20f;

        public float MaxFallSpeed = 50f;

        [Tooltip("A jump still works this long after walking off a ledge.")]
        public float CoyoteTime = 0.12f;

        [Tooltip("A jump pressed this long before landing still happens on landing.")]
        public float JumpBuffer = 0.12f;

        [Header("Ground")]
        [Tooltip("Steepest walkable slope in degrees. Steeper ground slides the character down.")]
        public float SlopeLimit = 45f;

        [Tooltip("Tallest step the character walks up without jumping.")]
        public float StepHeight = 0.35f;

        public float SlideSpeed = 8f;

        public LayerMask GroundLayers = ~0;

        [Header("Body")]
        public float Radius = 0.35f;
        public float StandingHeight = 1.8f;
        public float CrouchingHeight = 1.1f;

        [Tooltip("Camera height when standing, measured from the feet.")]
        public float StandingEyeHeight = 1.65f;

        public float CrouchingEyeHeight = 0.95f;

        [Tooltip("How quickly the eye height follows crouching and standing.")]
        public float EyeHeightSpeed = 10f;
    }
}
