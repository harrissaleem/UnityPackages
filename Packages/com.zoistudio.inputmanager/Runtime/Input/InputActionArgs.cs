using UnityEngine;

namespace ZoiStudio.InputManager
{
    public struct InputActionArgs
    {
        public GameAction Action;
        public Vector2 LastTouchPosition;
        public float Velocity;
    }
}