namespace Phezu.EffectorSystem.Internal
{
    [System.Serializable]
    public struct Effect
    {
        public string name;
        public float magnitude;
        /// <summary>
        /// Populate this at the start using DamageManager.GetEffectID()
        /// </summary>
        public int effectID;

        public Effect(string name, float magnitude)
        {
            this.name = name;
            this.magnitude = magnitude;
            effectID = -1;
        }
    }
}