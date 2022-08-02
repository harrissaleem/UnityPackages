using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

namespace ZoiStudio.InputManager
{
	public class TouchInputManager : Singleton<TouchInputManager>
	{
		public delegate void GetInput(InputActionArgs<TouchData> action);

		public event GetInput OnInput;

		private const int DESKTOP_TOUCH_ID = 0;
		private const float SWIPE_THRESHOLD = 15f;
		public const int MAX_TOUCHES = 4;

		/// <summary>
		/// You have to subscribe to receive a callback on raycast
		/// </summary>
		private Hashtable mListeners = new Hashtable();
		/// <summary>
		/// if you touch a UI object, it will be set as a priority listener and only it will receive callbacks until that Touch
		/// </summary>
		private Hashtable mPriorityListeners = new Hashtable();

		private Vector3 mTouchStartPosition, mTouchEndPosition;
		private float mStartTime;
		private float mEndTime;

		public void SubscribeToOnUITap(GameObject listenerObj, IInputListener<TouchData> listener)
        {
			if (!mListeners.ContainsKey(listenerObj))
				mListeners.Add(listenerObj, listener);
        }
		public void UnSubscribeToOnUITap(GameObject listenerObj)
		{
			mListeners.Remove(listenerObj);
		}

        private void Start()
		{
#if UNITY_EDITOR
			mPriorityListeners[DESKTOP_TOUCH_ID] = null;
#endif
		}

        private void Update()
		{
            #region Editor
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Mouse0))
			{
				mStartTime = Time.time;
				mTouchStartPosition = Input.mousePosition;
				InvokeTouch(TouchGameAction.Tap, Input.mousePosition, DESKTOP_TOUCH_ID);
			}
			else if (Input.GetKey(KeyCode.Mouse0))
			{
				InvokeTouch(TouchGameAction.Hold, Input.mousePosition, DESKTOP_TOUCH_ID);
			}
			else if (Input.GetKeyUp(KeyCode.Mouse0))
			{
				mEndTime = Time.time;
                mTouchEndPosition = Input.mousePosition;

                var gameAction = CheckSwipe();
                var velocity = CheckVelocity();

				InvokeTouch(gameAction, Input.mousePosition, DESKTOP_TOUCH_ID, velocity);
				InvokeTouch(TouchGameAction.HoldReleased, Input.mousePosition, DESKTOP_TOUCH_ID, velocity);
			}
#endif
			#endregion

			#region Mobile
#if !UNITY_EDITOR
			if (Input.touchCount <= 0 || Input.touchCount > MAX_TOUCHES)
			{
				mPriorityListeners.Clear();
				return;
			}

			foreach (Touch touchevt in Input.touches)
			{
				if (touchevt.phase == TouchPhase.Began)
				{
					mStartTime = Time.time;
					mTouchStartPosition = touchevt.position;
					InvokeTouch(TouchGameAction.Tap, touchevt.position, touchevt.fingerId);
				}
				else if (touchevt.phase == TouchPhase.Moved)
				{
					InvokeTouch(TouchGameAction.Hold, touchevt.position, touchevt.fingerId, CheckVelocity());
				}
				else if (touchevt.phase == TouchPhase.Stationary)
				{
					mTouchStartPosition = touchevt.position;
					InvokeTouch(TouchGameAction.Hold, touchevt.position, touchevt.fingerId);
				}
				else if (touchevt.phase == TouchPhase.Ended)
                {
					mEndTime = Time.time;
					mTouchEndPosition = touchevt.position;

					var gameAction = CheckSwipe();
					var velocity = CheckVelocity();

					InvokeTouch(gameAction, touchevt.position, touchevt.fingerId, velocity);
					InvokeTouch(TouchGameAction.HoldReleased, touchevt.position, touchevt.fingerId);
				}
			}

			if (Input.touches.Length == 2) {
				Debug.Log(Input.touches[0].fingerId + ": " + mPriorityListeners[Input.touches[0].fingerId] + ",  " + Input.touches[1].fingerId + ": " + mPriorityListeners[Input.touches[1].fingerId]);
			}
#endif
			#endregion
		}

        private TouchGameAction CheckSwipe()
		{
			float x = mTouchEndPosition.x - mTouchStartPosition.x;
			float y = mTouchEndPosition.y - mTouchStartPosition.y;
			TouchGameAction gameAction;
			if (Mathf.Abs(x) <= SWIPE_THRESHOLD && Mathf.Abs(y) <= SWIPE_THRESHOLD)
			{
				gameAction = TouchGameAction.TapReleased;
				return gameAction;
			}

			if (Mathf.Abs(x) > Mathf.Abs(y))
			{
				gameAction = x > 0 ? TouchGameAction.SwipeRight : TouchGameAction.SwipeLeft;
			}
			else
			{
				gameAction = y > 0 ? TouchGameAction.SwipeUp : TouchGameAction.SwipeDown;
			}

			mTouchStartPosition = mTouchEndPosition;

			return gameAction;
		}

		private float CheckVelocity()
		{
			mTouchStartPosition.z = mTouchEndPosition.z = Camera.main.nearClipPlane;

			//Makes the input pixel density independent
			mTouchStartPosition = Camera.main.ScreenToWorldPoint(mTouchStartPosition);
			mTouchEndPosition = Camera.main.ScreenToWorldPoint(mTouchEndPosition);

			float duration = mEndTime - mStartTime;

			//The direction of the swipe
			Vector3 dir = mTouchEndPosition - mTouchStartPosition;

			//The distance of the swipe
			float distance = dir.magnitude;

			//Faster or longer swipes give higher power
			float power = distance / duration;

			//Measure power here
			// Debug.Log("Power " + power);

			return power;
		}

		private void InvokeTouch(TouchGameAction action, Vector3 position, int touchID, float velocity = 0)
        {
			IInputListener<TouchData> priorityListener;
			if (action == TouchGameAction.Tap)
				priorityListener = GetOnHoverListener(position);
			else
				priorityListener = (IInputListener<TouchData>)mPriorityListeners[touchID];

			InputActionArgs<TouchData> inputArgs = GetInputArgs(action, position, touchID, velocity);

			Invoke(inputArgs, ref priorityListener);

			mPriorityListeners[touchID] = priorityListener;
		}

		private void Invoke(InputActionArgs<TouchData> inputArgs, ref IInputListener<TouchData> priorityListener)
		{
			TouchGameAction action = (TouchGameAction)inputArgs.Action;
			if (priorityListener != null)
			{
				priorityListener.OnInput(inputArgs);
				if (action == TouchGameAction.HoldReleased || action == TouchGameAction.TapReleased)
					priorityListener = null;
				return;
			}
			OnInput?.Invoke(inputArgs);
			InputEventManager<TouchData>.Invoke(inputArgs);
		}

		private IInputListener<TouchData> GetOnHoverListener(Vector3 position)
        {
			if (UIRaycast.PointerIsOverUI(position, out List<RaycastResult> raycastResults))
			{
				RaycastResult result = raycastResults[0]; // only considering the one at the front!

				if (mListeners.Contains(result.gameObject))
					return (IInputListener<TouchData>)mListeners[result.gameObject];
			}
			return null;
		}

		private InputActionArgs<TouchData> GetInputArgs(TouchGameAction action, Vector3 position, int fingerID, float velocity)
		{
			return new InputActionArgs<TouchData>()
			{
				Action = action,
				InputData = new TouchData() 
				{ 
					LastTouchPosition = position, 
					FingerID = fingerID,
					Velocity = velocity 
				}
			};
		}
	}
}