using UnityEngine;

namespace ITKamianets.Engine.Character
{
    /// <summary>
    /// What the character should do for one step. Built by whatever controls it: a
    /// constructor's controls package from player input, an AI, or the network replaying
    /// another player's input. The character never reads devices itself.
    /// </summary>
    public struct CharacterCommand
    {
        /// <summary>
        /// Wanted move direction in world space; only x and z are used. Length 1 is full speed,
        /// less is slower (a half-tilted stick). Longer is clamped to 1.
        /// </summary>
        public Vector3 Move;

        /// <summary>Facing around the up axis, in degrees. The character turns to it immediately.</summary>
        public float Yaw;

        public bool Sprint;

        /// <summary>True on the step the jump was pressed. Remembered briefly, so an early press still jumps on landing.</summary>
        public bool Jump;

        /// <summary>Held to crouch. The character stays crouched while there is no room to stand.</summary>
        public bool Crouch;
    }
}
