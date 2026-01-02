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

		if (preferMelee && inMelee)
		{
			TryMelee();
		}
		else if (inShoot)
		{
			// If close enough and preferMelee=false, you might still melee — adjust logic as you like
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

		Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
		Vector2 center = (Vector2)transform.position + dir * meleeRange;

		Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeRadius, meleeHitMask);
		foreach (var h in hits)
		{
			if (h.TryGetComponent<IDamageable>(out var dmg))
				dmg.TakeDamage(meleeDamage);
		}
	}

	private void TryShoot()
	{
		if (shootTimer > 0f) return;
		if (!projectilePrefab || !firePoint) return;

		shootTimer = shootCooldown;

		Vector2 dir = (player.position - firePoint.position).normalized;
		var proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
		proj.hitMask = projectileHitMask;
		proj.Init(dir, transform, projectileDamage, projectileSpeed);
	}
}
