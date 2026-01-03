using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMovement : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float moveSpeed = 5f;

	[Header("World Reference")]
	[SerializeField] private WorldGrid worldGrid;

	private Rigidbody2D rb;
	private Vector2 moveInput;     // raw input
	private Vector2 moveVector;    // normalized direction

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.gravityScale = 0f;
		rb.freezeRotation = true;
	}

	private void Start()
	{
		if (worldGrid != null)
		{
			Vector2 spawnPos = worldGrid.GetSafeSpawnWorldPosition();
			rb.position = spawnPos;
		}
	}
	private void Update()
	{
		Vector2 input = Vector2.zero;

		if (Keyboard.current != null)
		{
			if (Keyboard.current.aKey.isPressed) input.x -= 1;
			if (Keyboard.current.dKey.isPressed) input.x += 1;
			if (Keyboard.current.sKey.isPressed) input.y -= 1;
			if (Keyboard.current.wKey.isPressed) input.y += 1;
		}

		moveInput = input;
		moveVector = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
	}

	private void FixedUpdate()
	{
		if (moveVector == Vector2.zero) return;

		Vector2 targetPos =
			rb.position + moveVector * moveSpeed * Time.fixedDeltaTime;

		if (worldGrid == null || worldGrid.IsWalkable(targetPos))
		{
			rb.MovePosition(targetPos);
		}
	}


	// Useful if you want facing direction for attacks/animations later
	public Vector2 GetFacingDirection()
	{
		if (moveVector.sqrMagnitude > 0.0001f) return moveVector.normalized;
		return Vector2.down; // default facing
	}
}
