using System;
using UnityEngine;
using UnityEngine.Events;

namespace ZoiStudio.InputManager
{
    public struct InputActionArgs
    {
        public GameAction Action;
        public Vector2 LastTouchPosition;
        public float Velocity;
    }

    [Serializable]
    public struct InputAction
    {
        public GameAction Action;
        public UnityEvent OnAction;
    }
}