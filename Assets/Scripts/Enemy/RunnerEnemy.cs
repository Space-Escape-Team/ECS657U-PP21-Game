using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RunnerEnemy : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 50f;
    [SerializeField] private float wanderInterval = 15f;
    [SerializeField] private float dwellMin = 5f;
    [SerializeField] private float dwellMax = 10f;

    [Header("NavMesh Sampling")]
    [SerializeField] private float destinationSampleRadius = 2.0f;
    [SerializeField] private float wanderSampleRadius = 10f;

    [Header("Speed")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 900f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Perception")]
    [SerializeField] private float sightDistance = 40f;
    [SerializeField, Range(0f, 180f)] private float fieldOfView = 110f;
    [SerializeField] private float trackTime = 5f;
    [SerializeField] private float forgetTime = 15f;
    [SerializeField] private float closeRetentionRadius = 10f;
    [SerializeField] private LayerMask visionMask = ~0;
    [SerializeField] private float eyeHeight = 1.0f;
    [SerializeField] private float playerAimHeight = 1.0f;

    [Header("Search Behaviour")]
    [SerializeField] private int searchLooks = 10;
    [SerializeField] private float searchLookAngle = 130f;
    [SerializeField] private float searchTurnSpeed = 130f;
    [SerializeField] private float searchPausePerLook = 3f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackInterval = 3f;

    private Vector3 spawnPoint;
    private Vector3 lastKnownPlayerPosition;

    // Perception state
    private bool hasLOS;
    private bool hasExactAwareness;
    private bool hasEverDetected;
    private float timeSinceLostLOS = Mathf.Infinity;
    private float timeSinceLastExact = Mathf.Infinity;
    private bool hadLOSLastFrame;

    // Combat
    private PlayerHealth playerHealth;
    private float lastAttackTime = -Mathf.Infinity;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.speed = moveSpeed;

        spawnPoint = transform.position;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
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
            hasLOS = false;
            hasExactAwareness = false;
            return;
        }

        float dt = Time.deltaTime;

        hasLOS = CanSeePlayer(player);

        if (hasLOS)
            timeSinceLostLOS = 0f;
        else
            timeSinceLostLOS += dt;

        float dist = Vector3.Distance(transform.position, player.position);

        bool closeRetention = hasEverDetected && dist <= closeRetentionRadius;
        bool postLOSTracking = hasEverDetected && !hasLOS && timeSinceLostLOS <= trackTime;

        hasExactAwareness = hasLOS || closeRetention || postLOSTracking;

        if (hasExactAwareness)
        {
            hasEverDetected = true;
            lastKnownPlayerPosition = player.position;
            timeSinceLastExact = 0f;
        }
        else
        {
            timeSinceLastExact += dt;
        }

        hadLOSLastFrame = hasLOS;
    }

    // Enemy behaviour
    private enum State { Patrol, Chase, Investigate, Search }
    private State state = State.Patrol;

    private IEnumerator StateLoop()
    {
        while (true)
        {
            switch (state)
            {
                case State.Patrol: yield return Patrol(); break;
                case State.Chase: yield return Chase(); break;
                case State.Investigate: yield return Investigate(); break;
                case State.Search: yield return Search(); break;
            }
            yield return null;
        }
    }

    private void SetState(State s)
    {
        if (state == s)
            return;

        state = s;

        switch (state)
        {
            case State.Patrol:
                agent.stoppingDistance = 0f;
                break;
            case State.Chase:
                agent.stoppingDistance = Mathf.Max(attackRange, 1f);
                break;
            case State.Investigate:
                agent.stoppingDistance = 0f;
                break;
            case State.Search:
                agent.ResetPath();
                agent.stoppingDistance = 0f;
                break;
        }
    }

    private IEnumerator Patrol()
    {
        if (hasExactAwareness)
        {
            SetState(State.Chase);
            yield break;
        }

        Vector3? point = GetRandomNavmeshPoint(spawnPoint, wanderRadius, 20, wanderSampleRadius);
        if (point.HasValue)
        {
            agent.isStopped = false;
            agent.SetDestination(point.Value);

            float t = 0f;
            while (t < wanderInterval && !HasReachedDestination())
            {
                if (hasExactAwareness) { SetState(State.Chase); yield break; }
                t += Time.deltaTime;
                yield return null;
            }
        }

        float dwellFor = Random.Range(dwellMin, dwellMax);
        float elapsed = 0f;
        while (elapsed < dwellFor)
        {
            if (hasExactAwareness) { SetState(State.Chase); yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator Chase()
    {
        while (state == State.Chase)
        {
            if (hasExactAwareness && player != null)
            {
                agent.isStopped = false;
                agent.SetDestinationKeepOnNavmesh(player.position, destinationSampleRadius);
                yield return null;
                continue;
            }

            if (timeSinceLastExact < forgetTime)
            {
                agent.isStopped = false;
                agent.SetDestinationKeepOnNavmesh(lastKnownPlayerPosition, destinationSampleRadius);
                SetState(State.Investigate);
                yield break;
            }

            SetState(State.Patrol);
            yield break;
        }
    }

    private IEnumerator Investigate()
    {
        if (hasExactAwareness)
        {
            SetState(State.Chase);
            yield break;
        }

        agent.isStopped = false;
        agent.SetDestinationKeepOnNavmesh(lastKnownPlayerPosition, destinationSampleRadius);

        while (state == State.Investigate)
        {
            if (hasExactAwareness) { SetState(State.Chase); yield break; }
            if (timeSinceLastExact >= forgetTime) { SetState(State.Patrol); yield break; }

            if (HasReachedDestination()) { SetState(State.Search); yield break; }
            yield return null;
        }
    }

    private IEnumerator Search()
    {
        Quaternion startRotation = transform.rotation;

        for (int i = 0; i < searchLooks; i++)
        {
            if (hasExactAwareness) { SetState(State.Chase); yield break; }
            if (timeSinceLastExact >= forgetTime) { SetState(State.Patrol); yield break; }

            float yaw = GetAlternatingYawOffset(i, searchLookAngle);
            Quaternion targetRotation = startRotation * Quaternion.Euler(0f, yaw, 0f);

            while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
            {
                if (hasExactAwareness) { SetState(State.Chase); yield break; }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, searchTurnSpeed * Time.deltaTime);
                yield return null;
            }

            float pause = 0f;
            while (pause < searchPausePerLook)
            {
                if (hasExactAwareness) { SetState(State.Chase); yield break; }
                pause += Time.deltaTime;
                yield return null;
            }
        }

        SetState(timeSinceLastExact < forgetTime ? State.Investigate : State.Patrol);
    }

    private void UpdateMovementFacing()
    {
        if (agent.isStopped || agent.velocity.sqrMagnitude <= 0.01f)
            return;

        Vector3 dir = agent.velocity;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool("IsRunning", agent.velocity.magnitude >= 0.5f);
    }

    private void TryAttackIfValid()
    {
        if (player == null || !hasExactAwareness || !hasLOS)
            return;

        if (Vector3.Distance(transform.position, player.position) > attackRange)
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

    private bool CanSeePlayer(Transform playerTransform)
    {
        Vector3 enemyEye = transform.position + Vector3.up * eyeHeight;
        Vector3 playerAim = playerTransform.position + Vector3.up * playerAimHeight;

        Vector3 toPlayer = playerAim - enemyEye;
        float distance = toPlayer.magnitude;

        if (distance > sightDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f)
            return false;

        Vector3 dir = toPlayer / Mathf.Max(distance, 0.0001f);

        if (Physics.Raycast(enemyEye, dir, out RaycastHit hit, sightDistance, visionMask, QueryTriggerInteraction.Ignore))
            return hit.transform.IsChildOf(playerTransform);

        return false;
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
