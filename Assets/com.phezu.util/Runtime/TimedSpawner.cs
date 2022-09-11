using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Phezu.Util
{
    public class TimedSpawner
    {
        private ObjectPool<GameObject> mObjectPool;
        private Dictionary<GameObject, float> mTimers;

        public TimedSpawner(GameObject prefab, Action<GameObject> onCreate = null, Action<GameObject> onGet = null, Action<GameObject> onRelease = null, float activeDurationInSecs = 1f, int poolSize = 10)
        {
            //This is to make sure the OnEnable function does not get called on Instantiation
            prefab.SetActive(false);

            mTimers = new Dictionary<GameObject, float>();

            mObjectPool = new ObjectPool<GameObject>(
                () => //OnCreate
                {
                    GameObject particlesObj = GameObject.Instantiate(prefab);
                    onCreate?.Invoke(particlesObj);

                    return particlesObj;
                },
                (x) => //OnGet
                {
                    onGet?.Invoke(x);

                    mTimers.Add(x, activeDurationInSecs);
                    x.SetActive(true);
                },
                (x) => //OnRelease
                {
                    onRelease?.Invoke(x);

                    mTimers.Remove(x);
                    x.SetActive(false);
                },
                null,
                true,
                poolSize
                );
        }

        public void SpawnerTick(float deltaTime)
        {
            List<GameObject> objectsToReturn = new List<GameObject>();

            var kvpCopy = new KeyValuePair<GameObject, float>[mTimers.Count];
            int index = 0;

            foreach (var kvp in mTimers)
            {
                kvpCopy[index] = new KeyValuePair<GameObject, float>(kvp.Key, kvp.Value - deltaTime);
                if (kvp.Value - deltaTime < 0f)
                    objectsToReturn.Add(kvp.Key);
                index++;
            }

            foreach (var kvp in kvpCopy)
            {
                mTimers[kvp.Key] = kvp.Value;
            }

            foreach (GameObject obj in objectsToReturn)
                mObjectPool.Release(obj);
        }

        public GameObject SpawnObject()
        {
            return mObjectPool.Get();
        }

        public void ReturnObject(GameObject obj)
        {
            mObjectPool.Release(obj);
        }
    }
}