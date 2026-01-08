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
    private float wanderRadius;
    [SerializeField] private float minWanderRadius = 30f;
    [SerializeField] private float maxWanderRadius = 70f;

    private float wanderInterval;
    [SerializeField] private float minWanderInterval = 10f;
    [SerializeField] private float maxWanderInterval = 30f;

    private float dwellTime;
    [SerializeField] private float dwellMin = 3f;
    [SerializeField] private float dwellMax = 10f;

    [Header("NavMesh Sampling")]
    [SerializeField] private float destinationSampleRadius = 2.0f;
    [SerializeField] private float wanderSampleRadius = 10f;

    [Header("Speed")]
    private float moveSpeed;
    [SerializeField] private float minMoveSpeed = 10f;
    [SerializeField] private float maxMoveSpeed = 20f;
    [SerializeField] private float turnSpeed = 1000f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Perception")]
    private float sightDistance;
    [SerializeField] private float minSightDistance = 30f;
    [SerializeField] private float maxSightDistance = 70f;
    [SerializeField, Range(0f, 180f)] private float fieldOfView = 110f;

    private float trackTime;
    [SerializeField] private float minTrackTime = 3f;
    [SerializeField] private float maxTrackTime = 10f;

    private float forgetTime;
    [SerializeField] private float minForgetTime = 10f;
    [SerializeField] private float maxForgetTime = 20f;

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
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackInterval = 3f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    // Idle sounds
    [SerializeField] private AudioClip[] idleSounds;
    [SerializeField] private float minIdleSoundInterval = 5f;
    [SerializeField] private float maxIdleSoundInterval = 15f;

    // Attack sounds
    [SerializeField] private AudioClip[] attackSounds;

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

    // Difficulty
    [SerializeField] private GameDifficulty.Difficulty difficulty;

    // States
    private enum State {Wander, Chase, Investigate, Search}
    private State state = State.Wander;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = Mathf.Max(attackRange - 0.2f, 2f);
        agent.updateRotation = false;

        spawnPoint = transform.position;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            difficulty = player.GetComponent<GameDifficulty>().difficulty;
            AssignDifficultyStats();
        }

        StartCoroutine(PlayRandomIdleSounds());
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
            forgetTime = Random.Range(minForgetTime, (minForgetTime + maxForgetTime) / 2);
            moveSpeed = Random.Range(minMoveSpeed, (minMoveSpeed + maxMoveSpeed) / 2);
            sightDistance = Random.Range(minSightDistance, (minSightDistance + maxSightDistance) / 2);
            trackTime = Random.Range(minTrackTime, (minTrackTime + maxTrackTime) / 2);
            wanderInterval = Random.Range(minWanderInterval, (minWanderInterval + maxWanderInterval) / 2);
            wanderRadius = Random.Range(minWanderRadius, (minWanderRadius + maxWanderRadius) / 2);
            dwellTime = Random.Range(dwellMin, (dwellMin + dwellMax) / 2);
            attackDamage = 15;
        }
        // Random stats are chosen from the upper half of the bounds
        else if (difficulty == GameDifficulty.Difficulty.Challenge)
        {
            forgetTime = Random.Range((minForgetTime + maxForgetTime) / 2, maxForgetTime);
            moveSpeed = Random.Range((minMoveSpeed + maxMoveSpeed) / 2, maxMoveSpeed);
            sightDistance = Random.Range((minSightDistance + maxSightDistance) / 2, maxSightDistance);
            trackTime = Random.Range((minTrackTime + maxTrackTime) / 2, maxTrackTime);
            wanderInterval = Random.Range((minWanderInterval + maxWanderInterval) / 2, maxWanderInterval);
            wanderRadius = Random.Range((minWanderRadius + maxWanderRadius) / 2, maxWanderRadius);
            dwellTime = Random.Range((dwellMin + dwellMax) / 2, dwellMax);
            attackDamage = 25;
        }
        // Random stats can be any value within the bounds
        else
        {
            forgetTime = Random.Range(minForgetTime, maxForgetTime);
            moveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
            sightDistance = Random.Range(minSightDistance, maxSightDistance);
            trackTime = Random.Range(minTrackTime, maxTrackTime);
            wanderInterval = Random.Range(minWanderInterval, maxWanderInterval);
            wanderRadius = Random.Range(minWanderRadius, maxWanderRadius);
            dwellTime = Random.Range(dwellMin, dwellMax);
            attackDamage = 20;
        }
        agent.speed = moveSpeed;
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

        // Enemy have an unobstructed view of the player to detect them
        hasLOS = CanSeePlayer(player);

        if (hasLOS)
            timeSinceLostLOS = 0f;
        else
            timeSinceLostLOS += dt;

        float dist = Vector3.Distance(transform.position, player.position);

        // Once aware, will know the players exact location when close or for a short time after losing sight
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

    // Simple state machine controls behaviour
    private IEnumerator StateLoop()
    {
        while (true)
        {
            switch (state)
            {
                case State.Wander: yield return Wander(); break;
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
            case State.Wander:
                break;
            case State.Chase:
                break;
            case State.Investigate:
                break;
            case State.Search:
                agent.ResetPath();
                break;
        }
    }

    // Enemy wanders randomly within its wander radius
    private IEnumerator Wander()
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

    // Enemy has spotted player and is pursuing them
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

            SetState(State.Wander);
            yield break;
        }
    }

    // Enemy has lost sight of player so goes to the last know player location
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
            if (timeSinceLastExact >= forgetTime) { SetState(State.Wander); yield break; }

            if (HasReachedDestination()) { SetState(State.Search); yield break; }
            yield return null;
        }
    }

    // Enemy is at last known player position so looks around to find player
    private IEnumerator Search()
    {
        Quaternion startRotation = transform.rotation;

        for (int i = 0; i < searchLooks; i++)
        {
            if (hasExactAwareness) { SetState(State.Chase); yield break; }
            if (timeSinceLastExact >= forgetTime) { SetState(State.Wander); yield break; }

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

        SetState(timeSinceLastExact < forgetTime ? State.Investigate : State.Wander);
    }

    // Makes the enemy face the direction of movement
    private void UpdateMovementFacing()
    {
       Vector3 targetDirection = Vector3.zero;

        // If moving, face direction of movement
        if (!agent.isStopped && agent.velocity.sqrMagnitude > 0.01f)
        {
            targetDirection = agent.velocity;
        }

        // If not moving but aware of player, face the player
        else if (hasExactAwareness && player != null)
        {
            targetDirection = player.position - transform.position;
        }

        if (targetDirection.sqrMagnitude > 0.0001f)
        {
            targetDirection.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        // Only animate a run cycle when moving
        animator.SetBool("IsRunning", agent.velocity.magnitude >= 0.5f);
    }

    // Multiple checks for whether the player can be successfully attacked. If possible, do so
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

        // Play random attack sound
        PlayRandomSound(attackSounds);
    }

    private void PlayIdle()
    {
        animator.speed = 1f;
        animator.Play("IDLE");
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips.Length == 0 || audioSource == null) 
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.PlayOneShot(clip);
    }

    private IEnumerator PlayRandomIdleSounds()
    {
        while (true)
        {
            PlayRandomSound(idleSounds);

            float waitTime = Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private bool CanSeePlayer(Transform playerTransform)
    {
        Vector3 enemyEye = transform.position + Vector3.up * eyeHeight;
        Vector3 playerAim = playerTransform.position + Vector3.up * playerAimHeight;

        Vector3 toPlayer = playerAim - enemyEye;
        float distance = toPlayer.magnitude;

        if (distance > sightDistance)
            return false;

        // Can only see player within the enemies field of view
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f)
            return false;

        Vector3 dir = toPlayer / Mathf.Max(distance, 0.0001f);

        // Raycast determines if an object is obstructing the view of the player
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

    // Generates alternating angles for the search behaviour where the enemy looks side to side
    private static float GetAlternatingYawOffset(int index, float stepAngle)
    {
        if (index == 0)
            return 0f;

        int k = (index + 1) / 2;
        float side = (index % 2 == 1) ? 1f : -1f;
        return side * k * stepAngle;
    }
}