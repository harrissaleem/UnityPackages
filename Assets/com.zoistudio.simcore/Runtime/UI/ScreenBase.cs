using System;
using UnityEngine;

namespace SimCore.UI
{
    /// <summary>
    /// Base class for all UI screens.
    /// Screens are full-screen UI views that participate in stack-based navigation.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier for this screen.
        /// </summary>
        public abstract string ScreenId { get; }

        /// <summary>
        /// Reference to the UI navigator (set automatically).
        /// </summary>
        public UINavigator Navigator { get; internal set; }

        /// <summary>
        /// Whether this screen allows back navigation.
        /// </summary>
        protected virtual bool AllowBack => true;

        /// <summary>
        /// Called when the screen is shown.
        /// Override to initialize UI with provided data.
        /// </summary>
        /// <param name="data">Optional data passed during navigation.</param>
        public virtual void OnShow(object data) { }

        /// <summary>
        /// Called when the screen is hidden (pushed to stack or replaced).
        /// Override to cleanup or pause screen-specific logic.
        /// </summary>
        public virtual void OnHide() { }

        /// <summary>
        /// Called when the screen is resumed from the stack.
        /// Override to refresh UI or restart logic.
        /// </summary>
        public virtual void OnResume() { }

        /// <summary>
        /// Called when the back button is pressed.
        /// Return true to allow navigation back, false to block.
        /// </summary>
        public virtual bool OnBackPressed()
        {
            return AllowBack;
        }

        /// <summary>
        /// Navigate to another screen (push).
        /// </summary>
        protected void NavigateTo(string screenId, object data = null)
        {
            Navigator?.PushScreen(screenId, data);
        }

        /// <summary>
        /// Replace this screen with another (no back stack).
        /// </summary>
        protected void ReplaceTo(string screenId, object data = null)
        {
            Navigator?.ReplaceScreen(screenId, data);
        }

        /// <summary>
        /// Go back to previous screen.
        /// </summary>
        protected void GoBack()
        {
            Navigator?.PopScreen();
        }
    }

    /// <summary>
    /// Screen that binds to a data model.
    /// </summary>
    /// <typeparam name="TData">Type of data model.</typeparam>
    public abstract class ScreenBase<TData> : ScreenBase where TData : class
    {
        /// <summary>
        /// The data model for this screen.
        /// </summary>
        protected TData Data { get; private set; }

        public override void OnShow(object data)
        {
            Data = data as TData;
            OnBind(Data);
        }

        /// <summary>
        /// Called when data is bound to the screen.
        /// Override to populate UI elements.
        /// </summary>
        protected abstract void OnBind(TData data);
    }

    /// <summary>
    /// Base class for modal dialogs/popups.
    /// Modals display on top of screens and don't participate in the navigation stack.
    /// </summary>
    public abstract class ModalBase : MonoBehaviour
    {
        /// <summary>
        /// Reference to the UI navigator (set automatically).
        /// </summary>
        public UINavigator Navigator { get; internal set; }

        /// <summary>
        /// Whether clicking outside the modal closes it.
        /// </summary>
        protected virtual bool CloseOnOutsideClick => true;

        /// <summary>
        /// Whether the back button closes this modal.
        /// </summary>
        protected virtual bool CloseOnBack => true;

        /// <summary>
        /// Called when the modal is shown.
        /// </summary>
        public virtual void OnShow(object data) { }

        /// <summary>
        /// Called when the modal is hidden.
        /// </summary>
        public virtual void OnHide() { }

        /// <summary>
        /// Called when back button is pressed.
        /// Return true to close the modal.
        /// </summary>
        public virtual bool OnBackPressed()
        {
            return CloseOnBack;
        }

        /// <summary>
        /// Close this modal.
        /// </summary>
        protected void Close()
        {
            Navigator?.HideModal(this);
        }
    }

    /// <summary>
    /// Modal that binds to a data model.
    /// </summary>
    public abstract class ModalBase<TData> : ModalBase where TData : class
    {
        protected TData Data { get; private set; }

        public override void OnShow(object data)
        {
            Data = data as TData;
            OnBind(Data);
        }

        protected abstract void OnBind(TData data);
    }

    /// <summary>
    /// Base class for HUD (Heads-Up Display) components.
    /// HUDs are persistent UI elements shown during gameplay.
    /// </summary>
    public abstract class HUDBase : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier for this HUD.
        /// </summary>
        public abstract string HUDId { get; }

        /// <summary>
        /// Reference to the UI navigator (set automatically).
        /// </summary>
        public UINavigator Navigator { get; internal set; }

        /// <summary>
        /// Called when the HUD is shown.
        /// </summary>
        public virtual void OnShow() { }

        /// <summary>
        /// Called when the HUD is hidden.
        /// </summary>
        public virtual void OnHide() { }

        /// <summary>
        /// Called every frame while the HUD is active.
        /// </summary>
        public virtual void Tick(float deltaTime) { }
    }
}
