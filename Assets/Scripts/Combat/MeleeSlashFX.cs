using UnityEngine;

public class MeleeSlashFX : MonoBehaviour
{
	[Header("Lifetime")]
	public float lifeTime = 0.12f;

	[Header("Visual")]
	public float arcDistance = 0.8f;
	public float arcScale = 1.0f;
	public float rotateExtraDeg = 0f;

	[Tooltip("If true, the slash stays attached to the owner while it exists.")]
	public bool followOwner = true;

	private Transform owner;
	private Vector2 direction = Vector2.right;
	private Vector3 localOffset;

	public void Init(Transform ownerTransform, Vector2 dir)
	{
		owner = ownerTransform;
		direction = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;

		// Position in front of the owner
		Vector3 pos = owner.position + (Vector3)(direction * arcDistance);
		pos.z = 0f;
		transform.position = pos;

		// Save offset so it can follow owner cleanly
		localOffset = transform.position - owner.position;

		// Rotate to face direction
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle + rotateExtraDeg);

		transform.localScale = Vector3.one * arcScale;

		Destroy(gameObject, lifeTime);
	}

	private void Update()
	{
		if (followOwner && owner)
			transform.position = owner.position + localOffset;
	}
}
