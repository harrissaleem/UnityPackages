using System;

namespace SimCore.Services
{
    /// <summary>
    /// Base interface for all services in the SimCore framework.
    /// Services are singleton-like systems providing cross-cutting functionality.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Initialize the service. Called once at startup.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shutdown the service. Called once at application quit.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// Interface for services that need per-frame updates.
    /// </summary>
    public interface ITickableService : IService
    {
        /// <summary>
        /// Called every frame for services that need updates.
        /// </summary>
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Interface for services that need to respond to app lifecycle events.
    /// </summary>
    public interface ILifecycleAwareService : IService
    {
        /// <summary>
        /// Called when the application is paused (mobile: app goes to background).
        /// </summary>
        void OnApplicationPause(bool paused);

        /// <summary>
        /// Called when the application focus changes.
        /// </summary>
        void OnApplicationFocus(bool hasFocus);
    }
}
