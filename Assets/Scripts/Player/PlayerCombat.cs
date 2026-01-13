using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
	[Header("Melee")]
	public float meleeRange = 1.0f;
	public float meleeRadius = 0.35f;
	public int meleeDamage = 15;
	public float meleeCooldown = 0.35f;
	public LayerMask meleeHitMask; // set to "Enemy" layer

	[Header("Melee FX (Slash Sprite)")]
	public MeleeSlashFX slashPrefab;          // prefab with SpriteRenderer + MeleeSlashFX
	public float slashDistance = 0.8f;        // how far in front of player
	public float slashLife = 0.12f;           // how long it stays
	public float slashScale = 1.0f;           // overall size
	public float slashRotateExtraDeg = 0f;    // tweak if sprite faces wrong direction

	[Header("Projectile")]
	public Projectile2D projectilePrefab;
	public Transform firePoint;  // empty child at player's center/front
	public int projectileDamage = 10;
	public float projectileSpeed = 12f;
	public float projectileCooldown = 0.25f;
	public LayerMask projectileHitMask; // set to "Enemy" layer

	private float meleeTimer;
	private float shootTimer;

	private Camera cam;

	private void Awake()
	{
		cam = Camera.main;
	}

	private void Update()
	{
		meleeTimer -= Time.deltaTime;
		shootTimer -= Time.deltaTime;

		// Left click = melee, Right click = shoot (change if you want)
		if (Input.GetMouseButtonDown(0))
			TryMelee();

		if (Input.GetMouseButton(1))
			TryShoot();
	}

	private void TryMelee()
	{
		if (meleeTimer > 0f) return;
		meleeTimer = meleeCooldown;

		Vector2 dir = GetAimDirection();

		// Spawn slash visual
		if (slashPrefab != null)
		{
			var fx = Instantiate(slashPrefab);
			fx.lifeTime = slashLife;
			fx.arcDistance = slashDistance;
			fx.arcScale = slashScale;
			fx.rotateExtraDeg = slashRotateExtraDeg;
			fx.Init(transform, dir);
		}

		// Damage
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

		shootTimer = projectileCooldown;

		Vector2 dir = GetAimDirection();
		var proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
		proj.hitMask = projectileHitMask;
		proj.Init(dir, transform, projectileDamage, projectileSpeed);
	}

	private Vector2 GetAimDirection()
	{
		if (!cam) cam = Camera.main;
		Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = transform.position.z;

		Vector2 dir = (Vector2)(mouseWorld - transform.position);
		if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
		return dir.normalized;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;

		// Draw approximate melee range circle in front of the player (using current mouse aim)
		if (Application.isPlaying)
		{
			Vector2 dir = GetAimDirection();
			Vector2 center = (Vector2)transform.position + dir * meleeRange;
			Gizmos.DrawWireSphere(center, meleeRadius);
		}
		else
		{
			Gizmos.DrawWireSphere(transform.position, 0.1f);
		}
	}
}
