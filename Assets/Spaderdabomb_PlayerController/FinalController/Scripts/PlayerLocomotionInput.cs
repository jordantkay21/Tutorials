using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spaderdabomb.PlayerController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionActions
    {
        #region Class Variables
        [Tooltip("Configurable boolean that controls whether sprinting is a <hold action> or a <toggle action>")]
        [SerializeField] bool holdToSprint = true;

        [Tooltip("Indicates whether that player is currently sprinting or not")]
        public bool SprintToggleOn { get; private set; }
        public bool WalkToggleOn { get; private set; }
        public bool JumpPressed { get; private set; }
        public PlayerControls PlayerControls { get; private set; }
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        #endregion

        #region Startup
        private void OnEnable()
        {
            PlayerControls = new PlayerControls();
            PlayerControls.Enable();

            PlayerControls.PlayerLocomotion.Enable();
            PlayerControls.PlayerLocomotion.SetCallbacks(this);
        }

        private void OnDisable()
        {
            PlayerControls.PlayerLocomotion.Disable();
            PlayerControls.PlayerLocomotion.RemoveCallbacks(this); 
        }
        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            JumpPressed = false;
        }
        #endregion

        #region Input Callback
        public void OnMovement(InputAction.CallbackContext context)
        {
            MovementInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
        }

        public void OnToggleSprint(InputAction.CallbackContext context)
        {
            if (context.performed)
                SprintToggleOn = holdToSprint || !SprintToggleOn;
            if (context.canceled)
                SprintToggleOn = !holdToSprint && SprintToggleOn;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            JumpPressed = true;
        }

        public void OnToggleWalk(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            WalkToggleOn = !WalkToggleOn;
        }
        #endregion
    }
}