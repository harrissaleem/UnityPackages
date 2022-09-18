using UnityEngine;

namespace Phezu.WeaponSystem
{
    [CreateAssetMenu(fileName = "New Ballista", menuName = "Phezu/WeaponSystem/Ballista")]
    public class ShooterData : ScriptableObject
    {
        [Tooltip("Name of the shooter")]
        public string shooterName;

        [Tooltip("These particles are spawned at the barrel of the shooter upon firing")]
        public GameObject fireParticles;

        [Tooltip("How long the fire particles last after which they despawn")]
        public float fireParticlesDuration;

        [Tooltip("Collection of projectiles this shooter can shoot")]
        public AmmoData[] ammoData;

        [Tooltip("Speed of all projectiles gets multiplied by this value")]
        public float shooterSpeedMultiplier = 1f;
    }
}