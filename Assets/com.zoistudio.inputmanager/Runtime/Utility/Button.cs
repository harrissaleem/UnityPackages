using UnityEngine;

namespace ZoiStudio.InputManager {

    [AddComponentMenu("ZoiStudio/Input Manager/Button")]
    public class Button : UnityEngine.UI.Button, IInputListener<TouchData> {
        [SerializeField] private string listenerGroup;

        public string ListenerGroup => listenerGroup;

        private bool mIsRegistered = false;

        protected override void Awake() {
            base.Awake();
            if (!mIsRegistered) {
                InputControlHandler<TouchData>.RegisterAsInputListener(this);
                mIsRegistered = true;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
	        InputControlHandler<TouchData>.UnRegisterAsInputListener(this);
	    }

        public void Activate() {
            ///Short term solution for the given problem:
            ///Awake and Start methods of the base class 
            ///get called when the application is quitting
            ///causing up to register and activate again.
            if (TouchInputManager.Instance != null)
                TouchInputManager.Instance.SubscribeToOnUITap(gameObject, this);
        }

        public void Deactivate() {
            var instance = TouchInputManager.Instance;
            if (instance != null)
                instance.UnSubscribeToOnUITap(gameObject);
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