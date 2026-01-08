using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class LeaperEnemy : MonoBehaviour
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
    [SerializeField] private float wanderSampleRadius = 10f;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 900f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Leaper Perception")]
    [SerializeField] private float leaperAwarenessRadius = 50f;

    [Header("Leaper Loss Behaviour")]
    [SerializeField] private float leaperWaitAtLastKnownMin = 5f;
    [SerializeField] private float leaperWaitAtLastKnownMax = 10f;

    [Header("Leaper Hop Movement")]
    [SerializeField] private float leaperHopDistance = 5f;
    [SerializeField] private float leaperHopDuration = 0.5f;
    [SerializeField] private float leaperHopCooldown = 1f;
    [SerializeField] private float leaperHopArcHeight = 1f;
    [SerializeField] private float leaperArriveDistance = 1f;
    [SerializeField] private float leaperTurnSpeed = 900f;
    [SerializeField] private float leaperHopSampleRadius = 2.0f;

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

    // Leaper runtime
    private bool leaperIsHopping;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

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
            simpleAware = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        simpleAware = dist <= leaperAwarenessRadius;

        if (simpleAware)
            lastKnownPlayerPosition = player.position;
    }

    // Leaper enemy behaviour
    private enum LeaperState {Wander, Chase, GoToLastKnown, WaitAtLastKnown, ReturnToSpawn}
    private LeaperState leaperState = LeaperState.Wander;

    private IEnumerator StateLoop()
    {
        agent.isStopped = true;
        agent.ResetPath();
        agent.updatePosition = false;

        while (true)
        {
            if (simpleAware) 
                leaperState = LeaperState.Chase;

            switch (leaperState)
            {
                case LeaperState.Wander: agent.stoppingDistance = 0f; yield return Leaper_Wander(); break;
                case LeaperState.Chase: yield return Leaper_Chase(); agent.stoppingDistance = Mathf.Max(attackRange, 1f); break;
                case LeaperState.GoToLastKnown: yield return Leaper_GoToLastKnown(); agent.stoppingDistance = 0f; break;
                case LeaperState.WaitAtLastKnown: yield return Leaper_WaitAtLastKnown(); agent.stoppingDistance = 0f; break;
                case LeaperState.ReturnToSpawn: yield return Leaper_ReturnToSpawn(); agent.stoppingDistance = 0f; break;
            }
            yield return null;
        }
    }

    private IEnumerator Leaper_Wander()
    {
        Vector3? roam = GetRandomNavmeshPoint(spawnPoint, wanderRadius, 20, wanderSampleRadius);
        if (!roam.HasValue) 
            yield break;

        float timer = 0f;
        while (timer < wanderInterval)
        {
            if (simpleAware) 
            { 
                leaperState = LeaperState.Chase; 
                yield break; 
            }
            if (Vector3.Distance(transform.position, roam.Value) <= leaperArriveDistance) 
                break;

            yield return Leaper_HopToward(roam.Value);
            timer += leaperHopDuration + leaperHopCooldown;
        }

        float dwellFor = Random.Range(dwellMin, dwellMax);
        float elapsed = 0f;
        while (elapsed < dwellFor)
        {
            if (simpleAware) 
            { 
                leaperState = LeaperState.Chase; 
                yield break; 
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator Leaper_Chase()
    {
        while (leaperState == LeaperState.Chase)
        {
            if (!simpleAware || player == null) 
            { 
                leaperState = LeaperState.GoToLastKnown; 
                yield break; 
            }

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                FaceToward(player.position, leaperTurnSpeed);
                yield return null;
                continue;
            }

            yield return Leaper_HopToward(player.position);
        }
    }

    private IEnumerator Leaper_GoToLastKnown()
    {
        Vector3 target = lastKnownPlayerPosition;
        while (leaperState == LeaperState.GoToLastKnown)
        {
            if (simpleAware) 
            { 
                leaperState = LeaperState.Chase; 
                yield break; 
            }
            if (Vector3.Distance(transform.position, target) <= leaperArriveDistance) 
            { 
                leaperState = LeaperState.WaitAtLastKnown; 
                yield break; 
            }
            yield return Leaper_HopToward(target);
        }
    }

    private IEnumerator Leaper_WaitAtLastKnown()
    {
        float waitFor = Random.Range(leaperWaitAtLastKnownMin, leaperWaitAtLastKnownMax);
        float t = 0f;
        while (t < waitFor)
        {
            if (simpleAware) 
            { 
                leaperState = LeaperState.Chase; 
                yield break; 
            }
            t += Time.deltaTime;
            yield return null;
        }
        leaperState = LeaperState.ReturnToSpawn;
    }

    private IEnumerator Leaper_ReturnToSpawn()
    {
        while (leaperState == LeaperState.ReturnToSpawn)
        {
            if (simpleAware) 
            { 
                leaperState = LeaperState.Chase; 
                yield break; 
            }
            if (Vector3.Distance(transform.position, spawnPoint) <= leaperArriveDistance) 
            { 
                leaperState = LeaperState.Wander; 
                yield break; 
            }
            yield return Leaper_HopToward(spawnPoint);
        }
    }

    private IEnumerator Leaper_HopToward(Vector3 goal)
    {
        if (leaperIsHopping) 
            yield break;
        leaperIsHopping = true;

        FaceToward(goal, leaperTurnSpeed);

        Vector3 start = transform.position;
        Vector3 toGoal = goal - start; toGoal.y = 0f;

        if (toGoal.sqrMagnitude < 0.0001f) 
        { 
            leaperIsHopping = false; 
            yield break; 
        }

        float step = Mathf.Min(leaperHopDistance, toGoal.magnitude);
        Vector3 desired = start + toGoal.normalized * step;

        Vector3 landing = desired;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, leaperHopSampleRadius, NavMesh.AllAreas))
            landing = hit.position;

        float t = 0f;
        while (t < leaperHopDuration)
        {
            float a = t / Mathf.Max(leaperHopDuration, 0.0001f);
            Vector3 pos = Vector3.Lerp(start, landing, a);
            pos.y += Mathf.Sin(a * Mathf.PI) * leaperHopArcHeight;

            transform.position = pos;
            agent.nextPosition = transform.position;

            t += Time.deltaTime;
            yield return null;
        }

        transform.position = landing;
        agent.Warp(landing);

        float cd = 0f;
        while (cd < leaperHopCooldown)
        {
            cd += Time.deltaTime;
            yield return null;
        }

        leaperIsHopping = false;
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