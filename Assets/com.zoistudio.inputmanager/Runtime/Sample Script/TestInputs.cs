using UnityEngine;
using UnityEngine.UI;

namespace ZoiStudio.InputManager
{
	public class TestInputs : MonoBehaviour, IInputListener<TouchData>
	{
		public string ListenerGroup { get; private set; }
		public float ShowTextDuration = 0.1f;

		public Text tapTxt;
		public Text holdTxt;
		public Text velocity;


		float displayTextDuration;
		float displayHoldTextDuration;

		public void Activate()
		{
			// One way of doing it	
			//TouchInputManager.Instance.OnInput += TouchInputManager_OnInput;
			// The way I like
			InputEventManager<TouchData>.Subscribe(this, TouchGameAction.Tap, TouchGameAction.Hold, TouchGameAction.SwipeLeft, TouchGameAction.SwipeRight, TouchGameAction.SwipeUp, TouchGameAction.SwipeDown, TouchGameAction.TapReleased);
		}

		void HandleInputEvent(InputActionArgs<TouchData> inputArgs)
		{
			displayHoldTextDuration = Time.time;
			switch (inputArgs.Action)
			{
				case TouchGameAction.Hold:
					holdTxt.text = inputArgs.Action.ToString();
					break;
				case TouchGameAction.Tap:
				case TouchGameAction.TapReleased:
				case TouchGameAction.SwipeLeft:
				case TouchGameAction.SwipeRight:
				case TouchGameAction.SwipeUp:
				case TouchGameAction.SwipeDown:
					velocity.text = inputArgs.InputData.Velocity.ToString();
					displayTextDuration = Time.time;
					tapTxt.text = inputArgs.Action.ToString();
					break;
				default:
					break;
			}
		}

		private void Update()
		{
			if (Time.time - displayHoldTextDuration > ShowTextDuration)
			{
				displayHoldTextDuration = Time.time;
				holdTxt.text = "";
			}

			if (Time.time - displayTextDuration > ShowTextDuration)
			{
				displayTextDuration = Time.time;
				tapTxt.text = "";
			}
		}

		private void TouchInputManager_OnInput(InputActionArgs<TouchData> action)
		{
			// HandleInputEvent(action);
		}

		public void OnTap()
        {
			tapTxt.text = "Tap";
		}

		public void SwifeLeft()
        {
			tapTxt.text = "Swipe Left";
		}

		public void Deactivate()
		{
			// One way of doing it
			//TouchInputManager.Instance.OnInput -= TouchInputManager_OnInput;		
			// The way I like - but you should in practice always unsubscribe to all actions if the object is getting disabled or deleted
			InputEventManager<TouchData>.UnSubscribe(this, TouchGameAction.Tap);
		}

		public void OnInput(InputActionArgs<TouchData> input)
		{
			HandleInputEvent(input);
		}
	}
}