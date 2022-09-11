using UnityEngine;

namespace Phezu.StealthSystem.Internal
{
    public interface ISpotter
    {
        /// <summary>
        /// This method is called every stealth tick.
        /// </summary>
        public void OnStealthTick(float deltaTime);

        /// <summary>
        /// This method is called when a spy comes in range to be seen.
        /// </summary>
        public void OnSpyVisible(Transform spy);

        /// <summary>
        /// This method is called when a spy goes too far away to be seen.
        /// </summary>
        public void OnSpyInVisible(Transform spy);

        public void OnSpyAudible(Transform spy);

        public void OnSpyInAudible(Transform spy);

        /// <summary>
        /// This is called when the visibility rating of a spy who is in visibility range increases.
        /// </summary>
        /// <param name="spy">The spy in range.</param>
        /// <param name="visibilityRating">His new rating.</param>
        public void OnVisibilityChange(Transform spy, float visibilityRating);

        /// <summary>
        /// This is called when the audibility rating of a spy who you can hear increases.
        /// </summary>
        /// <param name="spy">The spy you are hearing.</param>
        /// <param name="audibilityRating">His new rating.</param>
        public void OnAudibilityChange(Transform spy, float audibilityRating);
    }
}