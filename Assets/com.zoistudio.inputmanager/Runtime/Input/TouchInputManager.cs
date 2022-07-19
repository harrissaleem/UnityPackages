using UnityEngine;

namespace ZoiStudio.InputManager
{
	public class TouchInputManager : Singleton<TouchInputManager>
	{
		public delegate void GetInput(InputActionArgs<TouchData> action);

		public event GetInput OnInput;

		static float swipeThreshold = 15f;

		Vector3 touchStartPosition, touchEndPosition;
		float startTime;
		float endTime;
		void Update()
		{
#if UNITY_EDITOR
			if (Input.GetKeyDown(KeyCode.Mouse0))
			{
				startTime = Time.time;
				touchStartPosition = Input.mousePosition;
				Invoke(TouchGameAction.Tap, Input.mousePosition);
			}
			else if (Input.GetKey(KeyCode.Mouse0))
			{
				Invoke(TouchGameAction.Hold, Input.mousePosition);
			}
			else if (Input.GetKeyUp(KeyCode.Mouse0))
			{
				endTime = Time.time;
				touchEndPosition = Input.mousePosition;

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
						startTime = Time.time;
						touchStartPosition = touchevt.position;
						Invoke(TouchGameAction.Tap, touchevt.position);
					}
					// Checking only on ended for now to fix the swipe issue
					else if (/*touchevt.phase == TouchPhase.Moved || */touchevt.phase == TouchPhase.Ended)
					{
						endTime = Time.time;
						touchEndPosition = touchevt.position;

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
			float x = touchEndPosition.x - touchStartPosition.x;
			float y = touchEndPosition.y - touchStartPosition.y;
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
			touchStartPosition.z = touchEndPosition.z = Camera.main.nearClipPlane;

			//Makes the input pixel density independent
			touchStartPosition = Camera.main.ScreenToWorldPoint(touchStartPosition);
			touchEndPosition = Camera.main.ScreenToWorldPoint(touchEndPosition);

			float duration = endTime - startTime;

			//The direction of the swipe
			Vector3 dir = touchEndPosition - touchStartPosition;

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
			if (UIRaycast.PointerIsOverUI(position, out IInputListener<TouchData> uiListerener))
			{
				uiListerener.OnInput(inputArgs);
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