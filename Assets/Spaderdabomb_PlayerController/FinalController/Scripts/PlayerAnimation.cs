using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spaderdabomb.PlayerController
{
    public class PlayerAnimation : MonoBehaviour
    {
        public static PlayerAnimation Instance;
        [SerializeField] private Animator _animator;
        [SerializeField] private float locomotionBlendSpeed = 0.02f;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        private PlayerController _playerController;

        private static int inputXHash = Animator.StringToHash("InputX");
        private static int inputYHash = Animator.StringToHash("InputY");
        private static int inputMagnitudeHash = Animator.StringToHash("InputMagnitude");
        private static int isIdlingHash = Animator.StringToHash("IsIdling");
        private static int isGroundedHash = Animator.StringToHash("IsGrounded");
        private static int isJumpingHash = Animator.StringToHash("IsJumping");
        private static int isFallingHash = Animator.StringToHash("IsFalling");
        private static int isRotatingToTarget = Animator.StringToHash("IsRotatingToTarget");
        private static int turnAngleHash = Animator.StringToHash("TurnAngle");

        private Vector3 _currentBlendInput = Vector3.zero;

        private float _sprintMaxBlendValue = 1.5f;
        private float _runMaxBlendValue = 1.0f;
        private float _walkMaxBlendValue = 0.5f;


        private void Awake()
        {
            if (Instance == null || Instance != this)
                Instance = this;

            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
            _playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            UpdateAnimationState();


        }

        private void UpdateAnimationState()
        {
            bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            bool isRunning = _playerState.CurrentPlayerMovementState == PlayerMovementState.Running;
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isJumping = _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping;
            bool isFalling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;
            bool isGrounded = _playerState.InGroundedState();

            bool isRunBlendValue = isRunning || isJumping || isFalling;

            Vector2 inputTarget = isSprinting ? _playerLocomotionInput.MovementInput * _sprintMaxBlendValue :
                                  isRunBlendValue ? _playerLocomotionInput.MovementInput * _runMaxBlendValue :
                                               _playerLocomotionInput.MovementInput * _walkMaxBlendValue;
            _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);

            _animator.SetBool(isFallingHash, isFalling);
            _animator.SetBool(isJumpingHash, isJumping);
            _animator.SetBool(isGroundedHash, isGrounded);
            _animator.SetBool(isIdlingHash, isIdling);
            _animator.SetBool(isRotatingToTarget, _playerController.IsRotatingToTarget);
            _animator.SetFloat(inputXHash, _currentBlendInput.x);
            _animator.SetFloat(inputYHash, _currentBlendInput.y);
            _animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);
            _animator.SetFloat(turnAngleHash, _playerController.TurnAngle);
        }

        public bool CheckCurrentAnimState(string stateName)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            bool stateMatch;

            if (stateInfo.IsName(stateName))
                stateMatch = true;
            else
                stateMatch = false;
            
            return stateMatch;

        }
    }
}
