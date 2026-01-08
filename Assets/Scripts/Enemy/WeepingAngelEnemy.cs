using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WeepingAngelEnemy : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Tooltip("Assign the player's camera")]
    [SerializeField] private Camera playerCamera;

    [Header("NavMesh Sampling")]
    [SerializeField] private float destinationSampleRadius = 2.0f;

    [Header("Movement")]
    private float baseSpeed;
    [SerializeField] private float minBaseSpeed = 15f;
    [SerializeField] private float maxBaseSpeed = 30f;

    [SerializeField] private float turnSpeed = 10000f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Awareness")]
    private float awarenessRadius;
    [SerializeField] private float minAwarenessRadius = 25f;
    [SerializeField] private float maxAwarenessRadius = 60f;

    [Header("Observation (LOS + On-screen)")]
    [SerializeField] private LayerMask observeMask = ~0;

    [Header("Return Behaviour")]
    private float returnDelay;
    [SerializeField] private float minReturnDelay = 10f;
    [SerializeField] private float maxReturnDelay = 30f;

    [SerializeField] private float returnSpeedMultiplier = 2f;
    [SerializeField] private float spawnArriveDistance = 5f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private int attackDamage;

    private Vector3 spawnPoint;
    private Vector3 lastKnownPlayerPosition;

    private bool isAware;

    // Combat
    private PlayerHealth playerHealth;
    private float lastAttackTime = -Mathf.Infinity;

    // Watch timer
    private float noTargetUnwatchedTimer;

    // Difficulty
    [SerializeField] private GameDifficulty.Difficulty difficulty;

    // States
    private enum State {Idle, Chase, FrozenNoTarget, ReturnToSpawn}
    private State state = State.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        spawnPoint = transform.position;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
            AssignDifficultyStats();

        if (playerCamera == null) 
            playerCamera = Camera.main;

        StartCoroutine(StateLoop());
    }

    private void Update()
    {
        UpdatePerception();
        UpdateMovementFacing();
        UpdateAnimation();
        TryAttackIfValid();
    }

    private void AssignDifficultyStats()
    {
        // Random stats are chosen from the lower half of the bounds
        if (difficulty == GameDifficulty.Difficulty.Story)
        {
            awarenessRadius = Random.Range(minAwarenessRadius, (minAwarenessRadius + maxAwarenessRadius) / 2f);
            returnDelay = Random.Range(minReturnDelay, (minReturnDelay + maxReturnDelay) / 2f);
            baseSpeed = Random.Range(minBaseSpeed, (minBaseSpeed + maxBaseSpeed) / 2f);
            attackDamage = 15;
        }
        // Random stats are chosen from the upper half of the bounds
        else if (difficulty == GameDifficulty.Difficulty.Challenge)
        {
            awarenessRadius = Random.Range((minAwarenessRadius + maxAwarenessRadius) / 2f, maxAwarenessRadius);
            returnDelay = Random.Range((minReturnDelay + maxReturnDelay) / 2f, maxReturnDelay);
            baseSpeed = Random.Range((minBaseSpeed + maxBaseSpeed) / 2f, maxBaseSpeed);
            attackDamage = 40;
        }
        // Random stats can be any value within the bounds
        else
        {
            awarenessRadius = Random.Range(minAwarenessRadius, maxAwarenessRadius);
            returnDelay = Random.Range(minReturnDelay, maxReturnDelay);
            baseSpeed = Random.Range(minBaseSpeed, maxBaseSpeed);
            attackDamage = 25;
        }

        agent.speed = baseSpeed;
    }

    private void UpdatePerception()
    {
        if (player == null)
        {
            isAware = false;
            return;
        }

        // Enemy is aware of the player if the player is within it's awareness radius
        float dist = Vector3.Distance(transform.position, player.position);
        isAware = dist <= awarenessRadius;

        if (isAware)
            lastKnownPlayerPosition = player.position;
    }

    // Simple state machine controls behaviour
    private IEnumerator StateLoop()
    {
        agent.updatePosition = true;
        agent.isStopped = true;
        agent.ResetPath();
        RestoreMove();

        state = State.Idle;
        noTargetUnwatchedTimer = 0f;

        while (true)
        {
            switch (state)
            {
                case State.Idle: yield return Idle(); break;
                case State.Chase: yield return Chase(); break;
                case State.FrozenNoTarget: yield return FrozenNoTarget(); break;
                case State.ReturnToSpawn: yield return ReturnToSpawn(); break;
            }
            yield return null;
        }
    }

    // Enemy is frozen at its spawn until a player is detected
    private IEnumerator Idle()
    {
        while (state == State.Idle)
        {
            FreezeNow();
            if (isAware) 
            { 
                state = State.Chase; 
                yield break; 
            }
            yield return null;
        }
    }

    // Enemy is aware of player and will pursue them when unobserved
    private IEnumerator Chase()
    {
        while (state == State.Chase)
        {
            agent.stoppingDistance = Mathf.Max(attackRange, 1f);
            if (!isAware || player == null)
            {
                FreezeNow();
                noTargetUnwatchedTimer = 0f;
                state = State.FrozenNoTarget;
                yield break;
            }

            if (IsObservedByPlayer_CameraConeLOS())
            {
                FreezeNow();
                yield return null;
                continue;
            }

            RestoreMove();
            agent.isStopped = false;
            agent.SetDestinationKeepOnNavmesh(player.position, destinationSampleRadius);
            FaceToward(player.position, turnSpeed);

            yield return null;
        }
    }

    // Enemy has lost sight of the player and so freezes in place
    private IEnumerator FrozenNoTarget()
    {
        while (state == State.FrozenNoTarget)
        {
            FreezeNow();

            if (isAware) 
            { 
                state = State.Chase; 
                yield break; 
            }

            if (!IsObservedByPlayer_CameraConeLOS())
            {
                noTargetUnwatchedTimer += Time.deltaTime;
                if (noTargetUnwatchedTimer >= returnDelay)
                {
                    state = State.ReturnToSpawn;
                    yield break;
                }
            }

            yield return null;
        }
    }

    // When unobserved for too long, return to spawn at a faster than normal speed
    private IEnumerator ReturnToSpawn()
    {
        while (state == State.ReturnToSpawn)
        {
            if (isAware) 
            { 
                RestoreMove(); 
                state = State.Chase; 
                yield break; 
            }

            if (IsObservedByPlayer_CameraConeLOS())
            {
                RestoreMove();
                FreezeNow();
                yield return null;
                continue;
            }

            ApplyReturnMoveSpeed();
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestinationKeepOnNavmesh(spawnPoint, destinationSampleRadius);

            if (Vector3.Distance(transform.position, spawnPoint) <= spawnArriveDistance)
            {
                RestoreMove();
                FreezeNow();
                state = State.Idle;
                yield break;
            }

            yield return null;
        }
    }

    // Stops the enemy from moving
    private void FreezeNow()
    {
        agent.isStopped = true;
        agent.ResetPath();

        agent.velocity = Vector3.zero;
        agent.angularSpeed = 0f;
    }

    // Makes the enemy move faster when returning to spawn
    private void ApplyReturnMoveSpeed()
    {
        agent.speed = baseSpeed * returnSpeedMultiplier;
    }

    // Allows enemy to move again
    private void RestoreMove()
    {
        agent.speed = baseSpeed;
        agent.angularSpeed = turnSpeed;
    }

    // Determines whether the player is looking at the enemy or not
    private bool IsObservedByPlayer_CameraConeLOS()
    {
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null)
        {
            return false;
        }

        Renderer r = GetComponentInChildren<Renderer>();
        if (r == null)
        {
            return false;
        }

        Bounds bounds = r.bounds;

        // Sample corners + face centers (14 points)
        Vector3[] points = new Vector3[14];
        // corners
        points[0] = bounds.min;
        points[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        points[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        points[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        points[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        points[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        points[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        points[7] = bounds.max;
        // face centers
        points[8]  = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        points[9]  = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        points[10] = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z);
        points[11] = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
        points[12] = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
        points[13] = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z);

        // Enemy is considered observed if ANY point is visible in front of the camera
        bool anyOnScreen = false;
        foreach (var point in points)
        {
            Vector3 vp = cam.WorldToViewportPoint(point);
            if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
            {
                anyOnScreen = true;
                break;
            }
        }

        if (!anyOnScreen)
        {
            return false;
        }

        // Raycast for line-of-sight
        Vector3 enemyAim = bounds.center;
        Vector3 origin = cam.transform.position;
        Vector3 toEnemy = enemyAim - origin;
        float dist = toEnemy.magnitude;

        if (dist <= 0.001f)
        {
            return true;
        }

        Vector3 dir = toEnemy / dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist + 0.05f, observeMask, QueryTriggerInteraction.Ignore))
        {
            bool seen = hit.transform.root == transform;
            return seen;
        }

        return false;
    }

    // Makes the enemy face the direction of movement
    private void UpdateMovementFacing()
    {
        if (agent.isStopped) 
            return;
        if (agent.velocity.sqrMagnitude <= 0.01f) 
            return;

        Vector3 dir = agent.velocity;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f) 
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void FaceToward(Vector3 worldPos, float degreesPerSecond)
    {
        Vector3 to = worldPos - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude <= 0.0001f) 
            return;

        Quaternion target = Quaternion.LookRotation(to.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
    }

    private void UpdateAnimation()
    {
        if (animator == null) 
            return;
    }

    // Multiple checks for whether the player can be successfully attacked. If possible, do so
    private void TryAttackIfValid()
    {
        if (player == null) 
            return;

        if (!isAware) 
            return;
        if (IsObservedByPlayer_CameraConeLOS()) 
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange) 
            return;

        if (Time.time - lastAttackTime < attackInterval) 
            return;

        if (playerHealth == null) 
            playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive) 
            return;

        playerHealth.TakeDamage(attackDamage);
        lastAttackTime = Time.time;

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending) 
            return false;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) 
            return false;
        if (!agent.hasPath) 
            return true;

        return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.1f);
    }

    private static Vector3? GetRandomNavmeshPoint(Vector3 center, float radius, int attempts, float sampleRadius)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 rnd = center + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                return hit.position;
        }
        return null;
    }
}