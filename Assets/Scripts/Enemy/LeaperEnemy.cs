using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class LeaperEnemy : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Awareness")]
    private float awarenessRadius;
    [SerializeField] private float minAwarenessRadius = 30f;
    [SerializeField] private float maxAwarenessRadius = 70f;

    [Header("Wander")]
    private float wanderRadius;
    [SerializeField] private float minWanderRadius = 30f;
    [SerializeField] private float maxWanderRadius = 70f;

    private float wanderInterval;
    [SerializeField] private float minWanderInterval = 10f;
    [SerializeField] private float maxWanderInterval = 20f;

    private float dwellTime;
    [SerializeField] private float dwellMin = 3f;
    [SerializeField] private float dwellMax = 10f;
    
    [Header("NavMesh Sampling")]
    [SerializeField] private float wanderSampleRadius = 10f;
    [SerializeField] private float hopSampleRadius = 2f;

    [Header("Hop Movement")]
    private float hopDistance;
    [SerializeField] private float minHopDistance = 2f;
    [SerializeField] private float maxHopDistance = 5f;

    [SerializeField] private float hopDuration = 1f;
    [SerializeField] private float hopArcHeight = 10f;

    private float hopCooldown;
    [SerializeField] private float minHopCooldown = 0.2f;
    [SerializeField] private float maxHopCooldown = 1f;

    [SerializeField] private float hopTurnSpeed = 900f;
    [SerializeField] private float arriveDistance = 1f;

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

    [Header("Combat")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackInterval = 3f;
    private int attackDamage;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    private float jumpAnimLength;
    private float attackAnimLength;

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

    private bool isHopping;

    // Combat
    private PlayerHealth playerHealth;
    private float lastAttackTime = -Mathf.Infinity;

    // Difficulty
    [SerializeField] private GameDifficulty.Difficulty difficulty;

    // States
    private enum State {Wander, Chase, Investigate, WaitAtLastKnown}
    private State state = State.Wander;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.updateRotation = false;
        agent.updatePosition = false;

        spawnPoint = transform.position;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            difficulty = player.GetComponent<GameDifficulty>().difficulty;
            AssignDifficultyStats();
        }

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "JUMP")
                jumpAnimLength = clip.length;
            else if (clip.name == "ATTACK")
                attackAnimLength = clip.length;
        }

        StartCoroutine(StateLoop());
        StartCoroutine(PlayRandomIdleSounds());
        PlayIdle();
    }

    private void Update()
    {
        UpdatePerception();
        TryAttackIfValid();

        if (hasExactAwareness && player != null)
        {
            FaceToward(player.position);
        }
    }

    private void AssignDifficultyStats()
    {
        // Random stats are chosen from the lower half of the bounds
        if (difficulty == GameDifficulty.Difficulty.Story)
        {
            forgetTime = Random.Range(minForgetTime, (minForgetTime + maxForgetTime) / 2);
            sightDistance = Random.Range(minSightDistance, (minSightDistance + maxSightDistance) / 2);
            trackTime = Random.Range(minTrackTime, (minTrackTime + maxTrackTime) / 2);

            awarenessRadius = Random.Range(minAwarenessRadius, (minAwarenessRadius + maxAwarenessRadius) / 2f);
            wanderRadius = Random.Range(minWanderRadius, (minWanderRadius + maxWanderRadius) / 2f);
            wanderInterval = Random.Range(minWanderInterval, (minWanderInterval + maxWanderInterval) / 2f);
            dwellTime = Random.Range(dwellMin, (dwellMin + dwellMax) / 2f);

            hopDistance = Random.Range(minHopDistance, (minHopDistance + maxHopDistance) / 2f);
            hopCooldown = Random.Range(minHopCooldown, (minHopCooldown + maxHopCooldown) / 2f);

            attackDamage = 15;
        }
        // Random stats are chosen from the upper half of the bounds
        else if (difficulty == GameDifficulty.Difficulty.Challenge)
        {
            forgetTime = Random.Range((minForgetTime + maxForgetTime) / 2, maxForgetTime);
            sightDistance = Random.Range((minSightDistance + maxSightDistance) / 2, maxSightDistance);
            trackTime = Random.Range((minTrackTime + maxTrackTime) / 2, maxTrackTime);

            awarenessRadius = Random.Range((minAwarenessRadius + maxAwarenessRadius) / 2f, maxAwarenessRadius);
            wanderRadius = Random.Range((minWanderRadius + maxWanderRadius) / 2f, maxWanderRadius);
            wanderInterval = Random.Range((minWanderInterval + maxWanderInterval) / 2f, maxWanderInterval);
            dwellTime = Random.Range((dwellMin + dwellMax) / 2f, dwellMax);

            hopDistance = Random.Range((minHopDistance + maxHopDistance) / 2f, maxHopDistance);
            hopCooldown = Random.Range((minHopCooldown + maxHopCooldown) / 2f, maxHopCooldown);

            attackDamage = 40;
        }
        // Random stats can be any value within the bounds
        else
        {
            forgetTime = Random.Range(minForgetTime, maxForgetTime);
            sightDistance = Random.Range(minSightDistance, maxSightDistance);
            trackTime = Random.Range(minTrackTime, maxTrackTime);

            awarenessRadius = Random.Range(minAwarenessRadius, maxAwarenessRadius);
            wanderRadius = Random.Range(minWanderRadius, maxWanderRadius);
            wanderInterval = Random.Range(minWanderInterval, maxWanderInterval);
            dwellTime = Random.Range(dwellMin, dwellMax);

            hopDistance = Random.Range(minHopDistance, maxHopDistance);
            hopCooldown = Random.Range(minHopCooldown, maxHopCooldown);

            attackDamage = 25;
        }
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
            if (hasExactAwareness && state != State.Chase)
                state = State.Chase;

            switch (state)
            {
                case State.Wander: yield return Wander(); break;
                case State.Chase: yield return Chase(); break;
                case State.Investigate: yield return Investigate(); break;
                case State.WaitAtLastKnown: yield return WaitAtLastKnown(); break;
            }
            yield return null;
        }
    }

    // Enemy wanders randomly within its wander radius
    private IEnumerator Wander()
    {
        Vector3? roam = GetRandomNavmeshPoint(spawnPoint, wanderRadius, 20, wanderSampleRadius);
        if (!roam.HasValue)
            yield break;

        float t = 0f;
        while (t < wanderInterval)
        {
            if (hasExactAwareness) { state = State.Chase; yield break; }
            yield return HopToward(roam.Value);
            t += hopDuration + hopCooldown;
        }

        float dwell = dwellTime;
        float elapsed = 0f;
        while (elapsed < dwell)
        {
            if (hasExactAwareness) { state = State.Chase; yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Enemy has spotted player and is pursuing them
    private IEnumerator Chase()
    {
        while (state == State.Chase)
        {
            if (!hasExactAwareness || player == null)
            {
                state = State.Investigate;
                yield break;
            }

            // Calculate direction and distance to player
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            // Only hop if player is farther than a tiny threshold
            if (distance > attackRange)
                yield return HopToward(player.position);

            yield return null;
        }
    }

    // Enemy has lost sight of player so goes to the last know player location
    private IEnumerator Investigate()
    {
        while (state == State.Investigate)
        {
            if (hasExactAwareness) { state = State.Chase; yield break; }
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) <= arriveDistance)
            {
                state = State.WaitAtLastKnown;
                yield break;
            }
            yield return HopToward(lastKnownPlayerPosition);
        }
    }

    // Enemy is at last known player position so waits in case of their return
    private IEnumerator WaitAtLastKnown()
    {
        float waitFor = Random.Range(3f, 7f);
        float t = 0f;
        while (t < waitFor)
        {
            if (hasExactAwareness) { state = State.Chase; yield break; }
            t += Time.deltaTime;
            yield return null;
        }
        state = State.Wander;
    }

    // The enemy moves in a hopping "arc" toward the player
    private IEnumerator HopToward(Vector3 goal)
    {
        if (isHopping)
            yield break;

        isHopping = true;

        // Play jump animation scaled so that 80% occurs in-air
        float airPercent = 0.8f;
        float airAnimTime = jumpAnimLength * airPercent;
        float animSpeed = airAnimTime / hopDuration;
        animator.speed = animSpeed;
        animator.Play("JUMP", 0, 0f);

        // Store start position
        Vector3 start = transform.position;
        float startY = start.y;
        Vector3 toGoal = goal - start;
        toGoal.y = 0f;

        // Rotate toward movement direction
        if (toGoal.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toGoal.normalized);

        // Determine hop distance
        float step = Mathf.Min(hopDistance, toGoal.magnitude);
        Vector3 desired = start + toGoal.normalized * step;

        // Sample landing on NavMesh
        Vector3 landing = desired;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, hopSampleRadius, NavMesh.AllAreas))
        {
            if (Vector3.Distance(hit.position, start) > 0.1f)
                landing = hit.position;
        }

        // Check if the path is clear
        Vector3 checkDir = landing - start;
        float checkDistance = checkDir.magnitude;
        if (Physics.Raycast(start + Vector3.up * 0.1f, checkDir.normalized, checkDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // Obstacle detected; skip hop
            isHopping = false;
            yield break;
        }

        // Skip hop if landing too close
        if (Vector3.Distance(start, landing) < 0.1f)
        {
            isHopping = false;
            yield break;
        }

        // Scale hop height based on distance
        float distance = Vector3.Distance(start, landing);
        float scaledHopHeight = hopArcHeight * Mathf.Clamp01(distance / hopDistance);

        // Move in an arc over hopDuration, respecting slopes
        float t = 0f;
        while (t < hopDuration)
        {
            float a = t / hopDuration;

            // Horizontal movement
            Vector3 horizontalPos = Vector3.Lerp(start, landing, a);

            // Sample ground height at this position
            float groundY = horizontalPos.y;
            if (NavMesh.SamplePosition(horizontalPos, out NavMeshHit groundHit, 1.0f, NavMesh.AllAreas))
            {
                groundY = groundHit.position.y;
            }

            // Hop arc relative to ground
            float arc = scaledHopHeight * 4f * a * (1f - a);

            transform.position = new Vector3(
                horizontalPos.x,
                groundY + arc,
                horizontalPos.z
            );

            t += Time.deltaTime;
            yield return null;
        }

        // Ensure final landing position
        transform.position = landing;

        // Play last 20% of jump animation after landing
        float landAnimTime = jumpAnimLength - airAnimTime;
        yield return new WaitForSeconds(landAnimTime / animSpeed);

        // Wait for cooldown
        yield return new WaitForSeconds(hopCooldown);

        PlayIdle();
        isHopping = false;
    }

    // Makes the enemy face the player
    private void FaceToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, hopTurnSpeed * Time.deltaTime);
    }

    // Multiple checks for whether the player can be successfully attacked. If possible, do so
    private void TryAttackIfValid()
    {
        // Check if enemy is aware and player exists
        if (!hasExactAwareness)
        {
            return;
        }

        if (playerHealth == null)
        {
            return;
        }

        // Check attack range
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange)
        {
            return;
        }

        // Check attack cooldown
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack < attackInterval)
        {
            return;
        }

        // Play attack animation
        animator.speed = 1f;
        animator.Play("ATTACK", 0, 0f);

        // Play random attack sound
        PlayRandomSound(attackSounds);

        // Apply damage
        playerHealth.TakeDamage(attackDamage);

        // Reset attack timer
        lastAttackTime = Time.time;
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

    private void PlayIdle()
    {
        animator.speed = 1f;
        animator.Play("IDLE");
    }

    private IEnumerator ReturnToIdleAfterAttack()
    {
        yield return new WaitForSeconds(attackAnimLength);
        if (!isHopping)
        {
            PlayIdle();
        }
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