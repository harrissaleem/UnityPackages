using UnityEngine;
using Phezu.StealthSystem.Internal;

namespace Phezu.StealthSystem
{
    [AddComponentMenu("Phezu/Stealth System/Stealth Prop")]
    public class StealthProp : MonoBehaviour, IStealthProp
    {
        [SerializeField] private float visibilityMultiplier;

        public float VisibilityMultiplier => visibilityMultiplier;
    }
}