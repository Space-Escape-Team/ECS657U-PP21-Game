using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviour : MonoBehaviour
{
    private NavMeshAgent agent;

    private enum BrainType
    {
        Runner,
        Leaper,
        WeepingAngel
    }

    [Header("Brain Selection")]
    [SerializeField] private BrainType brainType = BrainType.Runner;
    private BrainType activeBrain;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Tooltip("Assign the player's camera. Necessary for the Weeping Angel.")]
    [SerializeField] private Camera playerCamera;

    [Header("Wander (used by Runner + Leaper)")]
    [SerializeField] private float wanderRadius = 50f;
    [SerializeField] private float wanderInterval = 15f;
    [SerializeField] private float dwellMin = 5f;
    [SerializeField] private float dwellMax = 10f;

    [Header("NavMesh Sampling")]
    [SerializeField] private float destinationSampleRadius = 2.0f;
    [SerializeField] private float wanderSampleRadius = 10f;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 900f;

    [Header("Animation (Runner only)")]
    [SerializeField] private Animator animator;


    [Header("Runner Perception")]
    [SerializeField] private float runnerSightDistance = 40f;
    [SerializeField, Range(0f, 180f)] private float runnerFieldOfView = 110f;
    [SerializeField] private float runnerTrackTime = 5f;
    [SerializeField] private float runnerForgetTime = 15f;
    [SerializeField] private float runnerCloseRetentionRadius = 10f;
    [SerializeField] private LayerMask runnerVisionMask = ~0;
    [SerializeField] private float runnerEyeHeight = 1.0f;
    [SerializeField] private float runnerPlayerAimHeight = 1.0f;

    [Header("Runner Search Behaviour")]
    [SerializeField] private int searchLooks = 10;
    [SerializeField] private float searchLookAngle = 130f;
    [SerializeField] private float searchTurnSpeed = 130f; // degrees/sec
    [SerializeField] private float searchPausePerLook = 3f;


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


    [Header("Combat (shared)")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackInterval = 3f;

    private Vector3 spawnPoint;
    private Vector3 lastKnownPlayerPosition;

    // Runner perception state
    private bool runnerHasLOS;
    private bool runnerHasExactAwareness;
    private bool runnerHasEverDetected;
    private float runnerTimeSinceLostLOS = Mathf.Infinity;
    private float runnerTimeSinceLastExact = Mathf.Infinity;
    private bool runnerHadLOSLastFrame;

    // Leaper/Angel simple awareness state
    private bool simpleAware;

    // Combat
    private PlayerHealth playerHealth;
    private float lastAttackTime = -Mathf.Infinity;

    // Leaper runtime
    private bool leaperIsHopping;

    // Angel runtime
    private float angelNoTargetUnwatchedTimer;
    private float angelBaseSpeed;
    private float angelBaseAccel;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        spawnPoint = transform.position;
        activeBrain = brainType;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerCamera == null) 
            playerCamera = Camera.main;

        angelBaseSpeed = agent.speed;
        angelBaseAccel = agent.acceleration;
    }

    private void Start()
    {
        StartCoroutine(RunActiveBrain());
    }

    private void Update()
    {
        UpdatePerceptionByBrain();
        UpdateMovementFacing();
        UpdateAnimation();
        TryAttackIfValid();
    }

    private IEnumerator RunActiveBrain()
    {
        switch (activeBrain)
        {
            case BrainType.Runner:
                yield return RunnerLoop();
                yield break;

            case BrainType.Leaper:
                yield return LeaperLoop();
                yield break;

            case BrainType.WeepingAngel:
                yield return WeepingAngelLoop();
                yield break;
        }
    }

    private void UpdatePerceptionByBrain()
    {
        if (player == null)
        {
            runnerHasLOS = false;
            runnerHasExactAwareness = false;
            simpleAware = false;
            return;
        }

        switch (activeBrain)
        {
            case BrainType.Runner:
                UpdatePerception_Runner();
                break;

            case BrainType.Leaper:
                UpdatePerception_SimpleRadius(leaperAwarenessRadius);
                break;

            case BrainType.WeepingAngel:
                UpdatePerception_SimpleRadius(angelAwarenessRadius);
                break;
        }
    }

    private void UpdatePerception_Runner()
    {
        float dt = Time.deltaTime;

        runnerHasLOS = CanSeePlayer(player);

        if (runnerHasLOS)
            runnerTimeSinceLostLOS = 0f;
        else
            runnerTimeSinceLostLOS += dt;

        float dist = Vector3.Distance(transform.position, player.position);

        bool closeRetention = runnerHasEverDetected && dist <= runnerCloseRetentionRadius;
        bool postLOSTracking = runnerHasEverDetected && !runnerHasLOS && runnerTimeSinceLostLOS <= runnerTrackTime;

        runnerHasExactAwareness = runnerHasLOS || closeRetention || postLOSTracking;

        if (runnerHasExactAwareness)
        {
            runnerHasEverDetected = true;
            lastKnownPlayerPosition = player.position;
            runnerTimeSinceLastExact = 0f;
        }
        else
        {
            runnerTimeSinceLastExact += dt;
        }

        runnerHadLOSLastFrame = runnerHasLOS;
    }

    private void UpdatePerception_SimpleRadius(float radius)
    {
        float dist = Vector3.Distance(transform.position, player.position);
        simpleAware = dist <= radius;

        if (simpleAware)
            lastKnownPlayerPosition = player.position;
    }

    // Runner enemy behaviour
    private enum RunnerState {Patrol, Chase, Investigate, Search}
    private RunnerState runnerState = RunnerState.Patrol;

    private IEnumerator RunnerLoop()
    {
        while (true)
        {
            switch (runnerState)
            {
                case RunnerState.Patrol: yield return Runner_Patrol(); break;
                case RunnerState.Chase: yield return Runner_Chase(); break;
                case RunnerState.Investigate: yield return Runner_Investigate(); break;
                case RunnerState.Search: yield return Runner_Search(); break;
            }
            yield return null;
        }
    }

    private void SetRunnerState(RunnerState s)
    {
        if (runnerState == s) 
            return;
        runnerState = s;

        switch (runnerState)
        {
            case RunnerState.Patrol:
                agent.stoppingDistance = 0f;
                break;
            case RunnerState.Chase:
                agent.stoppingDistance = Mathf.Max(attackRange, 1f);
                break;
            case RunnerState.Investigate:
                agent.stoppingDistance = 0f;
                break;
            case RunnerState.Search:
                agent.ResetPath();
                agent.stoppingDistance = 0f;
                break;
        }
    }

    private IEnumerator Runner_Patrol()
    {
        if (runnerHasExactAwareness) 
        { 
            SetRunnerState(RunnerState.Chase); 
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
                if (runnerHasExactAwareness) { SetRunnerState(RunnerState.Chase); yield break; }
                t += Time.deltaTime;
                yield return null;
            }
        }

        float dwellFor = Random.Range(dwellMin, dwellMax);
        float elapsed = 0f;
        while (elapsed < dwellFor)
        {
            if (runnerHasExactAwareness) { SetRunnerState(RunnerState.Chase); yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator Runner_Chase()
    {
        while (runnerState == RunnerState.Chase)
        {
            if (runnerHasExactAwareness && player != null)
            {
                agent.isStopped = false;
                agent.SetDestinationKeepOnNavmesh(player.position, destinationSampleRadius);
                yield return null;
                continue;
            }

            if (runnerTimeSinceLastExact < runnerForgetTime)
            {
                agent.isStopped = false;
                agent.SetDestinationKeepOnNavmesh(lastKnownPlayerPosition, destinationSampleRadius);
                SetRunnerState(RunnerState.Investigate);
                yield break;
            }

            SetRunnerState(RunnerState.Patrol);
            yield break;
        }
    }

    private IEnumerator Runner_Investigate()
    {
        if (runnerHasExactAwareness) 
        { 
            SetRunnerState(RunnerState.Chase); 
            yield break; 
        }

        agent.isStopped = false;
        agent.SetDestinationKeepOnNavmesh(lastKnownPlayerPosition, destinationSampleRadius);

        while (runnerState == RunnerState.Investigate)
        {
            if (runnerHasExactAwareness) 
            { 
                SetRunnerState(RunnerState.Chase); 
                yield break; 
            }
            if (runnerTimeSinceLastExact >= runnerForgetTime) 
            { 
                SetRunnerState(RunnerState.Patrol); 
                yield break; 
            }

            if (HasReachedDestination()) 
            { 
                SetRunnerState(RunnerState.Search); 
                yield break; 
            }
            yield return null;
        }
    }

    private IEnumerator Runner_Search()
    {
        Quaternion startRotation = transform.rotation;

        for (int i = 0; i < searchLooks; i++)
        {
            if (runnerHasExactAwareness) 
            { 
                SetRunnerState(RunnerState.Chase); 
                yield break; 
            }
            if (runnerTimeSinceLastExact >= runnerForgetTime) 
            { 
                SetRunnerState(RunnerState.Patrol); 
                yield break; 
            }

            float yaw = GetAlternatingYawOffset(i, searchLookAngle);
            Quaternion targetRotation = startRotation * Quaternion.Euler(0f, yaw, 0f);

            while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
            {
                if (runnerHasExactAwareness) 
                { 
                    SetRunnerState(RunnerState.Chase); 
                    yield break; 
                }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, searchTurnSpeed * Time.deltaTime);
                yield return null;
            }

            float pause = 0f;
            while (pause < searchPausePerLook)
            {
                if (runnerHasExactAwareness) 
                { 
                    SetRunnerState(RunnerState.Chase); 
                    yield break; 
                }
                pause += Time.deltaTime;
                yield return null;
            }
        }

        SetRunnerState(runnerTimeSinceLastExact < runnerForgetTime ? RunnerState.Investigate : RunnerState.Patrol);
    }

    // Leaper enemy behaviour
    private enum LeaperState {Wander, Chase, GoToLastKnown, WaitAtLastKnown, ReturnToSpawn}
    private LeaperState leaperState = LeaperState.Wander;

    private IEnumerator LeaperLoop()
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

    // Weeping Angel enemy behaviour
    private enum AngelState {Idle, Chase, FrozenNoTarget, ReturnToSpawn}
    private AngelState angelState = AngelState.Idle;

    private IEnumerator WeepingAngelLoop()
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

        // Only Runner uses animations right now
        if (activeBrain != BrainType.Runner) 
            return;

        animator.SetBool("IsRunning", agent.velocity.magnitude >= 0.5f);
    }

    private void TryAttackIfValid()
    {
        if (player == null) 
            return;

        if (activeBrain == BrainType.Runner)
        {
            if (!runnerHasExactAwareness) 
                return;
            if (!runnerHasLOS) 
                return;
        }
        else if (activeBrain == BrainType.Leaper)
        {
            if (!simpleAware) 
                return;
        }
        else if (activeBrain == BrainType.WeepingAngel)
        {
            if (!simpleAware) 
                return;
            if (IsObservedByPlayer_CameraConeLOS()) 
                return;
        }
        else
        {
            return;
        }

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

        if (animator != null && activeBrain == BrainType.Runner)
            animator.SetTrigger("Attack");
    }

    private bool CanSeePlayer(Transform playerTransform)
    {
        Vector3 enemyEye = transform.position + Vector3.up * runnerEyeHeight;
        Vector3 playerAim = playerTransform.position + Vector3.up * runnerPlayerAimHeight;

        Vector3 toPlayer = playerAim - enemyEye;
        float distance = toPlayer.magnitude;

        if (distance > runnerSightDistance) 
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > runnerFieldOfView * 0.5f) 
            return false;

        Vector3 dir = toPlayer / Mathf.Max(distance, 0.0001f);

        if (Physics.Raycast(enemyEye, dir, out RaycastHit hit, runnerSightDistance, runnerVisionMask, QueryTriggerInteraction.Ignore))
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

public static class NavMeshAgentExtensions
{
    public static bool SetDestinationKeepOnNavmesh(this NavMeshAgent agent, Vector3 target, float sampleRadius = 2.0f)
    {
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return true;
        }
        return false;
    }
}