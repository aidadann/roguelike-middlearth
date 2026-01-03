using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
	public float speed = 10f;
	public float lifeTime = 2f;
	public int damage = 10;

	[Tooltip("Who fired it (so we don't hit self).")]
	public Transform owner;

	[Tooltip("Optional: restrict hits by layer mask (set in inspector).")]
	public LayerMask hitMask;

	private Vector2 direction = Vector2.right;

	public void Init(Vector2 dir, Transform ownerTransform, int dmg, float spd)
	{
		direction = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;
		owner = ownerTransform;
		damage = dmg;
		speed = spd;
	}

	private void Start()
	{
		Destroy(gameObject, lifeTime);
	}

	private void Update()
	{
		transform.position += (Vector3)(direction * speed * Time.deltaTime);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (owner && other.transform == owner) return;

		// If hitMask is set, only damage objects in that mask
		if (hitMask.value != 0 && (hitMask.value & (1 << other.gameObject.layer)) == 0)
			return;

		if (other.TryGetComponent<IDamageable>(out var dmg))
		{
			dmg.TakeDamage(damage);
			Destroy(gameObject);
		}
	}
}
