using UnityEngine;

namespace ZoiStudio.InputManager
{
    public interface IInputListener<T> where T : struct
    {
        public string ListenerGroup { get; }
        public void OnInput(InputActionArgs<T> action);
        /// <summary>
        /// Subscribe to the gameActions in this function
        /// </summary>
        public void Activate();
        /// <summary>
        /// UnSubscribe to the gameActions you subscribed to in this function
        /// </summary>
        public void Deactivate();
    }

}
