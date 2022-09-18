using System;
using System.Collections.Generic;
using UnityEngine;
using Phezu.EffectorSystem.Internal;
using System.Collections;

namespace Phezu.EffectorSystem
{
    [Serializable]
    public class EffectorData : IEnumerable<Effect>
    {
        [SerializeField] public List<Effect> effects;

        public IEnumerator<Effect> GetEnumerator()
        {
            return effects.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return effects.GetEnumerator();
        }

        public void Initialize()
        {
            for (int i = 0; i < effects.Count; i++)
            {
                Effect effect = effects[i];
                effect.effectID = EffectManager.Instance.GetEffectID(effects[i].name);
                effects[i] = effect;
            }
        }

        public Effect this[int i] 
        {
            get { return effects[i]; }
        }
    }
}