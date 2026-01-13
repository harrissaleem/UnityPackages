using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimCore.Services
{
    /// <summary>
    /// Service Locator pattern implementation for accessing game services.
    /// Provides a central registry for services without tight coupling.
    ///
    /// Usage:
    ///   ServiceLocator.Register&lt;IAdService&gt;(new AdService());
    ///   var adService = ServiceLocator.Get&lt;IAdService&gt;();
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> _services = new();
        private static readonly List<ITickableService> _tickableServices = new();
        private static readonly List<ILifecycleAwareService> _lifecycleServices = new();
        private static bool _isInitialized;

        /// <summary>
        /// Register a service instance. Call during game initialization.
        /// </summary>
        public static void Register<T>(T service) where T : class, IService
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Replacing.");
                Unregister<T>();
            }

            _services[type] = service;

            if (service is ITickableService tickable)
            {
                _tickableServices.Add(tickable);
            }

            if (service is ILifecycleAwareService lifecycle)
            {
                _lifecycleServices.Add(lifecycle);
            }

            Debug.Log($"[ServiceLocator] Registered: {type.Name}");
        }

        /// <summary>
        /// Unregister a service. Call during shutdown or when replacing services.
        /// </summary>
        public static void Unregister<T>() where T : class, IService
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                if (service is ITickableService tickable)
                {
                    _tickableServices.Remove(tickable);
                }

                if (service is ILifecycleAwareService lifecycle)
                {
                    _lifecycleServices.Remove(lifecycle);
                }

                _services.Remove(type);
                Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
            }
        }

        /// <summary>
        /// Get a registered service instance.
        /// </summary>
        /// <returns>The service instance, or null if not registered.</returns>
        public static T Get<T>() where T : class, IService
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            Debug.LogWarning($"[ServiceLocator] Service {type.Name} not registered.");
            return null;
        }

        /// <summary>
        /// Try to get a registered service instance.
        /// </summary>
        /// <returns>True if service exists, false otherwise.</returns>
        public static bool TryGet<T>(out T service) where T : class, IService
        {
            service = Get<T>();
            return service != null;
        }

        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        public static bool Has<T>() where T : class, IService
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Initialize all registered services. Call once at game startup.
        /// </summary>
        public static void InitializeAll()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ServiceLocator] Already initialized.");
                return;
            }

            Debug.Log($"[ServiceLocator] Initializing {_services.Count} services...");

            foreach (var kvp in _services)
            {
                try
                {
                    kvp.Value.Initialize();
                    Debug.Log($"[ServiceLocator] Initialized: {kvp.Key.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ServiceLocator] Failed to initialize {kvp.Key.Name}: {e}");
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Shutdown all registered services. Call once at application quit.
        /// </summary>
        public static void ShutdownAll()
        {
            Debug.Log($"[ServiceLocator] Shutting down {_services.Count} services...");

            foreach (var kvp in _services)
            {
                try
                {
                    kvp.Value.Shutdown();
                    Debug.Log($"[ServiceLocator] Shutdown: {kvp.Key.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ServiceLocator] Failed to shutdown {kvp.Key.Name}: {e}");
                }
            }

            _services.Clear();
            _tickableServices.Clear();
            _lifecycleServices.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Tick all tickable services. Call from main game loop.
        /// </summary>
        public static void TickAll(float deltaTime)
        {
            for (int i = 0; i < _tickableServices.Count; i++)
            {
                try
                {
                    _tickableServices[i].Tick(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ServiceLocator] Tick error in {_tickableServices[i].GetType().Name}: {e}");
                }
            }
        }

        /// <summary>
        /// Notify all lifecycle-aware services of pause state change.
        /// </summary>
        public static void NotifyApplicationPause(bool paused)
        {
            for (int i = 0; i < _lifecycleServices.Count; i++)
            {
                try
                {
                    _lifecycleServices[i].OnApplicationPause(paused);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ServiceLocator] OnApplicationPause error: {e}");
                }
            }
        }

        /// <summary>
        /// Notify all lifecycle-aware services of focus change.
        /// </summary>
        public static void NotifyApplicationFocus(bool hasFocus)
        {
            for (int i = 0; i < _lifecycleServices.Count; i++)
            {
                try
                {
                    _lifecycleServices[i].OnApplicationFocus(hasFocus);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ServiceLocator] OnApplicationFocus error: {e}");
                }
            }
        }

        /// <summary>
        /// Clear all services without calling shutdown. Use for testing only.
        /// </summary>
        public static void ClearForTesting()
        {
            _services.Clear();
            _tickableServices.Clear();
            _lifecycleServices.Clear();
            _isInitialized = false;
        }
    }
}
