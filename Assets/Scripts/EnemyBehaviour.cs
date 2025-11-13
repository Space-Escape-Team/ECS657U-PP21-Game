using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviour : MonoBehaviour
{
    private enum State {Idle, Wandering, Dwell, Chasing, Hunting}
    private State state = State.Idle;

    private Vector3 spawnPoint;

    [SerializeField] public float wanderRadius = 100;
    [SerializeField] public float wanderInterval = 10;
    [SerializeField] public float dwellMin = 3;
    [SerializeField] public float dwellMax = 5;

    [SerializeField] private Transform player;
    [SerializeField] private float sightDistance = 40f;
    [SerializeField] private float sightAngle = 55f;
    [SerializeField] private float trackTime = 5f;
    [SerializeField] private float forgetTime = 15f;

    private Vector3 lastKnownPlayerPosition;
    private float timeSinceLastSeen;

    [SerializeField] private Animator animator;

    NavMeshAgent agent;

    private int damageAmount = 25;
    private float attackInterval = 1;
    private float lastAttackTime = -Mathf.Infinity; // Enemy can attack immediately

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnPoint = transform.position;
        StartCoroutine(StateMachine());
    }

    void Update()
    {
        if (CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;
            timeSinceLastSeen = 0f;
            state = State.Chasing;
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
<<<<<<< Updated upstream:Assets/Scripts/EnemyBehaviour.cs
=======
            Debug.Log(timeSinceLastSeen);
>>>>>>> Stashed changes:Assets/Scripts/Enemy/EnemyBehaviour.cs
        }

        if (animator != null)
        {
            bool running = (state == State.Wandering || state == State.Chasing);
            animator.SetBool("isRunning", running);
        }
    }

    IEnumerator StateMachine()
    {
        while (true)
        {
            switch (state)
            {
                case State.Idle:
                    yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
                    state = State.Wandering;
                    break;
                case State.Wandering:
                    Vector3? point = GetRandomNavmeshPoint(spawnPoint, wanderRadius, 20);
                    if (point.HasValue)
                    {
                        agent.SetDestination(point.Value);
                        float t = 0f;
                        // continue while under the wander interval AND the agent has not yet reached its destination
                        while (t < wanderInterval && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
                        {
                            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
                            {
                                break;
                            }
                            t += Time.deltaTime;

                            if (state == State.Chasing)
                            {
                                break;
                            }

                            yield return null;
                        }
                    }
                    if (state != State.Chasing)
                    {
                        state = State.Dwell;
                    }
                    break;
                case State.Dwell:
                    float dwellFor = Random.Range(dwellMin, dwellMax);
                    float elapsed = 0f;
                    while (elapsed < dwellFor)
                    {
                        elapsed += Time.deltaTime;
                        if (state == State.Chasing)
                        {
                            break;
                        }
                        yield return null;
                    }
                    if (state != State.Chasing)
                    {
                        state = State.Wandering;
                    }
                    break;
                case State.Chasing:
                    // Can track player for some time after losing sight - simulates the enemy following the players direction of movement
                    if (timeSinceLastSeen <= trackTime)
                    {
                        lastKnownPlayerPosition = player.position;
                    }
                    if (state == State.Chasing && timeSinceLastSeen > trackTime)
                    {
                        state = State.Hunting;
                    }
                    if (state == State.Chasing && timeSinceLastSeen >= forgetTime)
                    {
                        state = State.Wandering;
                    }
                    while (Vector3.Distance(transform.position, lastKnownPlayerPosition) > 2f)
                    {
                        if (state != State.Chasing)
                        {
                            break;
                        }
                        
                        agent.SetDestination(lastKnownPlayerPosition);
                        yield return null;
                    }
                    break;
                case State.Hunting:
                    Debug.Log("Hello");
                    if (state == State.Hunting && timeSinceLastSeen >= forgetTime)
                    {
                        state = State.Wandering;
                    }
                    break;
            }
            yield return null;
        }
    }

    Vector3? GetRandomNavmeshPoint(Vector3 center, float radius, int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 rnd = center + Random.insideUnitSphere * radius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(rnd, out hit, 10.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return null;
    }

    bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 enemyCentre = transform.position + Vector3.up * 1f;
        Vector3 playerCentre = player.position + Vector3.up * 1f;

        Vector3 directionToPlayer = playerCentre - enemyCentre;
        float distance = directionToPlayer.magnitude;

        if (distance > sightDistance)
        {
            return false;
        }

        if (Vector3.Angle(transform.forward, directionToPlayer) > sightAngle)
        {
            return false;
        }

        if (Physics.Raycast(enemyCentre, directionToPlayer.normalized, out RaycastHit hit, sightDistance))
        {
            if (hit.transform != player)
            {
                return false;
            }
        }

        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryToAttack(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryToAttack(collision.gameObject);
    }

    private void TryToAttack(GameObject other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player == null || player.playerHealth == null)
        {
            return;
        }

        if (Time.time - lastAttackTime >= attackInterval)
        {
            player.playerHealth.TakeDamage(damageAmount);
            lastAttackTime = Time.time;

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }
}