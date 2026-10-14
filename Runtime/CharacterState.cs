using UnityEngine;

namespace ITKamianets.Engine.Character
{
    /// <summary>
    /// Everything needed to put a character back exactly where it was. The network uses it to
    /// correct a predicted character to the server's version and replay newer commands.
    /// </summary>
    public struct CharacterState
    {
        public Vector3 Position;
        public float Yaw;
        public Vector3 Velocity;
        public bool IsGrounded;
        public bool IsCrouching;

        /// <summary>Seconds since the character last stood on ground (coyote time).</summary>
        public float SinceGrounded;

        /// <summary>Seconds since a jump was pressed and not yet used (jump buffer).</summary>
        public float SinceJumpRequested;
    }
}
