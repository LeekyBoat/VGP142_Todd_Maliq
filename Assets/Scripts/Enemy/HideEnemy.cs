using UnityEngine;
using UnityEngine.AI;
using System.Collections;

//Enemystalker · CS;
 
/// <summary>
/// Enemy AI that sneaks toward the player when unseen, and freezes + becomes
/// visible the instant the player looks at it (Weeping Angel style).
///
/// SETUP:
/// 1. Bake a NavMesh for your level (Window > AI > Navigation).
/// 2. Attach this script to your enemy GameObject.
/// 3. Enemy needs a NavMeshAgent component (auto-added via RequireComponent).
/// 4. Assign the "playerCamera" field to the player's main camera (used for
///    determining what the player is looking at).
/// 5. Assign "playerTransform" to the player's root transform (used for
///    movement targeting / distance checks).
/// 6. Assign "meshRenderers" to the enemy's renderer(s) so it can be hidden.
/// 7. Set the "obstacleMask" to whatever layers should block line-of-sight
///    (walls, props, etc). Do NOT include the Player or Enemy layers in it.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyStalker : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform playerTransform;
    public Renderer[] meshRenderers; // all renderers to hide/show (mesh, materials etc.)

    [Header("Detection")]
    [Tooltip("Half-angle of the player's vision cone, in degrees.")]
    public float viewConeAngle = 30f;
    [Tooltip("Max distance at which the player can spot the enemy.")]
    public float viewDistance = 25f;
    [Tooltip("Layers that block line-of-sight between player and enemy.")]
    public LayerMask obstacleMask;
    [Tooltip("Extra buffer so line-of-sight raycast doesn't clip the enemy's own collider.")]
    public float eyeHeightOffset = 1.6f;

    [Header("Movement")]
    public float stalkSpeed = 3.5f;
    public float patrolSpeed = 1.5f;
    public float patrolRadius = 15f;
    public float minPatrolWaitTime = 2f;
    public float maxPatrolWaitTime = 5f;
    [Tooltip("Distance at which the enemy stops approaching (e.g. attack range).")]
    public float stopDistance = 1.5f;

    [Header("Visibility Fade")]
    public float fadeDuration = 0.5f;

    private NavMeshAgent agent;
    private bool isSeen = false;
    private bool isVisible = false;
    private Coroutine fadeRoutine;
    private Coroutine patrolRoutine;

    private enum State { Patrol, Stalk }
    private State currentState = State.Patrol;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        SetVisible(false, instant: true);
    }

    void Start()
    {
        patrolRoutine = StartCoroutine(PatrolLoop());
    }

    void Update()
    {
        bool seenNow = IsSeenByPlayer();

        if (seenNow != isSeen)
        {
            isSeen = seenNow;
            OnSeenStateChanged(isSeen);
        }

        if (isSeen)
        {
            // Frozen: do nothing, agent already stopped in OnSeenStateChanged.
            return;
        }

        // Decide behavior when unseen: stalk if player is within range, else patrol.
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (currentState != State.Stalk && distToPlayer <= viewDistance * 1.5f)
        {
            EnterStalkState();
        }
        else if (currentState == State.Stalk)
        {
            StalkUpdate(distToPlayer);
        }
    }

    // ---------------- DETECTION ----------------

    private bool IsSeenByPlayer()
    {
        if (playerCamera == null || playerTransform == null) return false;

        Vector3 eyePos = playerCamera.transform.position;
        Vector3 targetPos = transform.position + Vector3.up * (eyeHeightOffset * 0.5f);
        Vector3 toEnemy = targetPos - eyePos;
        float distance = toEnemy.magnitude;

        if (distance > viewDistance) return false;

        // Angle check (is enemy within the player's view cone?)
        float angle = Vector3.Angle(playerCamera.transform.forward, toEnemy);
        if (angle > viewConeAngle) return false;

        // Line-of-sight check (raycast for obstacles between player and enemy)
        if (Physics.Raycast(eyePos, toEnemy.normalized, out RaycastHit hit, distance, obstacleMask))
        {
            // Something (a wall, prop, etc.) is blocking the view.
            return false;
        }

        return true;
    }

    private void OnSeenStateChanged(bool seen)
    {
        if (seen)
        {
            // FREEZE + become visible
            if (patrolRoutine != null) StopCoroutine(patrolRoutine);
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            SetVisible(true);
        }
        else
        {
            // Resume sneaking + become invisible
            agent.isStopped = false;
            SetVisible(false);

            if (currentState == State.Patrol)
            {
                patrolRoutine = StartCoroutine(PatrolLoop());
            }
        }
    }

    // ---------------- VISIBILITY ----------------

    private void SetVisible(bool visible, bool instant = false)
    {
        isVisible = visible;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (instant)
        {
            SetAlphaImmediate(visible ? 1f : 0f);
            foreach (var r in meshRenderers) r.enabled = visible;
            return;
        }

        fadeRoutine = StartCoroutine(FadeTo(visible));
    }

    private IEnumerator FadeTo(bool visible)
    {
        if (visible)
        {
            foreach (var r in meshRenderers) r.enabled = true;
        }

        float start = visible ? 0f : 1f;
        float end = visible ? 1f : 0f;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(start, end, t / fadeDuration);
            SetAlphaImmediate(alpha);
            yield return null;
        }

        SetAlphaImmediate(end);

        if (!visible)
        {
            foreach (var r in meshRenderers) r.enabled = false;
        }
    }

    private void SetAlphaImmediate(float alpha)
    {
        foreach (var r in meshRenderers)
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                // For URP/HDRP Lit shaders you may need "_BaseColor" instead,
                // and the material's Surface Type set to Transparent.
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

    // ---------------- STALK STATE ----------------

    private void EnterStalkState()
    {
        currentState = State.Stalk;
        agent.speed = stalkSpeed;
        agent.stoppingDistance = stopDistance;
    }

    private void StalkUpdate(float distToPlayer)
    {
        agent.SetDestination(playerTransform.position);

        // If player wanders far enough away, drop back into patrol.
        if (distToPlayer > viewDistance * 2f)
        {
            currentState = State.Patrol;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 0f;
            patrolRoutine = StartCoroutine(PatrolLoop());
        }
    }

    // ---------------- PATROL STATE ----------------

    private IEnumerator PatrolLoop()
    {
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0f;

        while (currentState == State.Patrol)
        {
            Vector3 randomPoint = GetRandomPatrolPoint();
            agent.SetDestination(randomPoint);

            // Wait until we arrive (or get interrupted by being seen/switching state)
            while (!isSeen && currentState == State.Patrol &&
                   agent.pathPending == false && agent.remainingDistance > agent.stoppingDistance + 0.1f)
            {
                yield return null;
            }

            if (isSeen || currentState != State.Patrol) yield break;

            float wait = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
            yield return new WaitForSeconds(wait);
        }
    }

    private Vector3 GetRandomPatrolPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return transform.position; // fallback: stay put
    }

    // ---------------- GIZMOS (editor only, helps you tune values) ----------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }
}
