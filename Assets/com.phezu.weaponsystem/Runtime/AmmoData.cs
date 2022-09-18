using UnityEngine;

namespace Phezu.WeaponSystem
{
    [CreateAssetMenu(fileName = "New Projectile", menuName = "Phezu/WeaponSystem/Projectile")]
    public class AmmoData : ScriptableObject
    {
        [Tooltip("Name of the projectile")]
        public string projectileName;

        [Tooltip("This is the layer of objects the projectile can damage")]
        public LayerMask targetLayer;

        [Tooltip("Prefab of the projectile")]
        public GameObject prefab;

        [Tooltip("This prefab will be spawned for its duration upon collision")]
        public GameObject hitParticles;

        [Tooltip("Duration in seconds the hitParticles prefab will be spawned for")]
        public float hitParticlesDuration = 1f;

        [Tooltip("Speed of the projectile. This gets affected by the shooter's speed multiplier")]
        public float speed = 5f;

        [Tooltip("Amount of projectiles you can fire per second")]
        public float fireRate = 1f;

        [Tooltip("Projectile will turn towards its target this many degrees per second if it has one")]
        public float targetFollowingStrength;

        [Tooltip("Leniency in finding target. This represents the radius of the sphere cast")]
        public float targetSeekingLeniency;

        [Tooltip("If the target is at a turn of larger than this many degrees projectile will ignore it")]
        [Range(0f, 180f)] public float targetLosingThreshold;

        [Tooltip("After the projectile is shot it will be despawned when this much time passes")]
        public float lifeTimeInSeconds = 20f;

        [Tooltip("Strength of random noise in the projectile's movement")]
        public float movementNoiseStrength;

        [Tooltip("Scale of random noise in the projectile's movement")]
        public float movementNoiseScale;
    }
}