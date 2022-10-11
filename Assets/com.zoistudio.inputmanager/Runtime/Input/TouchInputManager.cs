using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace ZoiStudio.InputManager {

    [AddComponentMenu("ZoiStudio/Input Manager/Touch Input Manager")]
    public class TouchInputManager : Singleton<TouchInputManager> {
        [SerializeField] private LayerMask UI_Layer;

        public delegate void GetInput(InputActionArgs<TouchData> action);

        public event GetInput OnInput;

        private const int DESKTOP_TOUCH_ID = 0;
        private const float SWIPE_THRESHOLD = 15f;
        public const int MAX_TOUCHES = 4;

        /// <summary>
        /// Will only receive callbacks on touch events that started inside their collider.
        /// </summary>
        private Dictionary<GameObject, IInputListener<TouchData>> mWorldListeners = new();

        /// <summary>
        /// Will only receive callbacks on touch events that started inside their rect.
        /// </summary>
        private Dictionary<GameObject, IInputListener<TouchData>> mUIListeners = new();
       
        /// <summary>
        /// if you touch a UI object, it will be set as a priority listener and only it will receive callbacks until that Touch
        /// </summary>
        private Dictionary<int, IInputListener<TouchData>> mPriorityListeners = new();
        private Dictionary<int, Vector3> mTouchStartPositions = new();
        private Dictionary<int, Vector3> mTouchEndPositions = new();

        private Camera mCamera;
        private float mStartTime;
        private float mEndTime;

        /// <summary>
        /// This listener will get notified if it is on the UI_Layer and the touch events belong to a 
        /// finger that initially tapped on its rect. Only this listener will get notified when it is tapped
        /// and it is not notified if the user taps anywhere else. If it is above a OnWorldTap listener it 
        /// will get prioritized over it.
        /// </summary>
        public void SubscribeToOnUITap(GameObject listenerObj, IInputListener<TouchData> listener) {
            if (!mUIListeners.ContainsKey(listenerObj))
                mUIListeners.Add(listenerObj, listener);
        }
        public void UnSubscribeToOnUITap(GameObject listenerObj) {
            mUIListeners.Remove(listenerObj);
        }

        /// <summary>
        /// This listener will get notified if the touch events belong to a finger that initially tapped
        /// on its collider. Only this listener will get notified when it is tapped and it is not notified
        /// if the user taps anywhere else. Will be ignored if it is behind an OnUITap listener
        /// </summary>
        public void SubscribeToOnWorldTap(GameObject listenerCol, IInputListener<TouchData> listener) {
            mWorldListeners.TryAdd(listenerCol, listener);
        }
        public void UnSubscribeToOnWorldTap(GameObject listenerCol) {
            mWorldListeners.Remove(listenerCol);
        }

        /// <summary>
        /// Call this function if you switch main camera.
        /// </summary>
        public void RefreshCameraCache() {
            mCamera = Camera.main;
        }

        private void Start() {
#if UNITY_EDITOR
            mPriorityListeners[DESKTOP_TOUCH_ID] = null;
            mTouchStartPositions[DESKTOP_TOUCH_ID] = Vector3.zero;
            mTouchEndPositions[DESKTOP_TOUCH_ID] = Vector3.zero;
#endif

            mCamera = Camera.main;
        }

        private void Update() {
            #region Editor
#if UNITY_EDITOR
            RetrieveTouchPositions(DESKTOP_TOUCH_ID, out var touchStartPosition, out var touchEndPosition);

            if (Input.GetKeyDown(KeyCode.Mouse0)) {
                mStartTime = Time.time;
                touchStartPosition = touchEndPosition = Input.mousePosition;
                InvokeTouch(TouchGameAction.Tap, Input.mousePosition, DESKTOP_TOUCH_ID);
            }
            else if (Input.GetKey(KeyCode.Mouse0)) {
                Vector2 deltaPosition = CheckDeltaPosition(touchEndPosition, Input.mousePosition);
                touchEndPosition = Input.mousePosition;

                var velocity = CheckVelocity(touchStartPosition, touchEndPosition);

                InvokeTouch(TouchGameAction.Hold, Input.mousePosition, DESKTOP_TOUCH_ID, deltaPosition);
            }
            else if (Input.GetKeyUp(KeyCode.Mouse0)) {
                mEndTime = Time.time;
                Vector2 deltaPosition = CheckDeltaPosition(touchEndPosition, Input.mousePosition);
                touchEndPosition = Input.mousePosition;

                var gameAction = CheckSwipe(touchStartPosition, touchEndPosition);
                var velocity = CheckVelocity(touchStartPosition, touchEndPosition);

                touchStartPosition = touchEndPosition;

                InvokeTouch(gameAction, Input.mousePosition, DESKTOP_TOUCH_ID, deltaPosition, velocity);
                InvokeTouch(TouchGameAction.HoldReleased, Input.mousePosition, DESKTOP_TOUCH_ID, deltaPosition, velocity);
            }

            SaveTouchPositions(DESKTOP_TOUCH_ID, touchStartPosition, touchEndPosition);
#endif
            #endregion

            #region Mobile
#if !UNITY_EDITOR
			if (Input.touchCount <= 0)
			{
				mPriorityListeners.Clear();
                mTouchStartPositions.Clear();
                mTouchEndPositions.Clear();
				return;
			}
            if (Input.touchCount > MAX_TOUCHES) {
                return;
            }

			foreach (Touch touchevt in Input.touches)
			{
                RetrieveTouchPositions(touchevt.fingerId, out var touchStartPosition, out var touchEndPosition);

				if (touchevt.phase == TouchPhase.Began)
				{
					mStartTime = Time.time;
					touchStartPosition = touchEndPosition = touchevt.position;
					InvokeTouch(TouchGameAction.Tap, touchevt.position, touchevt.fingerId);
				}
				else if (touchevt.phase == TouchPhase.Moved)
				{
                    var deltaPosition = CheckDeltaPosition(touchEndPosition, touchevt.position);

                    touchEndPosition = touchevt.position;

					InvokeTouch(TouchGameAction.Hold, touchevt.position, touchevt.fingerId, deltaPosition);
				}
				else if (touchevt.phase == TouchPhase.Stationary)
				{
					touchStartPosition = touchEndPosition = touchevt.position;
					InvokeTouch(TouchGameAction.Hold, touchevt.position, touchevt.fingerId);
				}
				else if (touchevt.phase == TouchPhase.Ended)
                {
					mEndTime = Time.time;

                    var deltaPosition = CheckDeltaPosition(touchEndPosition, touchevt.position);

					touchEndPosition = touchevt.position;

					var gameAction = CheckSwipe(touchStartPosition, touchEndPosition);
					var velocity = CheckVelocity(touchStartPosition, touchEndPosition);

                    touchStartPosition = touchEndPosition;

					InvokeTouch(gameAction, touchevt.position, touchevt.fingerId, deltaPosition, velocity);
					InvokeTouch(TouchGameAction.HoldReleased, touchevt.position, touchevt.fingerId, deltaPosition, velocity);
				}

                SaveTouchPositions(touchevt.fingerId, touchStartPosition, touchEndPosition);
			}

			if (Input.touches.Length == 2) {
				Debug.Log(Input.touches[0].fingerId + ": " + mPriorityListeners[Input.touches[0].fingerId] + ",  " + Input.touches[1].fingerId + ": " + mPriorityListeners[Input.touches[1].fingerId]);
			}
#endif
            #endregion
        }

        private void RetrieveTouchPositions(int touchID, out Vector3 touchStartPosition, out Vector3 touchEndPosition) {
            if (!mTouchStartPositions.ContainsKey(touchID)) {
                mTouchStartPositions[touchID] = Vector3.zero;
                mTouchEndPositions[touchID] = Vector3.zero;
            }
            touchStartPosition = mTouchStartPositions[touchID];
            touchEndPosition = mTouchEndPositions[touchID];
        }
        private void SaveTouchPositions(int touchID, Vector3 touchStartPosition, Vector3 touchEndPosition) {
            mTouchStartPositions[touchID] = touchStartPosition;
            mTouchEndPositions[touchID] = touchEndPosition;
        }

        private TouchGameAction CheckSwipe(Vector3 startPos, Vector3 endPos) {
            float x = endPos.x - startPos.x;
            float y = endPos.y - startPos.y;
            TouchGameAction gameAction;
            if (Mathf.Abs(x) <= SWIPE_THRESHOLD && Mathf.Abs(y) <= SWIPE_THRESHOLD) {
                gameAction = TouchGameAction.TapReleased;
                return gameAction;
            }

            if (Mathf.Abs(x) > Mathf.Abs(y)) {
                gameAction = x > 0 ? TouchGameAction.SwipeRight : TouchGameAction.SwipeLeft;
            }
            else {
                gameAction = y > 0 ? TouchGameAction.SwipeUp : TouchGameAction.SwipeDown;
            }

            return gameAction;
        }

        private float CheckVelocity(Vector3 startPos, Vector3 endPos) {
            startPos.z = endPos.z = Camera.main.nearClipPlane;

            //Makes the input pixel density independent
            startPos = Camera.main.ScreenToWorldPoint(startPos);
            endPos = Camera.main.ScreenToWorldPoint(endPos);

            float duration = mEndTime - mStartTime;

            //The direction of the swipe
            Vector3 dir = endPos - startPos;

            //The distance of the swipe
            float distance = dir.magnitude;

            //Faster or longer swipes give higher power
            float power = distance / duration;

            //Measure power here
            // Debug.Log("Power " + power);

            return power;
        }

        private Vector2 CheckDeltaPosition(Vector3 endPos, Vector3 currPosition) {
            currPosition.z = endPos.z = Camera.main.nearClipPlane;

            //Makes the input pixel density independent
            currPosition = Camera.main.ScreenToWorldPoint(currPosition);
            endPos = Camera.main.ScreenToWorldPoint(endPos);

            return currPosition - endPos;
        }

        private void InvokeTouch(TouchGameAction action, Vector3 position, int touchID, Vector2 deltaPosition = default, float velocity = 0f) {
            IInputListener<TouchData> priorityListener;

            if (action == TouchGameAction.Tap)
                priorityListener = GetOnHoverListener(position);
            else
                priorityListener = mPriorityListeners[touchID];

            InputActionArgs<TouchData> inputArgs = GetInputArgs(action, position, touchID, deltaPosition, velocity);

            Invoke(inputArgs, ref priorityListener);

            mPriorityListeners[touchID] = priorityListener;
        }

        private void Invoke(InputActionArgs<TouchData> inputArgs, ref IInputListener<TouchData> priorityListener) {
            TouchGameAction action = (TouchGameAction)inputArgs.Action;

            if (priorityListener != null) {
                priorityListener.OnInput(inputArgs);
                if (action == TouchGameAction.HoldReleased || action == TouchGameAction.TapReleased)
                    priorityListener = null;
                return;
            }

            OnInput?.Invoke(inputArgs);
            InputEventManager<TouchData>.Invoke(inputArgs);
        }

        private IInputListener<TouchData> GetOnHoverListener(Vector3 position) {
            IInputListener<TouchData> onHoverListener = null;

            if (Raycast.PointerIsOverUI(position, out List<RaycastResult> raycastResultsUI, UI_Layer)) {
                RaycastResult result = raycastResultsUI[0]; // only considering the one at the front

                if (mUIListeners.ContainsKey(result.gameObject))
                    onHoverListener = mUIListeners[result.gameObject];
            }

            if (onHoverListener != null)
                return onHoverListener;

            if (Raycast.PointerIsOverCollider(position, out List<RaycastResult> raycastResults, mCamera)) {
                RaycastResult result = raycastResults[0]; // only considering the one at the front

                if (mWorldListeners.ContainsKey(result.gameObject))
                    onHoverListener = mWorldListeners[result.gameObject];
            }

            return onHoverListener;
        }

        private InputActionArgs<TouchData> GetInputArgs(TouchGameAction action, Vector3 position, int fingerID, Vector2 deltaPosition, float velocity) {
            return new InputActionArgs<TouchData>() {
                Action = action,

                InputData = new TouchData() {
                    LastTouchPosition = position,
                    DeltaPosition = deltaPosition,
                    FingerID = fingerID,
                    Velocity = velocity
                }
            };
        }
    }
}