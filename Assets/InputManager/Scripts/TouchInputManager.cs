using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class TouchInputManager : Singleton<TouchInputManager>
{
	public delegate void GetInput(InputActionArgs action);

	public event GetInput OnInput;

	static float swipeThreshold = 15f;

	Vector2 touchStartPosition, touchEndPosition;
	float startTime;
	float endTime;
	void Update()
	{
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			startTime = Time.time;
			touchStartPosition = Input.mousePosition;
			Invoke(GameAction.Tap, touchStartPosition);
		}
		else if (Input.GetKey(KeyCode.Mouse0))
		{
			Invoke(GameAction.Hold, touchStartPosition);
		}
		else if (Input.GetKeyUp(KeyCode.Mouse0))
		{
			endTime = Time.time;
			touchEndPosition = Input.mousePosition;

			var gameAction = CheckGameAction();
			var velocity = CheckVelocity();

			Invoke(gameAction, touchEndPosition, velocity);
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
					Invoke(GameAction.Tap, touchevt.position);
				}
				// Checking only on ended for now to fix the swipe issue
				else if (/*touchevt.phase == TouchPhase.Moved || */touchevt.phase == TouchPhase.Ended)
				{
					endTime = Time.time;
					touchEndPosition = touchevt.position;

					var gameAction = CheckGameAction();
					var velocity = CheckVelocity();

					Invoke(gameAction, touchevt.position, velocity);
				}
				else if (touchevt.phase == TouchPhase.Stationary)
				{
					Invoke(GameAction.Hold, touchevt.position);
				}
			}
		}
#endif
	}

	GameAction CheckGameAction()
	{
		float x = touchEndPosition.x - touchStartPosition.x;
		float y = touchEndPosition.y - touchStartPosition.y;
		GameAction gameAction;
		if (Mathf.Abs(x) <= swipeThreshold && Mathf.Abs(y) <= swipeThreshold)
		{
			// Touch was a tap and it ended. This can be used as tap Up if needed
			return GameAction.TapReleased;
		}

		if (Mathf.Abs(x) > Mathf.Abs(y))
		{
			gameAction = x > 0 ? GameAction.SwipeRight : GameAction.SwipeLeft;
		}
		else
		{
			gameAction = y > 0 ? GameAction.SwipeUp : GameAction.SwipeDown;
		}

		return gameAction;
	}

	float CheckVelocity()
	{
		float x = touchEndPosition.x - touchStartPosition.x;

		float time = endTime - startTime;
		float velocity = x / time;

		return velocity;
	}

	void Invoke(GameAction action, Vector3 position, float velocity = 0)
	{
		var inputArgs = GetInputArgs(action, position, velocity);
		OnInput?.Invoke(inputArgs);
		InputEventManager.Invoke(inputArgs);
	}

	InputActionArgs GetInputArgs(GameAction action, Vector3 position, float velocity)
	{
		return new InputActionArgs()
		{
			Action = action,
			LastTouchPosition = position,
			Velocity = velocity
		};
	}
}
