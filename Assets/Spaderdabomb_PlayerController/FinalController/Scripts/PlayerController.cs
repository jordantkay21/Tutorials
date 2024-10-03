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

        [Header("Debug Visual Cross Product")]
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

        [Header("Debug Ground Detection")]
        public GameObject FrontHitViz;
        public GameObject CenterHitViz;
        public GameObject RearHitViz;
        public GameObject MissingVectorViz;
        public GameObject StepCheckViz;

        #endregion

        #region Class Variables
        [Header("Components")]
        [SerializeField] CharacterController _characterController;
        [SerializeField] Camera _playerCamera;
        [SerializeField] Animator _animator;

        public float TurnAngle { get; private set; } = 0f;
        public bool IsRotatingToTarget { get; private set; } = false;
        [Header("Expression Objects")]
        public bool showEdgeDetection;
        public GameObject exclamationIcon;

        [Header("Base Movement")]
        public float movementThreshold = 0.01f;
        public float inAirAcceleration = 0.15f;
        public float walkAcceleration = 0.15f;
        public float walkSpeed = 3f;
        public float runAcceleration = 0.25f;
        public float runSpeed = 6f;
        public float sprintAcceleration = 0.5f;
        public float sprintSpeed = 9;
        public float drag = 0.1f;

        [Header("Jump Settings")]
        public float jumpSpeed;
        public float jumpCooldown = 0.5f; //Cooldown time in seconds between jumps
        public float jumpTimer = 0f; //Tracks the remaining cooldown time

        [Header("Fall Settings")]
        public float gravity;
        public float terminalVelocity = 50f;
        public float fallDuration;

        [Header("Animation")]
        public float playerModelRotationSpeed = 10f;
        public float rotateToTargetTime = 0.25f;

        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 89f;

        [Header("Environmental Details")]
        public LayerMask _groundLayers = default;
        public float groundOffset;
        public float raycastDistance;
        public Transform frontRayPos;
        public Transform rearRayPos;
        public Transform centerRayPos;
        public Transform StepCheckPos;


        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;

        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;

        [Header("Debug Values")]
        private bool _jumpedLastFrame;
        private float _rotatingToTargetTimer = 0f;
        private float _verticalVelocity = 0f;
        [SerializeField]  float _antiBump;
        [SerializeField]  float _stepOffset;
        [SerializeField] bool isOnEdge;
        [SerializeField] bool isSteepEdge;


        private PlayerMovementState _lastMovementState = PlayerMovementState.Falling;
        
        #endregion

        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();

            _antiBump = sprintSpeed;
            _stepOffset = _characterController.stepOffset;
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (jumpTimer > 0)
            {
                jumpTimer -= Time.deltaTime;
            }

            EdgeDetection();

            UpdateMovementState();
            HandleVerticalMovement();
            HandleLateralMovement();


            if (PlayerAnimation.Instance.CheckCurrentAnimState("Fall Blend"))
                fallDuration += Time.deltaTime;
            else
                fallDuration = 0;

        }
        private void EdgeDetection()
        {
            float totalRaycastDistance = groundOffset + 0.8f;

            RaycastHit frontHit;
            RaycastHit centerHit;
            RaycastHit stepHit;

            bool frontGrounded = Physics.Raycast(frontRayPos.position, Vector3.down, out frontHit, totalRaycastDistance, _groundLayers, QueryTriggerInteraction.Ignore);
            bool centerGrounded = Physics.Raycast(centerRayPos.position, Vector3.down, out centerHit, totalRaycastDistance, _groundLayers, QueryTriggerInteraction.Ignore);
            bool isEdgeAStep = Physics.Raycast(StepCheckPos.position, Vector3.down, out stepHit, totalRaycastDistance, _groundLayers, QueryTriggerInteraction.Ignore);

            // Visualize the raycasts
            Debug.DrawRay(frontRayPos.position, Vector3.down * totalRaycastDistance, frontGrounded ? Color.green : Color.red);
            Debug.DrawRay(centerRayPos.position, Vector3.down * totalRaycastDistance, centerGrounded ? Color.green : Color.red);
            Debug.DrawRay(StepCheckPos.position, Vector3.down * totalRaycastDistance, isEdgeAStep ? Color.green : Color.red);


            Vector3 frontPoint = frontHit.point;
            Vector3 centerPoint = centerHit.point;
            Vector3 stepCheckPoint = stepHit.point;
            Vector3 missingPoint = new Vector3(frontPoint.x, centerPoint.y, frontPoint.z);

            FrontHitViz.transform.position = frontPoint;
            CenterHitViz.transform.position = centerPoint;
            MissingVectorViz.transform.position = missingPoint;
            StepCheckViz.transform.position = stepCheckPoint;


            Debug.DrawLine(missingPoint, centerPoint, Color.yellow);
            Debug.DrawLine(missingPoint, frontPoint, Color.yellow);

            Vector3 frontVector = frontPoint - centerPoint;
            Vector3 missingVector = missingPoint - centerPoint;

            float slopeAngle = Vector3.Angle(frontVector, missingVector);

            float HeightCheckThreshold = 0.1f;

            float frontHeightDifference = Mathf.Abs(frontPoint.y - centerPoint.y);
            bool frontCenterMatch = frontHeightDifference <= HeightCheckThreshold;
            
            float stepCheckHeightDifference = Mathf.Abs(frontPoint.y - stepCheckPoint.y);
            bool isStep = stepCheckHeightDifference <= HeightCheckThreshold;

            if (!centerGrounded)
            {
                isOnEdge = false;
            }
            else
            {
                if (!frontGrounded)
                    isOnEdge = true;
                else
                {
                    if (!frontCenterMatch)
                        isOnEdge = !isStep && slopeAngle > _characterController.slopeLimit;
                    else
                        isOnEdge = false;
                }
            }

            if(showEdgeDetection)
            {
                exclamationIcon.SetActive(isOnEdge);
            }
        }

        private void UpdateMovementState()
        {
            _lastMovementState = _playerState.CurrentPlayerMovementState;

            // If currently in EdgeBalancing state, don't change state
            if (_playerState.CurrentPlayerMovementState == PlayerMovementState.EdgeBalancing)
            {
                return;
            }

            bool canRun = CanRun();
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;            //order
            bool isMovingLaterally = IsMovingLaterally();                                           //matter
            bool isSprinting = _playerLocomotionInput.SprintToggleOn && isMovingLaterally;          //order
            bool isWalking = (isMovingLaterally && !canRun) || _playerLocomotionInput.WalkToggleOn; //matters
            bool isGrounded = IsGrounded();

            PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking :
                                                isSprinting ? PlayerMovementState.Sprinting :
                                                isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;

            _playerState.SetPlayerMovementState(lateralState);

            // Control Airborn State
            if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y > 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y <= 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else
            {
                _characterController.stepOffset = _stepOffset;
            }
        }

        private void HandleVerticalMovement()
        {
            bool isGrounded = _playerState.InGroundedState();

            _verticalVelocity -= gravity * Time.deltaTime;

            if (isGrounded && _verticalVelocity < 0)
                _verticalVelocity = -_antiBump;


            if (_playerLocomotionInput.JumpPressed && isGrounded && jumpTimer <= 0f)
            {
                _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
                _jumpedLastFrame = true;

                //Reset the jump cooldown timer
                jumpTimer = jumpCooldown;
            }

            if (_playerState.IsStateGroundedState(_lastMovementState) && !isGrounded)
            {
                _verticalVelocity += _antiBump;
            }

            if(Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity))
            {
                _verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
            }
        }

        private void HandleLateralMovement()
        {
            // Create quick references for current state
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isWalking = _playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;
            bool isGrounded = _playerState.InGroundedState();

            //State dependant acceleration & speed
            float lateralAcceleration = !isGrounded ? inAirAcceleration :
                                        isWalking ? walkAcceleration :
                                        isSprinting ? sprintAcceleration : runAcceleration;

            float clampLateralMagnitude = !isGrounded ? sprintSpeed :
                                          isWalking ? walkSpeed :
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
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampLateralMagnitude); //Clamps Velocity to Maximum Run Speed,
            newVelocity.y += _verticalVelocity;                                                      //preventing the player from exceeding the maximum allowed speed
            newVelocity = !isGrounded ? HandleSteepSlopes(newVelocity) : newVelocity;



            //Move Character (Unity suggests only calling this once per tick)
            _characterController.Move(newVelocity * Time.deltaTime);
        }

        #region Helper Methods
        private bool IsValidSlopeAngle(out Vector3 normal)
        {
            normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= _characterController.slopeLimit;

            
            return validAngle;
        }

        private Vector3 HandleSteepSlopes(Vector3 velocity)
        { 
            if (!IsValidSlopeAngle(out Vector3 normal) && _verticalVelocity < 0f)
            {
                velocity = Vector3.ProjectOnPlane(velocity, normal);
            }

            return velocity;
        }
        #endregion
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
            bool grounded = _playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();

            return grounded;
        }

        private Vector3 spherePos;
        private bool IsGroundedWhileGrounded()
        {
            spherePos = new Vector3(transform.position.x, transform.position.y - groundOffset, transform.position.z);

            bool grounded = Physics.CheckSphere(spherePos, _characterController.radius, _groundLayers, QueryTriggerInteraction.Ignore);

            if (grounded)
            {
                if (IsValidSlopeAngle(out Vector3 normal))
                {
                    //Debug.Log($"Valid Slope = true | Angle ={Vector3.Angle(normal, Vector3.up)}");
                    return true;
                }
                else
                {
                    //check if within step offset (e.g. stairs)
                    float maxStepHeight = _characterController.stepOffset + _characterController.skinWidth;
                    Debug.DrawRay(transform.position, Vector3.down);
                    if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxStepHeight, _groundLayers, QueryTriggerInteraction.Ignore))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsGroundedWhileAirborne()
        {
            return _characterController.isGrounded && IsValidSlopeAngle(out Vector3 normal);
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGroundedWhileGrounded() ? Color.green : Color.red;

            //Gizmos.DrawSphere(spherePos, _characterController.radius);
        }
        #endregion
    }
}