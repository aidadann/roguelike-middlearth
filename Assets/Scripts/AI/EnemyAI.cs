using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
	public enum EncounterType { PassiveTouch, AggroChase }

	[Header("Mode")]
	public EncounterType encounterType = EncounterType.AggroChase;

	[Header("References")]
	public Transform player;

	[Header("Movement")]
	public float moveSpeed = 2.5f;

	[Header("Wander")]
	public float wanderRadius = 3f;
	public float wanderPointReachDist = 0.2f;
	public float wanderRepathTime = 2.0f;

	[Header("Aggro (only if AggroChase)")]
	public float detectionRange = 5f;
	public float loseInterestRange = 7f;

	[Header("Combat Spacing (Real-time)")]
	[Tooltip("If EnemyCombat exists, engage distance will follow melee/shoot ranges. Otherwise use engageDistanceFallback.")]
	public bool useEnemyCombatRanges = true;

	[Tooltip("Used when no EnemyCombat is attached, or when useEnemyCombatRanges is false.")]
	public float engageDistanceFallback = 0.9f;

	[Tooltip("If true, ranged enemies will back away if the player is too close.")]
	public bool allowBackOffForRanged = true;

	[Tooltip("How close is 'too close' for ranged back-off, as a fraction of engage distance.")]
	[Range(0.1f, 0.9f)]
	public float backOffThreshold = 0.6f;

	private Rigidbody2D rb;

	private Vector2 spawnPos;
	private Vector2 wanderTarget;
	private float wanderTimer;

	private EnemyCombat combat; // optional (from your new real-time combat system)

	private enum State { Wander, Chase }
	private State state = State.Wander;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.gravityScale = 0f;
		rb.freezeRotation = true;

		combat = GetComponent<EnemyCombat>();

		spawnPos = transform.position;
		PickNewWanderTarget();
		wanderTimer = wanderRepathTime;
	}

	private void Update()
	{
		if (!player) return;

		float distToPlayer = Vector2.Distance(transform.position, player.position);

		// Decide state (only for AggroChase type)
		if (encounterType == EncounterType.AggroChase)
		{
			if (state == State.Wander && distToPlayer <= detectionRange)
				state = State.Chase;

			if (state == State.Chase && distToPlayer >= loseInterestRange)
				state = State.Wander;
		}
		else
		{
			// PassiveTouch mode = just wander (no forced chase)
			state = State.Wander;
		}
	}

	private void FixedUpdate()
	{
		if (!player) { rb.linearVelocity = Vector2.zero; return; }

		if (state == State.Chase)
		{
			DoChaseMovement();
		}
		else
		{
			DoWanderMovement();
		}
	}

	private void DoChaseMovement()
	{
		float dist = Vector2.Distance(rb.position, player.position);
		float engageDist = GetEngageDistance();

		// If we have ranged ability and not preferring melee, we can keep distance
		bool isRanged = (combat != null && combat.canShoot);
		bool prefersMelee = (combat != null && combat.preferMelee);

		Vector2 toPlayer = (Vector2)player.position - rb.position;

		// Optional: back off if ranged and player is too close (prevents "hugging")
		if (allowBackOffForRanged && isRanged && !prefersMelee && dist < engageDist * backOffThreshold)
		{
			Vector2 away = (-toPlayer).sqrMagnitude < 0.0001f ? Vector2.zero : (-toPlayer).normalized;
			rb.linearVelocity = away * moveSpeed;
			return;
		}

		// Move in until engage distance, then stop so EnemyCombat can attack
		if (dist > engageDist)
		{
			MoveTowards((Vector2)player.position);
		}
		else
		{
			rb.linearVelocity = Vector2.zero;
		}
	}

	private void DoWanderMovement()
	{
		MoveTowards(wanderTarget);

		wanderTimer -= Time.fixedDeltaTime;

		float dist = Vector2.Distance(rb.position, wanderTarget);
		if (dist <= wanderPointReachDist || wanderTimer <= 0f)
		{
			PickNewWanderTarget();
			wanderTimer = wanderRepathTime;
		}
	}

	private void MoveTowards(Vector2 targetPos)
	{
		Vector2 dir = (targetPos - rb.position);
		if (dir.sqrMagnitude < 0.0001f)
		{
			rb.linearVelocity = Vector2.zero;
			return;
		}

		dir = dir.normalized;
		rb.linearVelocity = dir * moveSpeed;
	}

	private void PickNewWanderTarget()
	{
		Vector2 random = Random.insideUnitCircle * wanderRadius;
		wanderTarget = spawnPos + random;
	}

	private float GetEngageDistance()
	{
		if (!useEnemyCombatRanges || combat == null)
			return Mathf.Max(0.05f, engageDistanceFallback);

		// Prefer melee range if melee is available and enemy prefers melee
		if (combat.canMelee && combat.preferMelee)
			return Mathf.Max(0.05f, combat.meleeRange * 0.95f);

		// If only ranged, stay near shoot range (but slightly inside so it can keep firing)
		if (combat.canShoot && !combat.canMelee)
			return Mathf.Max(0.05f, combat.shootRange * 0.75f);

		// If it can do both but doesn't prefer melee, keep a mid distance
		if (combat.canShoot)
			return Mathf.Max(0.05f, combat.shootRange * 0.65f);

		// fallback
		return Mathf.Max(0.05f, engageDistanceFallback);
	}

	private void OnDrawGizmosSelected()
	{
		// Wander area (around spawn once playing; otherwise around current pos)
		Gizmos.color = Color.cyan;
		Vector3 center = Application.isPlaying ? (Vector3)spawnPos : transform.position;
		Gizmos.DrawWireSphere(center, wanderRadius);

		if (encounterType == EncounterType.AggroChase)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, detectionRange);

			Gizmos.color = new Color(1f, 0.5f, 0f);
			Gizmos.DrawWireSphere(transform.position, loseInterestRange);
		}

		// Engage distance (runtime only accuracy if EnemyCombat exists)
		Gizmos.color = Color.yellow;
		float engage = Application.isPlaying ? GetEngageDistance() : engageDistanceFallback;
		Gizmos.DrawWireSphere(transform.position, engage);
	}
}
