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

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 900f;

    [Header("Animation")]
    [SerializeField] private Animator animator;



    [Header("Weeping Angel Perception")]
    [SerializeField] private float angelAwarenessRadius = 50f;

    [Header("Weeping Angel Freeze When Observed (LOS + on-screen)")]
    [SerializeField] private LayerMask angelObserveMask = ~0;

    [Header("Weeping Angel Return Behaviour")]
    [Tooltip("If the angel is NOT watched and does NOT know where the player is, it will return to spawn after this many seconds.")]
    [SerializeField] private float angelUnwatchedReturnDelay = 20f;

    [Tooltip("Speed multiplier used only while returning to spawn (when not watched).")]
    [SerializeField] private float angelReturnSpeedMultiplier = 2f;

    [Tooltip("How close is considered 'at spawn'.")]
    [SerializeField] private float angelSpawnArriveDistance = 5f;


    [Header("Combat")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackInterval = 3f;

    private Vector3 spawnPoint;
    private Vector3 lastKnownPlayerPosition;

    // Simple awareness state
    private bool simpleAware;

    // Combat
    private PlayerHealth playerHealth;
    private float lastAttackTime = -Mathf.Infinity;

    // Angel runtime
    private float angelNoTargetUnwatchedTimer;
    private float angelBaseSpeed;
    private float angelBaseAccel;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        spawnPoint = transform.position;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerCamera == null) 
            playerCamera = Camera.main;

        angelBaseSpeed = agent.speed;
        angelBaseAccel = agent.acceleration;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
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

    private void UpdatePerception()
    {
        if (player == null)
        {
            simpleAware = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        simpleAware = dist <= angelAwarenessRadius;

        if (simpleAware)
            lastKnownPlayerPosition = player.position;
    }

    // Weeping Angel enemy behaviour
    private enum AngelState {Idle, Chase, FrozenNoTarget, ReturnToSpawn}
    private AngelState angelState = AngelState.Idle;

    private IEnumerator StateLoop()
    {
        agent.updatePosition = true;
        agent.isStopped = true;
        agent.ResetPath();
        RestoreAngelMove();

        angelState = AngelState.Idle;
        angelNoTargetUnwatchedTimer = 0f;

        while (true)
        {
            switch (angelState)
            {
                case AngelState.Idle: yield return Angel_Idle(); break;
                case AngelState.Chase: yield return Angel_Chase(); break;
                case AngelState.FrozenNoTarget: yield return Angel_FrozenNoTarget(); break;
                case AngelState.ReturnToSpawn: yield return Angel_ReturnToSpawn(); break;
            }
            yield return null;
        }
    }

    private IEnumerator Angel_Idle()
    {
        while (angelState == AngelState.Idle)
        {
            AngelFreezeNow();
            if (simpleAware) 
            { 
                angelState = AngelState.Chase; 
                yield break; 
            }
            yield return null;
        }
    }

    private IEnumerator Angel_Chase()
    {
        while (angelState == AngelState.Chase)
        {
            agent.stoppingDistance = Mathf.Max(attackRange, 1f);
            if (!simpleAware || player == null)
            {
                AngelFreezeNow();
                angelNoTargetUnwatchedTimer = 0f;
                angelState = AngelState.FrozenNoTarget;
                yield break;
            }

            if (IsObservedByPlayer_CameraConeLOS())
            {
                AngelFreezeNow();
                yield return null;
                continue;
            }

            RestoreAngelMove();
            agent.isStopped = false;
            agent.SetDestinationKeepOnNavmesh(player.position, destinationSampleRadius);
            FaceToward(player.position, turnSpeed);

            yield return null;
        }
    }

    private IEnumerator Angel_FrozenNoTarget()
    {
        while (angelState == AngelState.FrozenNoTarget)
        {
            AngelFreezeNow();

            if (simpleAware) 
            { 
                angelState = AngelState.Chase; 
                yield break; 
            }

            if (!IsObservedByPlayer_CameraConeLOS())
            {
                angelNoTargetUnwatchedTimer += Time.deltaTime;
                if (angelNoTargetUnwatchedTimer >= angelUnwatchedReturnDelay)
                {
                    angelState = AngelState.ReturnToSpawn;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator Angel_ReturnToSpawn()
    {
        while (angelState == AngelState.ReturnToSpawn)
        {
            if (simpleAware) 
            { 
                RestoreAngelMove(); 
                angelState = AngelState.Chase; 
                yield break; 
            }

            if (IsObservedByPlayer_CameraConeLOS())
            {
                RestoreAngelMove();
                AngelFreezeNow();
                yield return null;
                continue;
            }

            ApplyAngelReturnMove();
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestinationKeepOnNavmesh(spawnPoint, destinationSampleRadius);

            if (Vector3.Distance(transform.position, spawnPoint) <= angelSpawnArriveDistance)
            {
                RestoreAngelMove();
                AngelFreezeNow();
                angelState = AngelState.Idle;
                yield break;
            }

            yield return null;
        }
    }

    private void AngelFreezeNow()
    {
        agent.isStopped = true;
        agent.ResetPath();

        agent.velocity = Vector3.zero;
        agent.angularSpeed = 0f;
    }

    private void ApplyAngelReturnMove()
    {
        agent.speed = angelBaseSpeed * angelReturnSpeedMultiplier;
        agent.acceleration = angelBaseAccel * angelReturnSpeedMultiplier;
    }

    private void RestoreAngelMove()
    {
        agent.speed = angelBaseSpeed;
        agent.acceleration = angelBaseAccel;
        agent.angularSpeed = turnSpeed;
    }

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

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist + 0.05f, angelObserveMask, QueryTriggerInteraction.Ignore))
        {
            bool seen = hit.transform.root == transform;
            return seen;
        }

        return false;
    }

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

    private void TryAttackIfValid()
    {
        if (player == null) 
            return;

        if (!simpleAware) 
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

    private static float GetAlternatingYawOffset(int index, float stepAngle)
    {
        if (index == 0) 
            return 0f;
        int k = (index + 1) / 2;
        float side = (index % 2 == 1) ? 1f : -1f;
        return side * k * stepAngle;
    }
}