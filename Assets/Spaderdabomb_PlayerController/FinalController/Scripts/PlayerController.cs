using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spaderdabomb.PlayerController
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        #region Class Variables
        [Header("Components")]
        [SerializeField] CharacterController _characterController;
        [SerializeField] Camera _playerCamera;

        [Header("Base Movement")]
        public float runAcceleration = 0.25f;
        public float runSpeed = 4f;
        public float sprintAcceleration = 0.5f;
        public float sprintSpeed = 7;
        public float drag = 0.1f;
        public float gravity;
        public float jumpSpeed;
        public float movementThreshold = 0.01f;

        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 89f;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;

        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;

        private float _verticalVelocity = 0f;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            UpdateMovementState();
            HandleVerticalMovement();
            HandleLateralMovement();
        }

        private void UpdateMovementState()
        {
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;   //order
            bool isMovingLaterally = IsMovingLaterally();                                  //matter
            bool isSprinting = _playerLocomotionInput.SprintToggleOn && isMovingLaterally; //order matters
            bool isGrounded = IsGrounded();

            PlayerMovementState lateralState = isSprinting ? PlayerMovementState.Sprinting :
                                                isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;
            
            _playerState.SetPlayerMovementState(lateralState);

            // Control Airborn State
            if (!isGrounded && _characterController.velocity.y > 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
            }
            else if (!isGrounded && _characterController.velocity.y <= 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Falling);
            }
        }

        private void HandleVerticalMovement()
        {
            bool isGrounded = _playerState.InGroundedState();

            if (isGrounded && _verticalVelocity < 0)
                _verticalVelocity = 0f;

            _verticalVelocity -= gravity * Time.deltaTime;

            if (_playerLocomotionInput.JumpPressed && isGrounded)
                _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
        }

        private void HandleLateralMovement()
        {
            // Create quick references for current state
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = _playerState.InGroundedState();

            //State dependant acceleration & speed
            float lateralAcceleration = isSprinting ? sprintAcceleration : runAcceleration;
            float clampLateralMagnitude = isSprinting ? sprintSpeed : runSpeed;

            //Ensures the player moves in the direction relative to where the camera is facing
            Vector3 cameraForwardXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized; //Forward direction of camera projected onto the horizontal plane,
                                                                                                                                        //ignoring the y component to prevent vertical movement
            Vector3 cameraRightXZ = new Vector3(_playerCamera.transform.right.x, 0f, _playerCamera.transform.right.z).normalized; //The right direction of the camera on the XZ-Plane
            Vector3 movementDirection = cameraRightXZ * _playerLocomotionInput.MovementInput.x + cameraForwardXZ * _playerLocomotionInput.MovementInput.y; //Combines the player's input with the camera's orientation to determine the actual movement direction

            //Simulate acceleration, making the player's movement feel more natural and less abrupt
            Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime; //Change in velocity for this frame, scaled by 'runAcceleration' and 'Time.deltaTime' to make it frame-rate independent
            Vector3 newVelocity = _characterController.velocity + movementDelta; //Updates the player's velocity by adding the 'movementDelta' to the current velocity

            //Add drag to player to gradually reduce the player's speed when they're not actively accelerating, simulating friction or air resistance
            Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime; //Calculates the amount of drag to apply this frame
            newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero; // Reduces the velocity by the drag amount if the current speed is greater than the drag;
                                                                                                                      // otherwise, it sets the velocity to zero to prevent it from reversing direction due to over-subtraction
            newVelocity = Vector3.ClampMagnitude(newVelocity, clampLateralMagnitude); //Clamps Velocity to Maximum Run Speed,
            newVelocity.y += _verticalVelocity;                                                      //preventing the player from exceeding the maximum allowed speed

            //Move Character (Unity suggests only calling this once per tick)
            _characterController.Move(newVelocity * Time.deltaTime);
        }
        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            HandleCameraRotation();
        }

        private void HandleCameraRotation()
        {
            //Update the rotation angles for the camera based on the player's input
            _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x; //Accumulates the horizontal look input, scaled by the horizontal look sensitivity
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV); //Accumulates the vertical look input (inverted to match typical FPS controls),
                                                                                                                                           //scaled by the vertical look sensitivity and clamps it between the
                                                                                                                                           //vertical and horizontal limits to prevent the camera from flipping over
                                                                                                                                           //Rotate the player so it faces the direction the camera is looking horizontally
            _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _playerLocomotionInput.LookInput.x;
            transform.rotation = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);

            //Apply's the calculated pitcha nd yaw to the camera, allowing it to look up and down as well as turn left and right
            _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f); //Rotation.y = (PITCH) controls the up & down look
                                                                                                           //Rotation.x = (YAW) controls the left and right look
        }
        #endregion

        #region State Checks
        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.y);

            return lateralVelocity.magnitude > movementThreshold;
        }

        private bool IsGrounded()
        {
            return _characterController.isGrounded;
        }
        #endregion
    }
}