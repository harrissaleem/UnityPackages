using System;
using UnityEngine;

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
        public float Velocity;
    }

    public struct DesktopData {
        public KeyCode KeyCode;
        public Vector2 mousePosition;
        public Vector2 deltaMouse;
    }
}