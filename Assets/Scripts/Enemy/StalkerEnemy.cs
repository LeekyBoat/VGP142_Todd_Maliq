using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class StalkerEnemy : MonoBehaviour
{
    [Header("Setup References")]
    [Tooltip("Assign your main player transform here.")]
    public Transform playerTransform;
    [Tooltip("The main camera the player looks through.")]
    public Camera playerCamera;

    [Header("Stalker Settings")]
    [Tooltip("How close the stalker gets before stopping to watch.")]
    public float stalkingDistance = 5f;
    [Tooltip("Speed when the player is not looking.")]
    public float stalkSpeed = 3.5f;
    [Tooltip("Speed when creeping up if player looks away for too long.")]
    public float dashSpeed = 7f;

    [Header("Detection Settings")]
    [Tooltip("Layers that block the player's view to the stalker (e.g., Default, Environment).")]
    public LayerMask visibilityBlockLayers;

    [Header("Shooting Settings")]
    [Tooltip("The projectile prefab to fire at the player. Must have a Rigidbody.")]
    public GameObject projectilePrefab;
    [Tooltip("Empty child transform marking where projectiles spawn from (e.g. a 'gun barrel' point).")]
    public Transform firePoint;
    [Tooltip("Seconds between each shot while the player is looking at the stalker.")]
    public float fireRate = 1.5f;
    [Tooltip("Speed the projectile travels at.")]
    public float projectileSpeed = 20f;
    [Tooltip("Max range the stalker will bother shooting from. Set higher than stalkingDistance if you want ranged shots.")]
    public float shootingRange = 15f;
    [Tooltip("Layers that block a clear shot to the player (usually same as visibilityBlockLayers).")]
    public LayerMask shotBlockLayers;

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

        // 1. Check if the stalker is within the player's screen view and line of sight
        isPlayerLooking = CheckIfPlayerLooking();

        // 2. Control behavior based on player visibility status
        HandleStalkingBehavior();

        // 3. Handle shooting cooldown + firing while seen
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
                // If the ray hits an environment obstacle first, the player cannot actually see the stalker
                return false;
            }

            return true; // Player is directly looking at the stalker
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
            else
            {
                // Aggressive behavior: close range rush if they continue to look away
                agent.speed = dashSpeed;
                agent.SetDestination(playerTransform.position);
            }
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

        Vector3 toPlayer = playerTransform.position - firePoint.position;
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

        Vector3 direction = (playerTransform.position - firePoint.position).normalized;
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