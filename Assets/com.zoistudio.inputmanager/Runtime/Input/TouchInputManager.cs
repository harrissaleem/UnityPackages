using UnityEngine;
using System.Collections.Generic;

namespace ZoiStudio.InputManager
{
	public class TouchInputManager : Singleton<TouchInputManager>
	{
		public delegate void GetInput(InputActionArgs<TouchData> action);

		public event GetInput OnInput;

		static float swipeThreshold = 15f;

		private HashSet<IInputListener<TouchData>> mListeners = new HashSet<IInputListener<TouchData>>();
		Vector3 mTouchStartPosition, mTouchEndPosition;
		float mStartTime;
		float mEndTime;

		public void SubscribeToOnUITap(IInputListener<TouchData> listener)
        {
			mListeners.Add(listener);
        }
		public void UnSubscribeToOnUITap(IInputListener<TouchData> listener)
		{
			mListeners.Remove(listener);
		}

		void Update()
		{
#if UNITY_EDITOR
			if (Input.GetKeyDown(KeyCode.Mouse0))
			{
				mStartTime = Time.time;
				mTouchStartPosition = Input.mousePosition;
				Invoke(TouchGameAction.Tap, Input.mousePosition);
			}
			else if (Input.GetKey(KeyCode.Mouse0))
			{
				Invoke(TouchGameAction.Hold, Input.mousePosition);
			}
			else if (Input.GetKeyUp(KeyCode.Mouse0))
			{
				mEndTime = Time.time;
				mTouchEndPosition = Input.mousePosition;

				var gameAction = CheckSwipe();
				var velocity = CheckVelocity();

				Invoke(gameAction, Input.mousePosition, velocity);
				Invoke(TouchGameAction.HoldReleased, Input.mousePosition, velocity);
			}
#endif
#if UNITY_ANDROID
			if (Input.touchCount > 0)
			{
				foreach (Touch touchevt in Input.touches)
				{
					if (touchevt.phase == TouchPhase.Began)
					{
						mStartTime = Time.time;
						mTouchStartPosition = touchevt.position;
						Invoke(TouchGameAction.Tap, touchevt.position);
					}
					// Checking only on ended for now to fix the swipe issue
					else if (/*touchevt.phase == TouchPhase.Moved || */touchevt.phase == TouchPhase.Ended)
					{
						mEndTime = Time.time;
						mTouchEndPosition = touchevt.position;

						var gameAction = CheckSwipe();
						var velocity = CheckVelocity();

						Invoke(gameAction, touchevt.position, velocity);
						Invoke(TouchGameAction.HoldReleased, touchevt.position, velocity);
					}
					else if (touchevt.phase == TouchPhase.Stationary)
					{
						Invoke(TouchGameAction.Hold, touchevt.position);
					}
				}
			}
#endif
		}

		TouchGameAction CheckSwipe()
		{
			float x = mTouchEndPosition.x - mTouchStartPosition.x;
			float y = mTouchEndPosition.y - mTouchStartPosition.y;
			TouchGameAction gameAction;
			if (Mathf.Abs(x) <= swipeThreshold && Mathf.Abs(y) <= swipeThreshold)
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

			return gameAction;
		}

		float CheckVelocity()
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

		void Invoke(TouchGameAction action, Vector3 position, float velocity = 0)
		{
			var inputArgs = GetInputArgs(action, position, velocity);
			OnInput?.Invoke(inputArgs);
			InputEventManager<TouchData>.Invoke(inputArgs);

			if (action == TouchGameAction.Hold)
				return;
			if (UIRaycast.PointerIsOverUI(position, out IInputListener<TouchData> uiListener))
			{
				if (mListeners.Contains(uiListener))
					uiListener.OnInput(inputArgs);
			}
		}

		InputActionArgs<TouchData> GetInputArgs(TouchGameAction action, Vector3 position, float velocity)
		{
			return new InputActionArgs<TouchData>()
			{
				Action = action,
				InputData = new TouchData() 
				{ 
					LastTouchPosition = position, 
					Velocity = velocity 
				}
			};
		}
	}
}