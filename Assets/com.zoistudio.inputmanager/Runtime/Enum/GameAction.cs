namespace ZoiStudio.InputManager
{
    public enum TouchGameAction
    {
        Tap,
        /// <summary>
        /// Called if touch was not dragged sufficiently to be considered a swipe
        /// </summary>
        TapReleased,
        Hold,
        /// <summary>
        /// Called every time the user lifts his/her finger
        /// </summary>
        HoldReleased,
        SwipeLeft,
        SwipeRight,
        SwipeUp,
        SwipeDown
    }
    
    public enum DesktopGameAction
    {
        KeyDown,
        KeyUp,
        MouseButtonDown,
        MouseButtonUp,
        MouseMove
    }
}