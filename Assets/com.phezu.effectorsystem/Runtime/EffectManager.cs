using System.Collections.Generic;
using UnityEngine;
using Phezu.Util;

namespace Phezu.EffectorSystem
{
    [AddComponentMenu("Phezu/Effector System/Effect Manager")]
    public class EffectManager : Singleton<EffectManager>
    {
        [SerializeField] private List<string> allEffects;

        public IReadOnlyCollection<string> AllEffects => allEffects.AsReadOnly();

        private readonly Dictionary<Collider, IEffectable> mSubscribers = new();

        public void Register(Collider collider, IEffectable effectable)
        {
            if (!mSubscribers.ContainsKey(collider))
                mSubscribers.Add(collider, effectable);
        }

        public void UnRegister(Collider collider)
        {
            if (mSubscribers.ContainsKey(collider))
                mSubscribers.Remove(collider);
        }

        public int GetEffectID(string effectName)
        {
            for (int i = 0; i < allEffects.Count; i++)
            {
                if (allEffects[i] == effectName)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Sets effectable to the IEffectable component associated with the collider.
        /// </summary>
        /// <param name="collider">The collider to check</param>
        /// <param name="effectable">This is set to the IEffectable component of the collider</param>
        /// <returns>Returns true if collider is registered</returns>
        public bool GetEffectable(Collider collider, out IEffectable effectable)
        {
            mSubscribers.TryGetValue(collider, out effectable);
            return effectable != null;
        }
    }
}