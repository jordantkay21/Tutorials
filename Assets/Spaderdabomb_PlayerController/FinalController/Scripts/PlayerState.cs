using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spaderdabomb.PlayerController
{
    public enum PlayerMovementState
    {
        Idling = 0,
        Walking = 1,
        Running = 2,
        Sprinting = 3,
        Jumping = 4,
        Falling = 5,
        Strafing = 6,
    }

    public class PlayerState : MonoBehaviour
    {
        [field: SerializeField] public PlayerMovementState CurrentPlayerMovementState { get; private set; }

        public void SetPlayerMovementState(PlayerMovementState playerMovementState)
        {
            CurrentPlayerMovementState = playerMovementState;
        }

        public bool InGroundedState()
        {
            return CurrentPlayerMovementState == PlayerMovementState.Idling ||
                CurrentPlayerMovementState == PlayerMovementState.Walking ||
                CurrentPlayerMovementState == PlayerMovementState.Running ||
                CurrentPlayerMovementState == PlayerMovementState.Sprinting;
        }
    }
}

