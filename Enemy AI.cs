using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    public Transform[] waypoints; // Waypoints for patrolling
    public float patrolSpeed = 2f; // Speed while patrolling
    public float chaseSpeed = 5f; // Speed while chasing
    public float attackRange = 2f; // Range to switch to attack
    public float chaseRange = 10f; // Range to switch to chase
    public Animator animator; // Animator component
    public NavMeshAgent agent; // NavMeshAgent component
    public Transform player; // Reference to the player
    public AudioSource chaseAudioSource; // AudioSource for chase sound
    public AudioClip chaseSound; // AudioClip for chase sound
    public float fadeDuration = 1f; // Duration to fade out sound

    private int currentWaypointIndex;
    private float distanceToPlayer;
    private float originalVolume;
    private bool isFadingOut;
    private bool isChasing;

    private enum AIState { Idle, Patrol, Chase, Attack, Cutscene, Death }
    private AIState currentState;

    void Start()
    {
        if (waypoints.Length > 0)
        {
            currentWaypointIndex = 0;
            currentState = AIState.Patrol;
            agent.speed = patrolSpeed;
            GoToNextWaypoint();
        }

        if (chaseAudioSource != null)
        {
            originalVolume = chaseAudioSource.volume;
            chaseAudioSource.loop = true; // Set to loop the chase sound
        }
    }

    void Update()
    {
        distanceToPlayer = Vector3.Distance(player.position, transform.position);

        switch (currentState)
        {
            case AIState.Patrol:
                Patrol();
                break;
            case AIState.Chase:
                Chase();
                break;
            case AIState.Attack:
                Attack();
                break;
            case AIState.Cutscene:
                Cutscene();
                break;
            case AIState.Idle:
                Idle();
                break;
            case AIState.Death:
                Death();
                break;
        }

        // Check if player is within chase range and not in attack range
        if (distanceToPlayer <= chaseRange && distanceToPlayer > attackRange && currentState != AIState.Death)
        {
            if (currentState != AIState.Chase)
            {
                currentState = AIState.Chase;
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                animator.SetFloat("Speed", 1f); // Running animation

                if (chaseAudioSource != null)
                {
                    chaseAudioSource.clip = chaseSound;
                    chaseAudioSource.volume = originalVolume;
                    chaseAudioSource.Play();
                }

                isChasing = true;
            }
            FacePlayer();
        }
        // Check if player is within attack range
        else if (distanceToPlayer <= attackRange && currentState != AIState.Death)
        {
            if (currentState != AIState.Attack)
            {
                currentState = AIState.Attack;
                agent.isStopped = true; // Stop the agent while attacking
                animator.SetTrigger("Attack"); // Trigger attack animation
            }
        }
        // Switch back to patrol if player is out of chase range
        else if (distanceToPlayer > chaseRange && currentState != AIState.Death)
        {
            if (currentState != AIState.Patrol)
            {
                if (chaseAudioSource != null && chaseAudioSource.isPlaying)
                {
                    StartCoroutine(FadeOutSound(chaseAudioSource, fadeDuration));
                }

                currentState = AIState.Patrol;
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                animator.SetFloat("Speed", 0.5f); // Walking animation
                GoToNextWaypoint();

                isChasing = false;
            }
        }
    }
    void FacePlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0; // Keep the AI on the same Y plane

        if (directionToPlayer != Vector3.zero)
        {
            // Apply the corrective rotation of 90 degrees on the Y axis
            Quaternion toRotation = Quaternion.LookRotation(directionToPlayer) * Quaternion.Euler(0, 90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f); // Smooth rotation
        }
    }

    void FaceWaypoint(Vector3 waypointPosition)
    {
        Vector3 directionToWaypoint = waypointPosition - transform.position;
        directionToWaypoint.y = 0; // Keep the AI on the same Y plane

        if (directionToWaypoint != Vector3.zero)
        {
            // Apply the corrective rotation of 90 degrees on the Y axis
            Quaternion toRotation = Quaternion.LookRotation(directionToWaypoint) * Quaternion.Euler(0, 90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 5f); // Smooth rotation
        }
    }


    void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }



    IEnumerator RotateAndMove()
    {
        // Wait until the AI has rotated to face the waypoint
        while (Vector3.Angle(transform.forward, agent.destination - transform.position) > 1f)
        {
            FaceWaypoint(agent.destination);
            yield return null; // Wait until the next frame
        }

        // Start moving after facing the waypoint
        animator.SetFloat("Speed", 0.5f); // Walking animation
        agent.isStopped = false; // Resume the movement
    }



    void Chase()
    {
        if (currentState == AIState.Chase)
        {
            // Only move the AI if it's not too close to the player
            if (distanceToPlayer > attackRange)
            {
                agent.isStopped = false;
                agent.destination = player.position;

                // Check if the chase sound needs to be restarted if it's not playing
                if (chaseAudioSource != null && !chaseAudioSource.isPlaying && isChasing)
                {
                    chaseAudioSource.Play();
                }
            }
            else
            {
                agent.isStopped = true;
            }
        }
    }

    void Attack()
    {
        agent.isStopped = true;
        FacePlayer(); // Keep the AI facing the player during the attack
    }

    void Idle()
    {
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f); // Idle animation
    }

    void Death()
    {
        agent.isStopped = true;
        animator.SetTrigger("Death"); // Trigger death animation
    }

    void Cutscene()
    {
        Debug.Log("Triggering cutscene...");
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        // Select a random waypoint index
        currentWaypointIndex = Random.Range(0, waypoints.Length);

        // Set the destination to the selected waypoint's position
        agent.destination = waypoints[currentWaypointIndex].position;

        // Face the waypoint immediately
        FaceWaypoint(agent.destination);

        // Start moving after a short delay to ensure facing is complete
        StartCoroutine(RotateAndMove());

        // Debug log to see which waypoint is selected and its position
        Debug.Log($"Patrolling to waypoint {currentWaypointIndex} at position {waypoints[currentWaypointIndex].position}");
    }

    // Call this method when AI dies
    public void Die()
    {
        currentState = AIState.Death;
    }

    // Visualize waypoints and AI's path in the Scene view
    void OnDrawGizmos()
    {
        if (waypoints != null)
        {
            // Draw a small sphere at each waypoint location
            foreach (Transform waypoint in waypoints)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(waypoint.position, 0.5f);
            }

            // Draw a line showing the path from the AI to the current waypoint
            if (agent != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, waypoints[currentWaypointIndex].position);
            }
        }
    }

    // Coroutine to fade out the sound
    IEnumerator FadeOutSound(AudioSource audioSource, float duration)
    {
        float startVolume = audioSource.volume;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
            yield return null;
        }

        audioSource.volume = 0;
        audioSource.Stop();
    }
}
