using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimCore.Performance
{
    /// <summary>
    /// Generic object pool for frequently spawned objects.
    /// Reduces GC pressure and improves performance.
    /// </summary>
    /// <typeparam name="T">Type of object to pool.</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly int _maxSize;

        private int _countAll;
        private int _countActive;

        /// <summary>
        /// Number of objects currently in the pool (available).
        /// </summary>
        public int CountInactive => _pool.Count;

        /// <summary>
        /// Number of objects currently in use.
        /// </summary>
        public int CountActive => _countActive;

        /// <summary>
        /// Total objects created by this pool.
        /// </summary>
        public int CountAll => _countAll;

        /// <summary>
        /// Create a new object pool.
        /// </summary>
        /// <param name="createFunc">Function to create new instances.</param>
        /// <param name="onGet">Called when object is retrieved from pool.</param>
        /// <param name="onRelease">Called when object is returned to pool.</param>
        /// <param name="onDestroy">Called when object is destroyed (pool overflow).</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited).</param>
        /// <param name="prewarmCount">Number of objects to create immediately.</param>
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int maxSize = 0,
            int prewarmCount = 0)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;
            _pool = new Stack<T>(prewarmCount > 0 ? prewarmCount : 16);

            // Prewarm
            for (int i = 0; i < prewarmCount; i++)
            {
                var obj = _createFunc();
                _countAll++;
                _onRelease?.Invoke(obj);
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        public T Get()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = _createFunc();
                _countAll++;
            }

            _countActive++;
            _onGet?.Invoke(obj);

            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Release(T obj)
        {
            if (obj == null) return;

            _onRelease?.Invoke(obj);
            _countActive--;

            // Check max size
            if (_maxSize > 0 && _pool.Count >= _maxSize)
            {
                _onDestroy?.Invoke(obj);
                _countAll--;
            }
            else
            {
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// Clear all pooled objects.
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                _onDestroy?.Invoke(obj);
                _countAll--;
            }
        }

        /// <summary>
        /// Prewarm the pool with additional objects.
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = _createFunc();
                _countAll++;
                _onRelease?.Invoke(obj);
                _pool.Push(obj);
            }
        }
    }

    /// <summary>
    /// Component-based object pool for Unity GameObjects.
    /// </summary>
    public class GameObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSize = 10;
        [SerializeField] private int _maxSize = 50;
        [SerializeField] private bool _prewarmOnStart = true;

        private ObjectPool<GameObject> _pool;
        private Transform _poolRoot;

        /// <summary>
        /// Number of objects available in pool.
        /// </summary>
        public int CountInactive => _pool?.CountInactive ?? 0;

        /// <summary>
        /// Number of objects currently in use.
        /// </summary>
        public int CountActive => _pool?.CountActive ?? 0;

        private void Awake()
        {
            Initialize(_prefab, _initialSize, _maxSize);
        }

        private void Start()
        {
            if (_prewarmOnStart && _pool != null)
            {
                _pool.Prewarm(_initialSize);
            }
        }

        /// <summary>
        /// Initialize the pool with a prefab.
        /// </summary>
        public void Initialize(GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            _prefab = prefab;
            _initialSize = initialSize;
            _maxSize = maxSize;

            // Create pool root
            _poolRoot = new GameObject($"Pool_{prefab.name}").transform;
            _poolRoot.SetParent(transform);

            _pool = new ObjectPool<GameObject>(
                createFunc: CreateInstance,
                onGet: OnGetFromPool,
                onRelease: OnReturnToPool,
                onDestroy: OnDestroyPooled,
                maxSize: maxSize
            );
        }

        private GameObject CreateInstance()
        {
            var instance = Instantiate(_prefab, _poolRoot);
            instance.SetActive(false);

            var poolable = instance.GetComponent<IPoolable>();
            poolable?.OnCreated();

            return instance;
        }

        private void OnGetFromPool(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                var poolable = obj.GetComponent<IPoolable>();
                poolable?.OnSpawned();
            }
        }

        private void OnReturnToPool(GameObject obj)
        {
            if (obj != null)
            {
                var poolable = obj.GetComponent<IPoolable>();
                poolable?.OnDespawned();
                obj.SetActive(false);
                obj.transform.SetParent(_poolRoot);
            }
        }

        private void OnDestroyPooled(GameObject obj)
        {
            if (obj != null)
            {
                var poolable = obj.GetComponent<IPoolable>();
                poolable?.OnDestroyed();
                Destroy(obj);
            }
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        public GameObject Get()
        {
            return _pool?.Get();
        }

        /// <summary>
        /// Get an object and set its position/rotation.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Get an object and parent it to a transform.
        /// </summary>
        public GameObject Get(Transform parent)
        {
            var obj = Get();
            if (obj != null)
            {
                obj.transform.SetParent(parent, false);
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Release(GameObject obj)
        {
            _pool?.Release(obj);
        }

        /// <summary>
        /// Clear all pooled objects.
        /// </summary>
        public void Clear()
        {
            _pool?.Clear();
        }

        /// <summary>
        /// Prewarm the pool.
        /// </summary>
        public void Prewarm(int count)
        {
            _pool?.Prewarm(count);
        }
    }

    /// <summary>
    /// Interface for poolable objects to receive lifecycle callbacks.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called once when the object is first created.
        /// </summary>
        void OnCreated();

        /// <summary>
        /// Called each time the object is retrieved from the pool.
        /// </summary>
        void OnSpawned();

        /// <summary>
        /// Called each time the object is returned to the pool.
        /// </summary>
        void OnDespawned();

        /// <summary>
        /// Called when the object is destroyed (pool overflow or cleanup).
        /// </summary>
        void OnDestroyed();
    }

    /// <summary>
    /// Static pool manager for accessing pools by prefab.
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<GameObject, GameObjectPool> _pools = new();
        private static Transform _poolRoot;

        /// <summary>
        /// Get or create a pool for a prefab.
        /// </summary>
        public static GameObjectPool GetPool(GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                return pool;
            }

            // Create pool root if needed
            if (_poolRoot == null)
            {
                var rootObj = new GameObject("PoolManager");
                UnityEngine.Object.DontDestroyOnLoad(rootObj);
                _poolRoot = rootObj.transform;
            }

            // Create new pool
            var poolObj = new GameObject($"Pool_{prefab.name}");
            poolObj.transform.SetParent(_poolRoot);
            pool = poolObj.AddComponent<GameObjectPool>();
            pool.Initialize(prefab, initialSize, maxSize);

            _pools[prefab] = pool;
            return pool;
        }

        /// <summary>
        /// Spawn an object from its pool.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var pool = GetPool(prefab);
            return pool.Get(position, rotation);
        }

        /// <summary>
        /// Return an object to its pool.
        /// </summary>
        public static void Despawn(GameObject obj, GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        /// <summary>
        /// Clear all pools.
        /// </summary>
        public static void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
        }
    }
}
