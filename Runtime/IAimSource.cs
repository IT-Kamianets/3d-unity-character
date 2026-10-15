using UnityEngine;

namespace ITKamianets.Engine.Character
{
    /// <summary>
    /// Where a character aims, for gameplay that shoots, casts or points (a spell, a tool). Put a
    /// component implementing it next to the character: a VR hand, a turret's barrel. Gameplay
    /// asks the enabled one and falls back to its own default (e.g. the screen centre) when there
    /// is none or it returns false.
    /// </summary>
    public interface IAimSource
    {
        /// <summary>The aim ray in world space. False when it can't aim now (e.g. the hand isn't tracked).</summary>
        bool TryGetAimRay(out Ray ray);
    }
}
