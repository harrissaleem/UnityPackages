using System.Collections.Generic;
using UnityEngine;
using Phezu.Util;

namespace Phezu.WeaponSystem
{
    [AddComponentMenu("Phezu/Weapon System/Ballista")]
    public class Ballista : MonoBehaviour
    {
        [SerializeField] private ShooterData shooterData;
        [SerializeField] private Transform shooterBarrel;

        private TimedSpawner[] mAmmoPoolers;
        private Dictionary<GameObject, int> mAmmoToIndexMap;
        private Dictionary<GameObject, BallistaAmmo> mObjToAmmoMap;
        private TimedSpawner mParticlesSpawner;
        private float coolDown;
        private int mSelectedAmmo;

        private GameObject mAmmoShot;

        private void Start()
        {
            mAmmoShot = null;
            coolDown = mSelectedAmmo = 0;

            mAmmoPoolers = new TimedSpawner[shooterData.ammoData.Length];
            mAmmoToIndexMap = new Dictionary<GameObject, int>();
            mObjToAmmoMap = new Dictionary<GameObject, BallistaAmmo>();

            for (int i = 0; i < mAmmoPoolers.Length; i++)
            {
                int currentIndex = i;
                mAmmoPoolers[i] = new TimedSpawner(
                    shooterData.ammoData[i].prefab,
                    (x) => { OnAmmoPrefabCreate(x, currentIndex); },
                    OnAmmoGet,
                    null,
                    shooterData.ammoData[i].lifeTimeInSeconds
                    );
            }

            if (shooterData.fireParticles != null)
            {
                mParticlesSpawner = new TimedSpawner(
                    shooterData.fireParticles,
                    null,
                    OnParticlesGet,
                    null,
                    shooterData.fireParticlesDuration
                    );
            }
        }

        private void OnAmmoPrefabCreate(GameObject projectileObj, int i)
        {
            mAmmoToIndexMap.Add(projectileObj, i);

            BallistaAmmo ballistaAmmo = projectileObj.GetComponent<BallistaAmmo>();

            mObjToAmmoMap.Add(projectileObj, ballistaAmmo);

            if (ballistaAmmo == null)
            {
                Debug.LogError("Projectile prefab does not have a Projectile script attached");
                return;
            }
            ballistaAmmo.Initialize(shooterData.ammoData[i], ReturnAmmo, shooterData.shooterSpeedMultiplier);
        }
        private void OnAmmoGet(GameObject projectileObj)
        {
            projectileObj.transform.SetPositionAndRotation(shooterBarrel.position, shooterBarrel.rotation);
            mObjToAmmoMap[projectileObj].OnTriggerCompress();
            mAmmoShot = projectileObj;
        }
        private void OnParticlesGet(GameObject particlesObj)
        {
            particlesObj.transform.SetPositionAndRotation(shooterBarrel.position, shooterBarrel.rotation);
        }


        private void Update()
        {
            foreach (var spawner in mAmmoPoolers)
                spawner.SpawnerTick(Time.deltaTime);
            mParticlesSpawner?.SpawnerTick(Time.deltaTime);
            coolDown -= Time.deltaTime;
        }

        public void CompressTrigger()
        {
            if (coolDown > 0f)
                return;
            mAmmoPoolers[mSelectedAmmo].SpawnObject();
            mParticlesSpawner?.SpawnObject();
            coolDown = 1f / shooterData.ammoData[mSelectedAmmo].fireRate;
        }

        public void DecompressTrigger()
        {
            if (mAmmoShot == null)
                return;
            mObjToAmmoMap[mAmmoShot].OnTriggerDecompress();
        }

        private void ReturnAmmo(GameObject ammoObj)
        {
            int ammoIndex = mAmmoToIndexMap[ammoObj];
            mAmmoPoolers[ammoIndex].ReturnObject(ammoObj);
        }

        public void NextAmmo()
        {
            mSelectedAmmo++;
        }
        public void PrevAmmo()
        {
            mSelectedAmmo--;
        }
    }
}