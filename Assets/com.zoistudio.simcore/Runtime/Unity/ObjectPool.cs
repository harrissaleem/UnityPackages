// SimCore - Object Pool
// Generic object pooling system to avoid Instantiate/Destroy overhead
// Critical for mobile performance with many NPCs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimCore.Unity
{
    /// <summary>
    /// Generic object pool for GameObjects with a specific component
    /// Eliminates Instantiate/Destroy overhead for frequently spawned objects
    /// </summary>
    public class GameObjectPool<T> where T : Component
    {
        private readonly Stack<T> _available = new Stack<T>();
        private readonly HashSet<T> _inUse = new HashSet<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;
        private readonly Transform _poolParent;
        private readonly int _maxPoolSize;
        
        /// <summary>
        /// Create a new pool
        /// </summary>
        /// <param name="factory">Function to create new instances</param>
        /// <param name="onGet">Called when object is retrieved from pool</param>
        /// <param name="onReturn">Called when object is returned to pool</param>
        /// <param name="initialSize">Pre-warm pool with this many objects</param>
        /// <param name="maxPoolSize">Maximum objects to keep in pool (excess will be destroyed)</param>
        /// <param name="poolParent">Parent transform for pooled objects</param>
        public GameObjectPool(
            Func<T> factory, 
            Action<T> onGet = null, 
            Action<T> onReturn = null,
            int initialSize = 0,
            int maxPoolSize = 100,
            Transform poolParent = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onReturn = onReturn;
            _maxPoolSize = maxPoolSize;
            _poolParent = poolParent;
            
            // Pre-warm pool
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                if (_poolParent != null)
                {
                    obj.transform.SetParent(_poolParent);
                }
                _available.Push(obj);
            }
        }
        
        private T CreateNew()
        {
            return _factory();
        }
        
        /// <summary>
        /// Get an object from the pool (or create new if empty)
        /// </summary>
        public T Get()
        {
            T obj;
            
            if (_available.Count > 0)
            {
                obj = _available.Pop();
            }
            else
            {
                obj = CreateNew();
            }
            
            obj.gameObject.SetActive(true);
            _inUse.Add(obj);
            _onGet?.Invoke(obj);
            
            return obj;
        }
        
        /// <summary>
        /// Get an object and position it
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }
        
        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;
            
            if (!_inUse.Remove(obj))
            {
                // Object wasn't from this pool - just destroy it
                UnityEngine.Object.Destroy(obj.gameObject);
                return;
            }
            
            _onReturn?.Invoke(obj);
            
            if (_available.Count < _maxPoolSize)
            {
                obj.gameObject.SetActive(false);
                if (_poolParent != null)
                {
                    obj.transform.SetParent(_poolParent);
                }
                _available.Push(obj);
            }
            else
            {
                // Pool is full, destroy excess
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
        
        /// <summary>
        /// Clear the pool and destroy all objects
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _available)
            {
                if (obj != null && obj.gameObject != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            _available.Clear();
            
            foreach (var obj in _inUse)
            {
                if (obj != null && obj.gameObject != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            _inUse.Clear();
        }
        
        /// <summary>
        /// Number of objects available in pool
        /// </summary>
        public int AvailableCount => _available.Count;
        
        /// <summary>
        /// Number of objects currently in use
        /// </summary>
        public int InUseCount => _inUse.Count;
        
        /// <summary>
        /// Total objects managed by pool
        /// </summary>
        public int TotalCount => _available.Count + _inUse.Count;
    }
    
    /// <summary>
    /// Pool manager for multiple prefab types
    /// </summary>
    public class PrefabPoolManager : MonoBehaviour
    {
        private static PrefabPoolManager _instance;
        public static PrefabPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[PrefabPoolManager]");
                    _instance = go.AddComponent<PrefabPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<GameObject, GameObjectPool<Transform>> _pools = 
            new Dictionary<GameObject, GameObjectPool<Transform>>();
        
        private readonly Dictionary<GameObject, Transform> _poolContainers = 
            new Dictionary<GameObject, Transform>();
        
        /// <summary>
        /// Pre-warm a pool for a prefab
        /// </summary>
        public void WarmPool(GameObject prefab, int count)
        {
            GetOrCreatePool(prefab, count);
        }
        
        /// <summary>
        /// Get or create a pool for a prefab
        /// </summary>
        private GameObjectPool<Transform> GetOrCreatePool(GameObject prefab, int warmCount = 0)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                // Create container for pooled objects
                var container = new GameObject($"[Pool] {prefab.name}");
                container.transform.SetParent(transform);
                _poolContainers[prefab] = container.transform;
                
                pool = new GameObjectPool<Transform>(
                    factory: () => Instantiate(prefab).transform,
                    onGet: t => t.SetParent(null),
                    onReturn: t => ResetPooledObject(t),
                    initialSize: warmCount,
                    maxPoolSize: 150,
                    poolParent: container.transform
                );
                
                _pools[prefab] = pool;
            }
            
            return pool;
        }
        
        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var pool = GetOrCreatePool(prefab);
            var t = pool.Get(position, rotation);
            return t.gameObject;
        }
        
        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(GameObject prefab, GameObject instance)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                pool.Return(instance.transform);
            }
            else
            {
                // No pool for this prefab - just destroy
                Destroy(instance);
            }
        }
        
        /// <summary>
        /// Return an object without knowing its prefab (searches pools)
        /// </summary>
        public void ReturnAny(GameObject instance)
        {
            // Simple destroy - we don't track which pool an instance came from
            // For full tracking, you'd need to store the prefab reference on the instance
            Destroy(instance);
        }
        
        /// <summary>
        /// Reset a pooled object to default state
        /// </summary>
        private void ResetPooledObject(Transform t)
        {
            // Reset common components
            var npcMovement = t.GetComponent<NPCMovement>();
            if (npcMovement != null)
            {
                npcMovement.Stop();
            }
            
            // Reset NavMeshAgent
            var agent = t.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }
                agent.enabled = false;
            }
            
            // Reset Animator
            var animator = t.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Rebind();
                animator.enabled = false;
            }
            
            // Reset Rigidbody
            var rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            
            foreach (var container in _poolContainers.Values)
            {
                if (container != null)
                {
                    Destroy(container.gameObject);
                }
            }
            _poolContainers.Clear();
        }
        
        private void OnDestroy()
        {
            ClearAll();
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

