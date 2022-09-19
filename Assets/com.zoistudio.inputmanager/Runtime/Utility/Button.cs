using UnityEngine;

namespace ZoiStudio.InputManager {

    [AddComponentMenu("ZoiStudio/Input Manager/Button")]
    public class Button : UnityEngine.UI.Button, IInputListener<TouchData> {
        [SerializeField] private string listenerGroup;

        public string ListenerGroup => listenerGroup;

        private bool mIsRegistered = false;

        protected override void Start() {
            base.Awake();
            if (!mIsRegistered) {
                InputControlHandler<TouchData>.RegisterAsInputListener(this);
                mIsRegistered = true;
            }
        }

        public void Activate() {
            TouchInputManager.Instance.SubscribeToOnUITap(gameObject, this);
        }

        public void Deactivate() {
            TouchInputManager.Instance.UnSubscribeToOnUITap(gameObject);
        }

        public void OnInput(InputActionArgs<TouchData> action) {
            TouchGameAction Action = (TouchGameAction)action.Action;
            switch (Action) {
                case TouchGameAction.Tap:
                    onClick.Invoke();
                    break;
            }
        }
    }
}