namespace ZoiStudio.InputManager
{
    public interface IInputListener<T> where T : struct
    {
        void OnInput(InputActionArgs<T> action);
    }

}
