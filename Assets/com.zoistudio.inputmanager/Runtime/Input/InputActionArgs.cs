using System;
using UnityEngine;
using UnityEngine.Events;

namespace ZoiStudio.InputManager
{
    public struct InputActionArgs<T> where T : struct
    {
        public Enum Action;
        public T InputData;
    }

    public struct TouchData
    {
        public Vector2 LastTouchPosition;
        public Vector2 DeltaPosition;
        public int FingerID;
        public float Velocity;
    }

    public struct DesktopData
    {
        public KeyCode keyCode;
        public Vector2 mousePosition;
        public Vector2 deltaMouse;
        public int mouseButton;
    }

    [Serializable]
    public struct InputAction
    {
        public Enum Action;
        public UnityEvent OnAction;
    }
}