using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMovement : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float moveSpeed = 5f;

	private Rigidbody2D rb;
	private Vector2 moveInput;     // raw input
	private Vector2 moveVector;    // normalized direction

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.gravityScale = 0f;
		rb.freezeRotation = true;
	}

	private void Update()
	{
		// Keyboard / controller (old input system)
		float x = Input.GetAxisRaw("Horizontal"); // A/D, Left/Right
		float y = Input.GetAxisRaw("Vertical");   // W/S, Up/Down

		moveInput = new Vector2(x, y);

		// Normalize so diagonal isn't faster (8-direction balanced)
		moveVector = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
	}

	private void FixedUpdate()
	{
		Vector2 newPos = rb.position + moveVector * moveSpeed * Time.fixedDeltaTime;
		rb.MovePosition(newPos);
	}

	// Optional: for mobile joystick later
	public void SetMoveInput(Vector2 input)
	{
		moveInput = input;
		moveVector = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
	}

	// Useful if you want facing direction for attacks/animations later
	public Vector2 GetFacingDirection()
	{
		if (moveVector.sqrMagnitude > 0.0001f) return moveVector.normalized;
		return Vector2.down; // default facing
	}
}
