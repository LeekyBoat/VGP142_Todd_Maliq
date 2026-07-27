using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class StalkerEnemy : MonoBehaviour
{
    [Header("Setup References")]
    public Transform playerTransform;
    public Camera playerCamera;

    [Header("Stalker Settings")]
    public float stalkingDistance = 5f;
    public float stalkSpeed = 3.5f;
    //public float dashSpeed = 7f;

    [Header("Detection Settings")]
    public LayerMask visibilityBlockLayers;

    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 1.5f;
    public float projectileSpeed = 20f;
    public float shootingRange = 15f;
    public LayerMask shotBlockLayers;
    //[Tooltip("Vertical offset added to the player's position when aiming, so shots target chest/head height instead of their feet.")]
    public float aimHeightOffset = 1.2f;

    private NavMeshAgent agent;
    private bool isPlayerLooking;
    private float fireCooldownTimer = 0f;

    void Start()
    {
        // Get the NavMeshAgent component automatically
        agent = GetComponent<NavMeshAgent>();
        agent.speed = stalkSpeed;

        // Auto-locate player if not assigned
        if (playerTransform == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (playerTransform == null || playerCamera == null) return;

        isPlayerLooking = CheckIfPlayerLooking();

        HandleStalkingBehavior();

        HandleShooting();
    }

    bool CheckIfPlayerLooking()
    {
        // Convert stalker position to player screen space
        Vector3 screenPoint = playerCamera.WorldToViewportPoint(transform.position);

        // Check if the object is physically within the camera's view frustum boundary
        bool inFrustum = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        if (inFrustum)
        {
            // Use a Raycast to ensure there isn't a wall or obstacle blocking the player's vision
            Vector3 directionToStalker = (transform.position - playerCamera.transform.position).normalized;
            float distanceToStalker = Vector3.Distance(playerCamera.transform.position, transform.position);

            if (Physics.Raycast(playerCamera.transform.position, directionToStalker, out RaycastHit hit, distanceToStalker, visibilityBlockLayers))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    void HandleStalkingBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (isPlayerLooking)
        {
            // Freeze completely if caught looking
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            // Face the player while frozen so shots aim correctly.
            FacePlayer();
        }
        else
        {
            // Unfreeze and pursue if the player turns their back
            agent.isStopped = false;

            if (distanceToPlayer > stalkingDistance)
            {
                // Player is far away; stalk them normally
                agent.speed = stalkSpeed;
                agent.SetDestination(playerTransform.position);
            }
            //else
            //{
            //    // Aggressive behavior: close range rush if they continue to look away
            //    agent.speed = dashSpeed;
            //    agent.SetDestination(playerTransform.position);
            //}
        }
    }

    void HandleShooting()
    {
        // Tick the cooldown down regardless of state.
        if (fireCooldownTimer > 0f)
        {
            fireCooldownTimer -= Time.deltaTime;
        }

        if (!isPlayerLooking) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > shootingRange) return;

        if (fireCooldownTimer <= 0f && HasClearShotToPlayer())
        {
            Shoot();
            fireCooldownTimer = fireRate;
        }
    }

    bool HasClearShotToPlayer()
    {
        if (firePoint == null) return false;

        Vector3 aimPoint = playerTransform.position + Vector3.up * aimHeightOffset;
        Vector3 toPlayer = aimPoint - firePoint.position;
        float distance = toPlayer.magnitude;

        // If something (a wall, prop) blocks the straight line to the player, don't fire.
        if (Physics.Raycast(firePoint.position, toPlayer.normalized, out RaycastHit hit, distance, shotBlockLayers))
        {
            return false;
        }

        return true;
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 aimPoint = playerTransform.position + Vector3.up * aimHeightOffset;
        Vector3 direction = (aimPoint - firePoint.position).normalized;
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed; // use rb.velocity on older Unity versions
        }
    }

    void FacePlayer()
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f; // keep the stalker upright, don't tilt up/down

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}