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

	[Tooltip("How fast the enemy accelerates toward the desired velocity.")]
	public float accel = 25f;

	[Tooltip("How fast the enemy brakes to a stop.")]
	public float decel = 35f;

	[Header("Wander")]
	public float wanderRadius = 3f;
	public float wanderPointReachDist = 0.2f;
	public float wanderRepathTime = 2.0f;

	[Tooltip("Idle pause when reaching a wander point (min seconds).")]
	public float wanderIdleMin = 0.2f;

	[Tooltip("Idle pause when reaching a wander point (max seconds).")]
	public float wanderIdleMax = 0.9f;

	[Tooltip("Bias wander targets to be a bit more in the current moving direction (0..1).")]
	[Range(0f, 1f)]
	public float wanderForwardBias = 0.35f;

	[Tooltip("Smooth noise amplitude added to wander targets (world units).")]
	public float wanderNoiseAmplitude = 0.6f;

	[Tooltip("Smooth noise speed (higher = changes faster).")]
	public float wanderNoiseSpeed = 0.25f;

	[Header("Aggro")]
	public float detectionRange = 5f;
	public float loseInterestRange = 7f;

	[Header("Detection (Line of Sight)")]
	public bool requireLineOfSight = true;
	public LayerMask obstacleMask;

	[Header("Decision Timing")]
	public float reactionInterval = 0.12f;
	public float chaseCommitDuration = 0.9f;
	public float lostSightGraceTime = 0.6f;

	[Header("Combat Spacing")]
	[Tooltip("How much extra distance to keep so melee enemies don't physically touch the player.")]
	public float meleeStopBuffer = 0.25f;

	[Tooltip("Ranged: preferred distance as a fraction of shootRange (0.5–0.85 feels good).")]
	[Range(0.2f, 0.95f)]
	public float rangedPreferredFrac = 0.7f;

	[Tooltip("Ranged: width of the distance band (fraction of preferred distance).")]
	[Range(0.05f, 0.5f)]
	public float rangedBandFrac = 0.18f;

	[Tooltip("Ranged orbit strength (how much sideways movement while in range).")]
	[Range(0f, 2f)]
	public float orbitStrength = 0.9f;

	[Tooltip("How often the enemy flips orbit direction (seconds).")]
	public float orbitSwitchMin = 1.2f;
	public float orbitSwitchMax = 2.5f;

	[Tooltip("Small noise to prevent perfect circles.")]
	public float orbitNoise = 0.15f;

	private Rigidbody2D rb;
	private EnemyCombat combat; // optional but recommended

	private Vector2 spawnPos;
	private Vector2 wanderTarget;
	private float wanderTimer;

	private float wanderIdleTimer;
	private bool isWanderIdling;

	private enum State { Wander, Chase }
	private State state = State.Wander;

	// Decision timing
	private float nextSenseTime;
	private float chaseCommitUntil;
	private float lastSeenTime;
	private Vector2 cachedChaseTarget;

	// Noise seeds
	private float noiseSeedX;
	private float noiseSeedY;

	// Orbit
	private int orbitSign = 1;
	private float nextOrbitFlipTime;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.gravityScale = 0f;
		rb.freezeRotation = true;

		combat = GetComponent<EnemyCombat>();

		spawnPos = transform.position;

		noiseSeedX = Random.Range(0f, 1000f);
		noiseSeedY = Random.Range(0f, 1000f);

		orbitSign = Random.value < 0.5f ? -1 : 1;
		nextOrbitFlipTime = Time.time + Random.Range(orbitSwitchMin, orbitSwitchMax);

		PickNewWanderTarget();
		wanderTimer = wanderRepathTime;

		isWanderIdling = false;
		wanderIdleTimer = 0f;

		nextSenseTime = 0f;
		chaseCommitUntil = -999f;
		lastSeenTime = -999f;
		cachedChaseTarget = player ? (Vector2)player.position : rb.position;
	}

	private void Update()
	{
		if (!player) return;

		if (encounterType != EncounterType.AggroChase)
		{
			state = State.Wander;
			return;
		}

		if (Time.time < nextSenseTime) return;
		nextSenseTime = Time.time + Mathf.Max(0.02f, reactionInterval);

		Vector2 myPos = rb.position;
		Vector2 playerPos = (Vector2)player.position;
		float dist = Vector2.Distance(myPos, playerPos);

		bool canSee = (!requireLineOfSight) || HasLOS(myPos, playerPos);
		bool detected = (dist <= detectionRange) && canSee;

		if (detected)
		{
			cachedChaseTarget = playerPos;
			lastSeenTime = Time.time;

			if (state == State.Wander)
			{
				state = State.Chase;
				chaseCommitUntil = Time.time + Mathf.Max(0f, chaseCommitDuration);
			}
			else
			{
				chaseCommitUntil = Mathf.Max(chaseCommitUntil, Time.time + 0.1f);
			}
		}

		if (state == State.Chase)
		{
			bool commitActive = Time.time < chaseCommitUntil;
			bool recentlySeen = (Time.time - lastSeenTime) <= Mathf.Max(0f, lostSightGraceTime);

			if (!commitActive && !recentlySeen && dist >= loseInterestRange)
			{
				state = State.Wander;
				StartWanderIdle();
			}
		}
	}

	private void FixedUpdate()
	{
		if (!player)
		{
			BrakeToStop();
			return;
		}

		if (state == State.Chase) DoChaseMovement();
		else DoWanderMovement();
	}

	private bool HasLOS(Vector2 origin, Vector2 target)
	{
		if (obstacleMask.value == 0) return true;

		Vector2 dir = (target - origin);
		float dist = dir.magnitude;
		if (dist < 0.001f) return true;

		RaycastHit2D hit = Physics2D.Raycast(origin, dir.normalized, dist, obstacleMask);
		return hit.collider == null;
	}

	// =========================
	// NEW: Natural aggro movement
	// =========================
	private void DoChaseMovement()
	{
		Vector2 myPos = rb.position;
		Vector2 playerPos = (Vector2)player.position;

		// Less perfect tracking (uses cached target updated on reaction ticks)
		Vector2 targetPos = cachedChaseTarget;

		// Orbit direction flip occasionally
		if (Time.time >= nextOrbitFlipTime)
		{
			orbitSign *= -1;
			nextOrbitFlipTime = Time.time + Random.Range(orbitSwitchMin, orbitSwitchMax);
		}

		bool canMelee = combat != null && combat.canMelee;
		bool canShoot = combat != null && combat.canShoot;

		bool meleeOnly = canMelee && !canShoot;
		bool rangedOnly = canShoot && !canMelee;
		bool hybrid = canMelee && canShoot;

		Vector2 toPlayer = playerPos - myPos;
		float distToPlayer = toPlayer.magnitude;
		Vector2 toPlayerDir = distToPlayer < 0.0001f ? Vector2.right : (toPlayer / distToPlayer);

		// Hybrid rule: shoot at distance, melee if player too close
		if (hybrid)
		{
			float meleeStartDist = Mathf.Max(0.05f, combat.meleeRange + meleeStopBuffer);

			if (distToPlayer <= meleeStartDist)
			{
				// behave like melee: close but don't touch
				MeleeApproach(myPos, playerPos, toPlayerDir, meleeStartDist);
				return;
			}
			else
			{
				// behave like ranged hover: keep distance and orbit while shooting
				RangedHover(myPos, playerPos, toPlayerDir, combat.shootRange);
				return;
			}
		}

		if (meleeOnly)
		{
			float stopDist = Mathf.Max(0.05f, combat.meleeRange + meleeStopBuffer);
			MeleeApproach(myPos, playerPos, toPlayerDir, stopDist);
			return;
		}

		if (rangedOnly)
		{
			RangedHover(myPos, playerPos, toPlayerDir, combat.shootRange);
			return;
		}

		// If no EnemyCombat attached, fall back to basic chase (stop a bit away)
		float fallbackStop = 0.9f + meleeStopBuffer;
		MeleeApproach(myPos, playerPos, toPlayerDir, fallbackStop);
	}

	private void MeleeApproach(Vector2 myPos, Vector2 playerPos, Vector2 toPlayerDir, float stopDist)
	{
		float dist = Vector2.Distance(myPos, playerPos);

		if (dist > stopDist)
		{
			ApplySteering(toPlayerDir * moveSpeed);
		}
		else
		{
			BrakeToStop();
		}
	}

	private void RangedHover(Vector2 myPos, Vector2 playerPos, Vector2 toPlayerDir, float shootRange)
	{
		float dist = Vector2.Distance(myPos, playerPos);

		// Preferred distance band
		float preferred = Mathf.Max(0.4f, shootRange * rangedPreferredFrac);
		float band = Mathf.Max(0.08f, preferred * rangedBandFrac);
		float minD = preferred - band;
		float maxD = preferred + band;

		// Tangent/orbit direction (perpendicular)
		Vector2 tangent = new Vector2(-toPlayerDir.y, toPlayerDir.x) * orbitSign;

		// Small noise so it doesn't look like a perfect orbit
		float nx = (Mathf.PerlinNoise(noiseSeedX, Time.time * 1.6f) - 0.5f) * 2f;
		float ny = (Mathf.PerlinNoise(noiseSeedY, Time.time * 1.6f) - 0.5f) * 2f;
		Vector2 noise = new Vector2(nx, ny) * orbitNoise;

		Vector2 desiredDir;

		if (dist < minD)
		{
			// Too close: drift outward (but not straight back every frame)
			desiredDir = (-toPlayerDir + tangent * 0.35f + noise * 0.15f).normalized;
		}
		else if (dist > maxD)
		{
			// Too far: move closer, but with a sideways component
			desiredDir = (toPlayerDir + tangent * 0.35f + noise * 0.15f).normalized;
		}
		else
		{
			// In the band: orbit/hover around naturally
			desiredDir = (tangent * orbitStrength + noise).normalized;
		}

		ApplySteering(desiredDir * moveSpeed);
	}

	private void DoWanderMovement()
	{
		if (isWanderIdling)
		{
			wanderIdleTimer -= Time.fixedDeltaTime;
			BrakeToStop();

			if (wanderIdleTimer <= 0f)
			{
				isWanderIdling = false;
				PickNewWanderTarget();
				wanderTimer = wanderRepathTime;
			}
			return;
		}

		Vector2 myPos = rb.position;
		Vector2 toTarget = (wanderTarget - myPos);

		if (toTarget.sqrMagnitude < wanderPointReachDist * wanderPointReachDist)
		{
			StartWanderIdle();
			return;
		}

		Vector2 dir = toTarget.normalized;
		ApplySteering(dir * moveSpeed);

		wanderTimer -= Time.fixedDeltaTime;
		if (wanderTimer <= 0f)
		{
			PickNewWanderTarget();
			wanderTimer = wanderRepathTime;
		}
	}

	private void StartWanderIdle()
	{
		isWanderIdling = true;
		wanderIdleTimer = Random.Range(Mathf.Min(wanderIdleMin, wanderIdleMax), Mathf.Max(wanderIdleMin, wanderIdleMax));
	}

	private void ApplySteering(Vector2 desiredVelocity)
	{
		float a = Mathf.Max(0.01f, accel);
		rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, a * Time.fixedDeltaTime);
	}

	private void BrakeToStop()
	{
		float d = Mathf.Max(0.01f, decel);
		rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, d * Time.fixedDeltaTime);
	}

	private void PickNewWanderTarget()
	{
		Vector2 currentVel = rb ? rb.linearVelocity : Vector2.zero;
		Vector2 forward = currentVel.sqrMagnitude > 0.05f ? currentVel.normalized : Random.insideUnitCircle.normalized;

		Vector2 random = Random.insideUnitCircle * wanderRadius;
		Vector2 biased = forward * (wanderRadius * wanderForwardBias * Random.Range(0.25f, 0.9f));

		float nx = Mathf.PerlinNoise(noiseSeedX, Time.time * wanderNoiseSpeed) - 0.5f;
		float ny = Mathf.PerlinNoise(noiseSeedY, Time.time * wanderNoiseSpeed) - 0.5f;
		Vector2 noise = new Vector2(nx, ny) * (2f * wanderNoiseAmplitude);

		Vector2 candidate = spawnPos + random + biased + noise;

		Vector2 offset = candidate - spawnPos;
		offset = Vector2.ClampMagnitude(offset, wanderRadius);
		wanderTarget = spawnPos + offset;
	}

	private void OnDrawGizmosSelected()
	{
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
	}
}
