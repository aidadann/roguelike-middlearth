using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyCombat : MonoBehaviour
{
	[Header("References")]
	public Transform player;

	[Header("Melee")]
	public bool canMelee = true;
	public float meleeRange = 0.9f;
	public float meleeRadius = 0.35f;
	public int meleeDamage = 10;
	public float meleeCooldown = 0.8f;
	public LayerMask meleeHitMask; // Player layer

	[Header("Melee FX (Slash Sprite)")]
	public MeleeSlashFX slashPrefab;          // same prefab you used for player
	public float slashDistance = 0.8f;
	public float slashLife = 0.12f;
	public float slashScale = 1.0f;
	public float slashRotateExtraDeg = 0f;

	[Header("Ranged")]
	public bool canShoot = true;
	public float shootRange = 5f;
	public float shootCooldown = 1.2f;
	public int projectileDamage = 8;
	public float projectileSpeed = 10f;
	public Projectile2D projectilePrefab;
	public Transform firePoint;
	public LayerMask projectileHitMask; // Player layer

	[Header("Behavior")]
	[Tooltip("If both are available, enemy prefers melee when close enough.")]
	public bool preferMelee = true;

	[Header("Debug")]
	public bool debugLogs = false;

	private float meleeTimer;
	private float shootTimer;

	private void Update()
	{
		if (!player) return;

		meleeTimer -= Time.deltaTime;
		shootTimer -= Time.deltaTime;

		float dist = Vector2.Distance(transform.position, player.position);

		bool inMelee = canMelee && dist <= meleeRange;
		bool inShoot = canShoot && dist <= shootRange;

		// HYBRID PRIORITY (your requested behavior):
		// - If close -> melee
		// - Else if in range -> shoot
		if (canMelee && canShoot)
		{
			if (inMelee) { TryMelee(); return; }
			if (inShoot) { TryShoot(); return; }
			return;
		}

		// Original logic for single-mode enemies
		if (preferMelee && inMelee)
		{
			TryMelee();
		}
		else if (inShoot)
		{
			if (inMelee && canMelee && meleeTimer <= 0f) TryMelee();
			else TryShoot();
		}
		else if (inMelee)
		{
			TryMelee();
		}
	}

	private void TryMelee()
	{
		if (meleeTimer > 0f) return;
		meleeTimer = meleeCooldown;

		Vector2 dir = ((Vector2)(player.position - transform.position));
		if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
		dir.Normalize();

		// Spawn slash visual (same as player)
		if (slashPrefab != null)
		{
			var fx = Instantiate(slashPrefab);
			fx.lifeTime = slashLife;
			fx.arcDistance = slashDistance;
			fx.arcScale = slashScale;
			fx.rotateExtraDeg = slashRotateExtraDeg;
			fx.Init(transform, dir);
		}

		// Damage zone in front of enemy
		Vector2 center = (Vector2)transform.position + dir * meleeRange;

		Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeRadius, meleeHitMask);

		if (debugLogs)
		{
			Debug.Log($"{name} melee: hits={hits.Length}, mask={meleeHitMask.value}", this);
			if (hits.Length == 0)
				Debug.LogWarning($"{name} melee: no colliders found. Check meleeHitMask includes Player layer and Player has Collider2D.", this);
		}

		foreach (var h in hits)
		{
			if (h.TryGetComponent<IDamageable>(out var dmg))
			{
				dmg.TakeDamage(meleeDamage);

				if (debugLogs)
					Debug.Log($"{name} melee hit: {h.name} for {meleeDamage}", this);
			}
			else if (debugLogs)
			{
				Debug.LogWarning($"{name} melee overlap: {h.name} has no IDamageable (does Player have Health implementing IDamageable?)", this);
			}
		}
	}

	private void TryShoot()
	{
		if (shootTimer > 0f) return;
		if (!projectilePrefab || !firePoint) return;

		shootTimer = shootCooldown;

		Vector2 dir = (player.position - firePoint.position);
		if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
		dir.Normalize();

		var proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
		proj.hitMask = projectileHitMask;
		proj.Init(dir, transform, projectileDamage, projectileSpeed);
	}

	private void OnDrawGizmosSelected()
	{
		if (!canMelee) return;

		// 1) Show melee RANGE around the enemy (how close player must be to trigger melee)
		Gizmos.color = new Color(1f, 0.6f, 0f, 1f); // orange
		Gizmos.DrawWireSphere(transform.position, meleeRange);

		// 2) Decide a direction to place the melee HIT circle
		Vector2 dir = Vector2.right;

		if (player != null)
		{
			dir = (Vector2)(player.position - transform.position);
			if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
			dir.Normalize();
		}

		// 3) Show melee HIT RADIUS circle in front of the enemy (actual damage area)
		Vector2 center = (Vector2)transform.position + dir * meleeRange;

		Gizmos.color = new Color(1f, 1f, 0f, 1f); // yellow
		Gizmos.DrawWireSphere(center, meleeRadius);

		// 4) Draw a line to the hit center for clarity
		Gizmos.color = new Color(1f, 1f, 1f, 0.8f);
		Gizmos.DrawLine(transform.position, center);
	}

}
