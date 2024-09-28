using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace Spaderdabomb.PlayerController
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        #region Debug Variables

        [Header("Visual Cross Product")]
        public Transform CharacterForwardViz;
        public Transform CameraForwardViz;
        public Transform CrossProductViz;
        public Transform CharacterUpViz;
        public Transform NegCharacterUpViz;
        public Transform OriginViz;
        public GameObject PlayerCam_ParallelogramViz;
        public GameObject CrossPlayer_ParallelogramViz;
        public GameObject CrossCam_ParallelogramViz;

        public Color PositiveCrossColor;
        public Color NegativeCrossColor;

        #endregion

        #region Class Variables
        [Header("Components")]
        [SerializeField] CharacterController _characterController;
        [SerializeField] Camera _playerCamera;

        public float TurnAngle { get; private set; } = 0f;
        public bool IsRotatingToTarget { get; private set; } = false;

        [Header("Base Movement")]
        public float walkAcceleration = 0.15f;
        public float walkSpeed = 3f;
        public float runAcceleration = 0.25f;
        public float runSpeed = 6f;
        public float sprintAcceleration = 0.5f;
        public float sprintSpeed = 9;
        public float drag = 0.1f;
        public float gravity;
        public float jumpSpeed;
        public float movementThreshold = 0.01f;

        [Header("Animation")]
        public float playerModelRotationSpeed = 10f;
        public float rotateToTargetTime = 0.25f;

        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 89f;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;

        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;

        private bool _isRotatingClockwise = false;
        private float _rotatingToTargetTimer = 0f;
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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            UpdateMovementState();
            HandleVerticalMovement();
            HandleLateralMovement();
        }

        private void UpdateMovementState()
        {
            bool canRun = CanRun();
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;            //order
            bool isMovingLaterally = IsMovingLaterally();                                           //matter
            bool isSprinting = _playerLocomotionInput.SprintToggleOn && isMovingLaterally;          //order
            bool isWalking = (isMovingLaterally && !canRun) || _playerLocomotionInput.WalkToggleOn; //matters
            bool isGrounded = IsGrounded();

            PlayerMovementState lateralState =  isWalking ? PlayerMovementState.Walking :
                                                isSprinting ? PlayerMovementState.Sprinting :
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
            bool isWalking = _playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;
            bool isGrounded = _playerState.InGroundedState();

            //State dependant acceleration & speed
            float lateralAcceleration = isWalking ? walkAcceleration :
                                        isSprinting ? sprintAcceleration : runAcceleration;
            float clampLateralMagnitude = isWalking ? walkSpeed : 
                                          isSprinting ? sprintSpeed : runSpeed;

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
            CinemachineVirtualCamera virtualCamera = _playerCamera.GetComponentInChildren<CinemachineVirtualCamera>();
            //float fov = virtualCamera.m_Lens.FieldOfView;
            //Update the rotation angles for the camera based on the player's input
            _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x; //Accumulates the horizontal look input, scaled by the horizontal look sensitivity

            if (_playerState.CurrentPlayerMovementState != PlayerMovementState.Sprinting)
            {
                _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);
                virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, 75, .1f);
            }
            else
            {
                _cameraRotation.y = Mathf.Lerp(_cameraRotation.y, 0f, .1f);
                virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, 100, .1f);
            }
            
                                                                                                                                           
            _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _playerLocomotionInput.LookInput.x;
 
            float rotationTolerance = 90f;
            bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            IsRotatingToTarget = _rotatingToTargetTimer > 0;

            //ROTATE IF WE ARE NOT IDLING
            if (!isIdling)
            {
                playerModelRotationSpeed = 10f;
                RotatePlayerToTarget();
            }
            //IF TURN ANGLE IS NOT WITHIN TOLERANCE, OR ROTATE TO TARGET IS ACTIVE, ROTATE...
            else if (!isIdling || Mathf.Abs(TurnAngle) > rotationTolerance || IsRotatingToTarget)
            {
                playerModelRotationSpeed = 3f;
                UpdateIdleRotation(rotationTolerance);
            }


            //Apply's the calculated pitch and yaw to the camera, allowing it to look up and down as well as turn left and right
            _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f); //Rotation.y = (PITCH) controls the up & down look
                                                                                                                   //Rotation.x = (YAW) controls the left and right look

            Vector3 camForwardProjectedXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;

            Vector3 crossProduct = Vector3.Cross(transform.forward, camForwardProjectedXZ);

            float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
            
            TurnAngle = sign * Vector3.Angle(transform.forward, camForwardProjectedXZ);

            Color parallelogramColor = (sign >= 0) ? PositiveCrossColor : NegativeCrossColor;

            CameraForwardViz.position = camForwardProjectedXZ;
            CharacterForwardViz.position = transform.forward;
            CrossProductViz.position = crossProduct;
            CharacterUpViz.position = transform.up;
            NegCharacterUpViz.position = -transform.up;

            VisualizeVector(CameraForwardViz.gameObject.GetComponent<LineRenderer>(), OriginViz.position, camForwardProjectedXZ, Color.blue);
            VisualizeVector(CharacterForwardViz.gameObject.GetComponent<LineRenderer>(), OriginViz.position, transform.forward, Color.green);
            VisualizeVector(CrossProductViz.gameObject.GetComponent<LineRenderer>(), OriginViz.position, crossProduct, Color.red);

            // Visualize parallelogram
            DrawParallelogram(OriginViz.position, transform.forward, camForwardProjectedXZ, parallelogramColor, sign, PlayerCam_ParallelogramViz: PlayerCam_ParallelogramViz);
            DrawParallelogram(OriginViz.position, crossProduct, transform.forward, parallelogramColor, sign, CrossPlayer_ParallelogramViz: CrossPlayer_ParallelogramViz);
            DrawParallelogram(OriginViz.position, crossProduct, camForwardProjectedXZ, parallelogramColor, sign, CrossCam_ParallelogramViz: CrossCam_ParallelogramViz);
        }

        private void RotatePlayerToTarget()
        {
            Quaternion targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);
        }

        private void UpdateIdleRotation(float rotationTolerance)
        {
            //Initiate new rotation direction
            if (Mathf.Abs(TurnAngle) > rotationTolerance)
            {
                _rotatingToTargetTimer = rotateToTargetTime;
            }
            _rotatingToTargetTimer -= Time.deltaTime;

            RotatePlayerToTarget();


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

        private bool CanRun()
        { 
            //This means player is moving diagonally at 45 degrees or forward, if so, character can run
            return _playerLocomotionInput.MovementInput.y >= Mathf.Abs(_playerLocomotionInput.MovementInput.x);
        }
        #endregion

        #region Visual Helpers
        void VisualizeVector(LineRenderer lr, Vector3 origin, Vector3 direction, Color color)
        {
            lr.startColor = color;
            lr.endColor = color;
            lr.SetPosition(0, origin);
            lr.SetPosition(1, origin + direction);
            lr.startWidth = .03f;
            lr.endWidth = .03f;
        }
        void DrawParallelogram(Vector3 origin, Vector3 vecA, Vector3 vecB, Color parallelogramColor, float sign, GameObject PlayerCam_ParallelogramViz = null, GameObject CrossPlayer_ParallelogramViz = null, GameObject CrossCam_ParallelogramViz = null)
        {
            // Define the four corners of the parallelogram
            Vector3[] vertices = new Vector3[4];
            vertices[0] = origin;
            vertices[1] = origin + vecA;
            vertices[2] = origin + vecA + vecB;
            vertices[3] = origin + vecB;

            // Create the parallelogram mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;

            int[] triangles;
            if(sign >= 0)
            {
                // Original winding order
                triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
            }
            else
            {
                //Reverse the winding order
                triangles = new int[6] { 0, 2, 1, 0, 3, 2 };
            }
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            GameObject targetParallelogramViz = null;

            if (CrossCam_ParallelogramViz != null)
                targetParallelogramViz = CrossCam_ParallelogramViz;
            else if (CrossPlayer_ParallelogramViz != null)
                targetParallelogramViz = CrossPlayer_ParallelogramViz;
            else
                targetParallelogramViz = PlayerCam_ParallelogramViz;

            if (targetParallelogramViz == null)
            {
                Debug.LogError("No ParallelogramViz GameObject assigned.");
                return;
            }

            // Update or add MeshFilter and MeshRenderer
            MeshFilter mf = targetParallelogramViz.GetComponent<MeshFilter>();
            if (mf == null)
            { 
                mf = targetParallelogramViz.AddComponent<MeshFilter>(); 
            }

            mf.mesh = mesh;

            MeshRenderer mr = targetParallelogramViz.GetComponent<MeshRenderer>();
            if (mr == null)
                mr = targetParallelogramViz.AddComponent<MeshRenderer>();

            // Set material and color
            if (mr.material == null)
            {
                mr.material = new Material(Shader.Find("Standard"));
            }
            
            mr.material.color = parallelogramColor;
        }
        #endregion
    }
}