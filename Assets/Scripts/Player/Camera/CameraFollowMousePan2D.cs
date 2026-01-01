using UnityEngine;

public class CameraFollowMouseLookAhead2D : MonoBehaviour
{
	[Header("Target")]
	[SerializeField] private Transform target;

	[Header("Follow")]
	[SerializeField] private Vector3 baseOffset = new Vector3(0f, 0f, -10f);
	[SerializeField] private float followSmoothTime = 0.15f;

	[Header("Mouse Look-Ahead")]
	[Tooltip("No camera shift while mouse is within this distance from player (world units).")]
	[SerializeField] private float deadzoneRadius = 1.25f;

	[Tooltip("Distance where look-ahead reaches maximum (world units). Beyond this, it stays max.")]
	[SerializeField] private float maxEffectDistance = 6f;

	[Tooltip("Maximum camera offset toward mouse direction (world units).")]
	[SerializeField] private float maxLookAheadOffset = 3f;

	[Tooltip("Smoothing for the look-ahead offset.")]
	[SerializeField] private float lookAheadSmoothTime = 0.08f;

	private Camera cam;
	private Vector3 followVelocity;
	private Vector3 lookVelocity;
	private Vector3 currentLookOffset;

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	private void LateUpdate()
	{
		if (!target || !cam) return;

		Vector3 desiredLook = ComputeMouseLookOffset();
		currentLookOffset = Vector3.SmoothDamp(currentLookOffset, desiredLook, ref lookVelocity, lookAheadSmoothTime);

		Vector3 desiredPos = target.position + baseOffset + currentLookOffset;
		transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVelocity, followSmoothTime);
	}

	private Vector3 ComputeMouseLookOffset()
	{
		// Convert mouse to world (for orthographic, z isn't important)
		Vector3 mouseScreen = Input.mousePosition;
		Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
		mouseWorld.z = target.position.z;

		Vector2 toMouse = (Vector2)(mouseWorld - target.position);
		float dist = toMouse.magnitude;

		// Inside deadzone: no offset
		if (dist <= deadzoneRadius || dist < 0.0001f)
			return Vector3.zero;

		Vector2 dir = toMouse / dist;

		// 0..1 strength based on how far the mouse is beyond deadzone
		float t = Mathf.InverseLerp(deadzoneRadius, maxEffectDistance, dist);
		// nicer ramp (optional)
		t = t * t; // ease-in

		Vector2 offset2D = dir * (t * maxLookAheadOffset);
		return new Vector3(offset2D.x, offset2D.y, 0f);
	}

	public void SetTarget(Transform t) => target = t;
}
