using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private CharacterController _characterController;
    private Animator _animator;
    private InputAction _moveAction;
    private Vector2 _moveInput;
    private InputAction _jumpAction;

    private InputAction _sprintAction;
    private InputAction _aimAction;
    private InputAction _interactAction;
    private InputAction _attackAction;
    
    [SerializeField] private float _movementSpeed = 10;
    [SerializeField] private float _jumpHeight = 2;
    [SerializeField] private float _pushForce = 5;
    [SerializeField] private float _throwForce = 20;

    private float _turnSmoothVelocity;
    [SerializeField] float _smoothTime = 1;

    // 002.3 A/D orbit (15/11): A/D = giro continuo + movimiento lateral → círculo (mantener = 360°).
    // radio del círculo = vel_lateral / vel_giro · vel_lateral = radio * omega(deg/s) / 10.
    [SerializeField] private float _strafeOmega = 90f;      // giro caminando (deg/s)
    [SerializeField] private float _strafeRadius = 1.5f;    // radio caminando (m) → vel_lateral 13.5 m/s
    [SerializeField] private float _strafeRunOmega = 60f;   // giro corriendo (deg/s)
    [SerializeField] private float _strafeRunRadius = 3f;   // radio corriendo (m, más grande) → vel_lateral 18 m/s

    private float _gravity;
    [SerializeField] private Vector3 _playerGravity;

    [SerializeField] private Transform _sensorTransform;
    [SerializeField] private float _sensorRadius;
    [SerializeField] private LayerMask _groundLayer;

    private Transform _cameraTransform;

    private GameObject _grabbedObject;
    [SerializeField] private Transform _handsTransform;
    [SerializeField] private float _handsRadius = 1;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        _moveAction = InputSystem.actions["Move"];
        _jumpAction = InputSystem.actions["Jump"];
        _sprintAction = InputSystem.actions["Sprint"];
        _aimAction = InputSystem.actions["Aim"];
        _interactAction = InputSystem.actions["Interact"];
        _attackAction = InputSystem.actions["Attack"];

        _cameraTransform = Camera.main.transform;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _gravity = Physics.gravity.y;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Debug.Log("Void Start PlayerController: Executed.");
    }

    // Update is called once per frame
    void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();

        Gravity();

        if(_jumpAction.WasPressedThisFrame() && IsGrounded())
        {
            Jump();
            Debug.Log("Void Update: JumAction was pressed this frame.");

        }

        _animator.SetBool("IsJumping", !IsGrounded());

        if(_moveAction.WasPressedThisFrame())
        {
            
            Debug.Log("Void Update: MoveAction was pressed this frame.");

        }

        bool moving  = _moveInput.magnitude > 0.01f;
        bool running = _sprintAction.IsPressed() && moving;

        _animator.SetBool("IsRunning", running);
        // IsWalking = solo W (adelante) · IsWalkingBackwards = solo S (atrás) — NUNCA los dos a la vez (máquina 002.3)
        _animator.SetBool("IsWalking", _moveInput.y > 0f);
        _animator.SetBool("IsWalkingBackwards", _moveInput.y < 0f);
        // A → WalkingLeft / A+Shift → RunningLeft · D → WalkingRight / D+Shift → RunningRight
        // (máquina 002.3: states de run = IsRunning + bool de dirección · walk = bool de dirección)
        _animator.SetBool("IsRunningLeft",  _moveInput.x < 0f);
        _animator.SetBool("IsRunningRight", _moveInput.x > 0f);
        _animator.SetBool("IsWalkingLeft",  _moveInput.x < 0f);
        _animator.SetBool("IsWalkingRight", _moveInput.x > 0f);
        
        
        int strafeDir = 0;
        if (_moveInput.x > 0.01f)       strafeDir =  1;   // D → círculo a la derecha
        else if (_moveInput.x < -0.01f) strafeDir = -1;   // A → círculo a la izquierda

        if (moving)
        {
            if (strafeDir != 0)  StrafeOrbitMovement(strafeDir, running);
            else if (running)    WalkMovement();   // W / W+Shift · S+Shift: cuerpo al frente de cámara
            else                 TPMovement();     // W: gira y camina · S: bloqueado a cámara
        }
    
        

        if(_interactAction.WasPressedThisFrame())
        {
            GrabObject();
            Debug.Log("Void Update: GrabObject was pressed this frame.");

        }    

        if(_attackAction.WasPressedThisFrame())
        {
            ThrowObject();
            Debug.Log("Void Update: ThrowObject was pressed this frame.");

        }

    }

    void TopDownMovement()
    {
        Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y);

        if(moveDirection != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _smoothTime);

            transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

            _characterController.Move(moveDirection * _movementSpeed * Time.deltaTime);
        }
        Debug.Log("Void TopDownMovement: Was executed this frame.");

    }

    void TPMovement()
    {
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y);

        _animator.SetFloat("Vertical", direction.magnitude, 0.2f, Time.deltaTime);
        _animator.SetFloat("Horizontal", 0);

        if(direction != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;

            // 002.3 (15/11): W → giro hacia donde voy y camino.
            // S → BLOQUEADO A CÁMARA (cuerpo al frente de cámara, camina hacia atrás) — el «efecto raro» era conservar la orientación.
            // OJO: el input W/S vive en direction.Z (la Y del Vector3 está hardcodeada a 0).
            float faceTarget = direction.z > 0f ? targetAngle : _cameraTransform.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, faceTarget, ref _turnSmoothVelocity, _smoothTime);

            transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

            Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            _characterController.Move(moveDirection * _movementSpeed * Time.deltaTime);
        }
     Debug.Log("Void TPMovement v3 [S bloqueado a cámara]: Was executed this frame.");
    }

    void WalkMovement()
    {
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y);

        _animator.SetFloat("Horizontal", _moveInput.x, 0.2f, Time.deltaTime);
        _animator.SetFloat("Vertical", _moveInput.y, 0.2f, Time.deltaTime);

        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, _cameraTransform.eulerAngles.y, ref _turnSmoothVelocity, _smoothTime);

        transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

        if(direction != Vector3.zero)
        {
            Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            _characterController.Move(moveDirection * _movementSpeed * Time.deltaTime);
        }
             Debug.Log("Void WalkMovement: Was executed this frame.");

    }

    // A/D: «walking turn» — giro continuo + avance HACIA DELANTE (los clips son Left/Right Turn,
    // hechos para caminar mirando hacia donde va — moverlo de lado lo dejaba mirando al exterior del círculo).
    // dir: +1 = D (giro derecha) · -1 = A (giro izquierda) · running: círculo más grande.
    void StrafeOrbitMovement(int dir, bool running)
    {
        float omega  = running ? _strafeRunOmega  : _strafeOmega;
        float radius = running ? _strafeRunRadius : _strafeRadius;

        float forwardSpeed = radius * omega * 0.1f; // radio(m) * deg/s / 10 → m/s coherente con el radio

        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + dir * omega * Time.deltaTime, 0);

        _characterController.Move(transform.forward * forwardSpeed * Time.deltaTime);

        Debug.Log("Void StrafeOrbitMovement: Was executed this frame.");
    }

    void Gravity()
    {
        if(!IsGrounded())
        {
            _playerGravity.y += _gravity * Time.deltaTime;
        }
        else if(IsGrounded() && _playerGravity.y < 0)
        {
            _playerGravity.y = _gravity;
        }

        _characterController.Move(_playerGravity * Time.deltaTime);
        Debug.Log("Void Gravity: Was executed this frame.");

    }

    void Jump()
    {
        _playerGravity.y = Mathf.Sqrt(_jumpHeight * -2 * _gravity);
        Debug.Log("Void Jump: Was executed this frame.");

    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(_sensorTransform.position, _sensorRadius, _groundLayer);
    }

