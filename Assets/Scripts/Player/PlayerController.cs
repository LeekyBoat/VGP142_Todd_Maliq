using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float timeToJumpApex = 0.4f;

    [Header("Aim Settings")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private LayerMask groundMask;

    private float gravity;
    private float initalJumpVelocity;

    CharacterController cc;
    Camera cam;

    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;
    private Vector3 velocity;
    private bool jumpPressed = false;


    void CalculateJumpVariables()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        initalJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    private void OnValidate()
    {
        CalculateJumpVariables();
    }

    void Start()
    {
        CalculateJumpVariables();
        cc = GetComponent<CharacterController>();
        cam = Camera.main;

        InputManager.instance.OnMoveEvent += (vector) => moveInput = vector;
        InputManager.instance.OnJumpEvent += (pressed) => jumpPressed = pressed;
        InputManager.instance.OnLookEvent += (vector) => lookInput = vector;
    }

    void Update()
    {
        RotateTowardsMouse();
    }
    void FixedUpdate()
    {
        // Find the projected move direction based on the camera orientation

        UpdateCharacterVelocity();
        cc.Move(velocity * Time.fixedDeltaTime);

    }

    //private Vector3 ProjectedMoveDirection()
    //{
    //    Vector3 forward = cam.transform.forward;
    //    forward.y = 0f;
    //    forward.Normalize();

    //    Vector3 right = cam.transform.right;
    //    right.y = 0f;
    //    right.Normalize();

    //    return forward * moveInput.y + right * moveInput.x;
    //}

    //private void UpdateCharacterVelocity(Vector3 ProjectedMoveDirection)
    //{
    //    velocity.x = ProjectedMoveDirection.x * speed;
    //    velocity.z = ProjectedMoveDirection.z * speed;

    //    if (cc.isGrounded)
    //    {
    //        velocity.y = cc.skinWidth;
    //        if (jumpPressed)
    //        {
    //            velocity.y = initalJumpVelocity;
    //        }
    //    }
    //    else
    //    {
    //        velocity.y += gravity * Time.fixedDeltaTime;
    //    }
    //}

    //private void UpdateCharacterRotation(Vector3 ProjectedMoveDirection)
    //{
    //    if (forward.sqrMagnitude > 0.001f)
    //    {
    //        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized);
    //        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    //    }
    //}
    private void UpdateCharacterVelocity()
    {
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveInput.y) + (camRight * moveInput.x);

        velocity.x = moveDirection.x * speed;
        velocity.z = moveDirection.z * speed;

        if (cc.isGrounded)
        {
            velocity.y = cc.skinWidth;
            if (jumpPressed)
            {
                velocity.y = initalJumpVelocity;
            }
        }
        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }
    }

    private void RotateTowardsMouse()
    {
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        //camForward.Normalize();

        //Vector3 right = cam.transform.right;
        //right.y = 0f;
        //right.Normalize();

        if (camForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.blue);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Win"))
        {
            SceneManager.LoadScene("GameEnd");
        }
        if (other.CompareTag("Projectile"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
