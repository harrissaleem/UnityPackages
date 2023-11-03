using UnityEngine;
using UnityEngine.Events;

namespace Phezu.Util {
    /// <summary>
    /// Delegates the call to OnTrigger2D for this object to another object.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class OnTriggerDelegator2D : MonoBehaviour {
        private Collider2D caller;

        private void Awake() {
            caller = GetComponent<Collider2D>();
        }

        [Tooltip("Which function should be called when trigger was entered.")]
        public UnityEvent<OnTriggerDelegation2D> Enter;

        [Tooltip("Which function should be called when trigger was exited.")]
        public UnityEvent<OnTriggerDelegation2D> Exit;

        void OnTriggerEnter2D(Collider2D other) => Enter.Invoke(new OnTriggerDelegation2D(caller, other));
        void OnTriggerExit2D(Collider2D other) => Exit.Invoke(new OnTriggerDelegation2D(caller, other));
    }

    /// <summary>
    /// Stores which collider triggered this call and which collider belongs to the other object.
    /// </summary>
    public struct OnTriggerDelegation2D {

        /// <summary>
        /// Creates an OnTriggerDelegation struct.
        /// Stores which collider triggered this call and which collider belongs to the other object.
        /// </summary>
        /// <param name="caller">The trigger collider which triggered the call.</param>
        /// <param name="other">The collider which belongs to the other object.</param>
        public OnTriggerDelegation2D(Collider2D caller, Collider2D other) {
            Caller = caller;
            Other = other;
        }

        /// <summary>
        /// The trigger collider which triggered the call.
        /// </summary>
        public Collider2D Caller { get; private set; }

        /// <summary>
        /// The other collider.
        /// </summary>
        public Collider2D Other { get; private set; }
    }
}