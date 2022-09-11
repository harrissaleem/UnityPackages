﻿using UnityEngine;
using Phezu.StealthSystem.Internal;

namespace Phezu.StealthSystem
{
    public class StealthProp : MonoBehaviour, IStealthProp
    {
        [SerializeField] private float visibilityMultiplier;

        public float VisibilityMultiplier => visibilityMultiplier;
    }
}