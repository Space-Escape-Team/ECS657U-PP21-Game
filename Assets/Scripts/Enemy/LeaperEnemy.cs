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
    [SerializeField] private float minAwarenessRadius = 25f;
    [SerializeField] private float maxAwarenessRadius = 75f;

    [Header("Wander")]
    private float wanderRadius;
    [SerializeField] private float minWanderRadius = 25f;
    [SerializeField] private float maxWanderRadius = 75f;

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
    [SerializeField] private float minHopDistance = 5f;
    [SerializeField] private float maxHopDistance = 10f;

    [SerializeField] private float hopDuration = 0.5f;
    [SerializeField] private float hopArcHeight = 1f;

    private float hopCooldown;
    [SerializeField] private float minHopCooldown = 0.3f;
    [SerializeField] private float maxHopCooldown = 1f;

    [SerializeField] private float hopTurnSpeed = 900f;
    [SerializeField] private float arriveDistance = 1f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackInterval = 3f;
    private int attackDamage;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Vector3 spawnPoint;
    private Vector3 lastKnownPlayerPosition;

    private bool isAware;
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
        agent.updateRotation = false;
        agent.updatePosition = false;

        spawnPoint = transform.position;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            difficulty = player.GetComponent<GameDifficulty>().difficulty;
            AssignDifficultyStats();
        }

        StartCoroutine(StateLoop());
    }

    private void Update()
    {
        UpdatePerception();
        TryAttackIfValid();

        if (isAware && player != null)
        {
            FaceToward(player.position);
        }
    }

    private void AssignDifficultyStats()
    {
        // Random stats are chosen from the lower half of the bounds
        if (difficulty == GameDifficulty.Difficulty.Story)
        {
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
            isAware = false;
            return;
        }

        // Enemy is aware of the player if the player is within it's awareness radius
        isAware = Vector3.Distance(transform.position, player.position) <= awarenessRadius;

        if (isAware)
            lastKnownPlayerPosition = player.position;
    }

    // Simple state machine controls behaviour
    private IEnumerator StateLoop()
    {
        while (true)
        {
            if (isAware)
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
            if (isAware) { state = State.Chase; yield break; }
            yield return HopToward(roam.Value);
            t += hopDuration + hopCooldown;
        }

        float dwell = dwellTime;
        float elapsed = 0f;
        while (elapsed < dwell)
        {
            if (isAware) { state = State.Chase; yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Enemy has spotted player and is pursuing them
    private IEnumerator Chase()
    {
        while (state == State.Chase)
        {
            if (!isAware || player == null)
            {
                state = State.Investigate;
                yield break;
            }

            if (Vector3.Distance(transform.position, player.position) > attackRange)
                yield return HopToward(player.position);

            yield return null;
        }
    }

    // Enemy has lost sight of player so goes to the last know player location
    private IEnumerator Investigate()
    {
        while (state == State.Investigate)
        {
            if (isAware) { state = State.Chase; yield break; }
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
            if (isAware) { state = State.Chase; yield break; }
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

        // Determines starting location
        Vector3 start = transform.position;
        Vector3 toGoal = goal - start;
        toGoal.y = 0f;

        // Determines how far to hop
        float step = Mathf.Min(hopDistance, toGoal.magnitude);
        Vector3 desired = start + toGoal.normalized * step;

        // Determines landing location
        Vector3 landing = desired;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, hopSampleRadius, NavMesh.AllAreas))
            landing = hit.position;

        // Moves the enemy in an arc across a set duration
        float t = 0f;
        while (t < hopDuration)
        {
            float a = t / hopDuration;
            Vector3 pos = Vector3.Lerp(start, landing, a);
            pos.y += Mathf.Sin(a * Mathf.PI) * hopArcHeight;
            Vector3 moveStep = pos - transform.position;
            agent.Move(moveStep);
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = landing;
        agent.Warp(landing);

        yield return new WaitForSeconds(hopCooldown);
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
        if (!isAware || playerHealth == null)
            return;

        if (Vector3.Distance(transform.position, player.position) > attackRange)
            return;

        if (Time.time - lastAttackTime < attackInterval)
            return;

        playerHealth.TakeDamage(attackDamage);
        lastAttackTime = Time.time;
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