/*    
     bool IsRunning()
    {
        Debug.Log("Void Jump: Was executed this frame.");
    }
    */
    void GrabObject()
    {
        if(_grabbedObject == null)
        {
            Collider[] objectsToGrab = Physics.OverlapSphere(_handsTransform.position, _handsRadius);

            foreach(Collider item in objectsToGrab)
            {
                if(item.CompareTag("Empujable"))
                {
                    _grabbedObject = item.gameObject;
                    _grabbedObject.transform.SetParent(_handsTransform);
                    _grabbedObject.transform.position = _handsTransform.position;
                    _grabbedObject.transform.rotation = _handsTransform.rotation;
                    Rigidbody objectRigidbody = _grabbedObject.GetComponent<Rigidbody>();
                    objectRigidbody.isKinematic = true;
                    objectRigidbody.constraints = RigidbodyConstraints.None;

                    return;
                }
            }
        }
        else
        {
            Rigidbody objectRigidbody = _grabbedObject.GetComponent<Rigidbody>();
            objectRigidbody.isKinematic = false;
            _grabbedObject.transform.SetParent(null);
            _grabbedObject = null;
        }

        Debug.Log("Void GrabObject: Was executed this frame.");

    }

    void ThrowObject()
    {
        if(_grabbedObject != null)
        {
            Rigidbody objectRigidbody = _grabbedObject.GetComponent<Rigidbody>();
            objectRigidbody.isKinematic = false;
            _grabbedObject.transform.SetParent(null);
            objectRigidbody.AddForce(_cameraTransform.forward * _throwForce, ForceMode.Impulse);
            _grabbedObject = null;
        }

        Debug.Log("Void ThrowObject: Was executed this frame.");

    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.gameObject.CompareTag("Empujable"))
        {
            Rigidbody _rbody = hit.gameObject.GetComponent<Rigidbody>();

            Vector3 pushDirection =  hit.moveDirection;
            pushDirection.y = 0;

            Debug.Log(pushDirection);

            //_rbody.AddForce(pushDirection * _pushForce, ForceMode.Force);
            _rbody.linearVelocity = pushDirection * _pushForce / _rbody.mass;
        }

        if(hit.gameObject.CompareTag("Enemy"))
        {


            Enemy enemyScript = hit.gameObject.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage();
                return;
            }

        }

        IDamageable damageable = hit.gameObject.GetComponent<IDamageable>();
        if(damageable != null)
        {
            damageable.TakeDamage();
        }

      /*  Ejemplo antiguo:
      
      if (hit.gameObject.CompareTag("Caja"))
        {
            Caja cahaScript = hit.gameObject.GetComponent<Caja>();
            cajaScript.TakeDama();
        } 
        
        */
        Debug.Log("Void OnControllerColliderHit: Was executed this frame.");

    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_sensorTransform.position, _sensorRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_handsTransform.position, _handsRadius);
        Debug.Log("Void OnDrawGizmos: Was executed this frame.");

    }
}