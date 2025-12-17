using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviour : MonoBehaviour
{
    private NavMeshAgent agent;

    private enum State {Idle, Wandering, Dwell, Chasing, Investigating, Searching}
    private State state = State.Idle;

    [Header("Wander")]
    [SerializeField] public float wanderRadius = 100f;
    [SerializeField] public float wanderInterval = 10f;
    [SerializeField] public float dwellMin = 3f;
    [SerializeField] public float dwellMax = 5f;
    private Vector3 spawnPoint;

    [Header("Sight")]
    [SerializeField] private Transform player;
    [SerializeField] private float sightDistance = 40f;
    [SerializeField] private float fieldOfView = 110f;
    [SerializeField] private float trackTime = 5f;
    [SerializeField] private float forgetTime = 15f;
    [SerializeField] private float closeAwarenessRadius = 10f;
    
    
    [Header("Searching")]
    [SerializeField] private int searchLooks = 10;
    [SerializeField] private float searchLookAngle = 80f;
    [SerializeField] private float turnSpeed = 500f; // degrees/sec
    [SerializeField] private float searchTurnSpeed = 60f; // degrees/sec
    [SerializeField] private float searchPausePerLook = 3f;
    private Vector3 lastKnownPlayerPosition;
    private float timeSinceLastSeen = Mathf.Infinity;


    [Header("Raycast")]
    [SerializeField] private LayerMask visionBlockers = ~0;
    [SerializeField] private float eyeHeight = 1.0f;


    [Header("Combat")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackInterval = 3f;
    private float lastAttackTime = -Mathf.Infinity; // Enemy can attack immediately


    [Header("Animation")]
    [SerializeField] private Animator animator;
    

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        spawnPoint = transform.position;
    }

    void Start()
    {
        StartCoroutine(StateMachine());
    }

    void Update()
    {
        if (DetectPlayer())
        {
            timeSinceLastSeen = 0f;
            SetState(State.Chasing);
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
        }

        FaceDirectionOfMovement();

        if (animator != null)
        {
            float speed = agent.velocity.magnitude;

            // If above 0, enemy is moving forward
            float forwardDot = Vector3.Dot(transform.forward, agent.velocity.normalized);

            // Conditions
            bool movingForward = (speed >= 0.5f && forwardDot > 0.1f);

            animator.SetBool("IsRunning", movingForward);
        }

        TryAttackIfValid();
    }

    IEnumerator StateMachine()
    {
        while (true)
        {
            switch (state)
            {
                case State.Idle:
                    yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
                    SetState(State.Wandering);
                    break;

                case State.Wandering:
                    Vector3? point = GetRandomNavmeshPoint(spawnPoint, wanderRadius, 20);
                    if (point.HasValue)
                    {
                        agent.stoppingDistance = 0f;
                        agent.SetDestination(point.Value);

                        float t = 0f;
                        // continue while under the wander interval AND the agent has not yet reached its destination
                        while (t < wanderInterval && !HasReachedDestination())
                        {
                            // Update() can change the state to Chasing
                            if (state != State.Wandering)
                            {
                                break;
                            }
                            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
                            {
                                break;
                            }

                            t += Time.deltaTime;

                            yield return null;
                        }
                    }

                    // If reached destination without finding the player, dwell
                    if (state == State.Wandering)
                    {
                        SetState(State.Dwell);
                    }
                    break;

                case State.Dwell:
                    float dwellFor = Random.Range(dwellMin, dwellMax);
                    float elapsed = 0f;
                    while (elapsed < dwellFor)
                    {
                        // Update() can change the state to Chasing
                        if (state != State.Dwell)
                        {
                            break;
                        }
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    // If finished dwelling, start wandering
                    if (state == State.Dwell)
                    {
                        state = State.Wandering;
                    }
                    break;

                case State.Chasing:
                    agent.stoppingDistance = Mathf.Max(attackRange, 1f);

                    while (state == State.Chasing)
                    {
                        // Enemy knows where player is until losing sight for 3 seconds
                        if (timeSinceLastSeen < 3f)
                        {
                            lastKnownPlayerPosition = player.position;
                        }
                        // Enemy has lost track so investigate last known position
                        else
                        {
                            SetState(State.Investigating);
                            break;
                        }

                        RefreshDestination(lastKnownPlayerPosition);
                        yield return null;
                    }
                    break;

                case State.Investigating:
                    // Go to last known position
                    agent.stoppingDistance = 0.25f;
                    agent.SetDestinationKeepOnNavmesh(lastKnownPlayerPosition);

                    while (state == State.Investigating)
                    {
                        if (timeSinceLastSeen >= forgetTime)
                        {
                            SetState(State.Wandering);
                            break;
                        }

                        if (timeSinceLastSeen <= trackTime)
                        {
                            // Continue moving toward last known player position and then search the area
                            if (HasReachedDestination())
                            {
                                SetState(State.Searching);
                                break;
                            }
                        }
                        else
                        {
                            // Enemy stops following player and searches the immediate vicinity
                            SetState(State.Searching);
                            break;
                        }

                        yield return null;
                    }

                    break;

                case State.Searching:
                    // At last known position, look around for the player
                    agent.ResetPath();
                    agent.stoppingDistance = 0f;

                    Quaternion startRotation = transform.rotation;

                    for (int i = 0; i < searchLooks; i++)
                    {
                        if (state != State.Searching) 
                        {
                            break;
                        }
                        if (timeSinceLastSeen >= forgetTime)
                        {
                            SetState(State.Wandering);
                            break;
                        }

                        // Alternate left/right angles around forward
                        float side = (i % 2 == 0) ? 1f : -1f;
                        float step = Mathf.Ceil(i / 2f);
                        float targetYaw = side * step * searchLookAngle;

                        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, targetYaw, 0f);

                        // Smoothly rotate
                        while (state == State.Searching && Quaternion.Angle(transform.rotation, targetRotation) > 1f)
                        {
                            transform.rotation = Quaternion.RotateTowards(
                                transform.rotation,
                                targetRotation,
                                searchTurnSpeed * Time.deltaTime
                            );
                            yield return null;

                            // Update() can change the state to Chasing
                        }

                        // Short pause per look
                        float pause = 0f;
                        while (state == State.Searching && pause < searchPausePerLook)
                        {
                            pause += Time.deltaTime;
                            yield return null;
                        }
                    }

                    // If the player isn't found, dwell or wander
                    if (state == State.Searching)
                    {
                        // If the enemy still remembers the player, dwell. Else, return to wandering
                        if (timeSinceLastSeen < forgetTime)
                        {
                            SetState(State.Dwell);
                        }
                        else
                        {
                            SetState(State.Wandering);   
                        }
                    }
                    break;
            }
        }
    }

    private void SetState(State newState)
    {
        state = newState;
    }

    private void RefreshDestination(Vector3 destination)
    {
        agent.SetDestinationKeepOnNavmesh(destination);
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending) 
        {
            return false;
        }
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) 
        {
            return true; // Treat invalid pathing as completed travel to indicate no more movement 
        }
        return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.1f);
    }

    Vector3? GetRandomNavmeshPoint(Vector3 center, float radius, int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 rnd = center + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return null;
    }

    bool CanSeePlayer(out Vector3 seenPosition)
    {
        seenPosition = default;

        if (player == null)
        {
            return false;
        }

        Vector3 enemyEye = transform.position + Vector3.up * eyeHeight;
        Vector3 playerCentre = player.position + Vector3.up * 1f;

        Vector3 directionToPlayer = playerCentre - enemyEye;
        float distance = directionToPlayer.magnitude;

        if (distance > sightDistance)
        {
            return false;
        }

        float halfFOV = fieldOfView / 2;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > halfFOV)
        {
            return false;
        }

        // Check if the enemy has an unobstructed line of sight
        if (Physics.Raycast(enemyEye, directionToPlayer.normalized, out RaycastHit hit, sightDistance, visionBlockers))
        {
            if (hit.transform != player)
            {
                return false;
            }
        }

        seenPosition = player.position;
        return true;
    }

    private bool DetectPlayer()
    {
        if (player == null) 
        {
            return false;
        }

        if (CanSeePlayer(out lastKnownPlayerPosition))
        {
            timeSinceLastSeen = 0;
            return true;
        }

        // Proximity detection (mimics hearing / peripheral / footsteps)
        float distance = Vector3.Distance(transform.position, player.position);
        if (IsAware())
        {
            // Enemy has a doubled awareness radius when its actively looking for the player
            if (distance <= closeAwarenessRadius * 2)
            {
                lastKnownPlayerPosition = player.position;
                timeSinceLastSeen = 0;
                return true;
            }
        }
        else
        {
            if (distance <= closeAwarenessRadius)
            {
                lastKnownPlayerPosition = player.position;
                timeSinceLastSeen = 0;
                return true;
            }
        }

        return false;
    }

    private bool IsAware()
    {
        return state == State.Chasing || state == State.Investigating || state == State.Searching;
    }

    private void FaceDirectionOfMovement()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 dir = agent.velocity.normalized;
            dir.y = 0f;

            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }

    private void TryAttackIfValid()
    {
        // Must be aware of the player
        if (!IsAware() || player == null) 
        {
            return;
        }

        // Must be looking at player
        if (!CanSeePlayer(out _)) // discard exact position
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Must be within range
        if (distance > attackRange) 
        {
            return;
        }
        // Must not be on an attack cooldown
        if (Time.time - lastAttackTime < attackInterval) 
        {
            return;
        }

        var health = player.GetComponent<PlayerHealth>();

        // Player must still be alive
        if (health == null || !health.IsAlive)
        {
            return;
        }

        // Attack
        health.TakeDamage(attackDamage);
        lastAttackTime = Time.time;

        if (animator != null)
            animator.SetTrigger("Attack");
    }
}

/// Helper to avoid SetDestination calls to points off-navmesh.
public static class NavMeshAgentExtensions
{
    public static void SetDestinationKeepOnNavmesh(this NavMeshAgent agent, Vector3 target)
    {
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(target);
        }
    }
}