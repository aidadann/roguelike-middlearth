using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMovement : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float moveSpeed = 5f;

	[Header("World Reference")]
	[SerializeField] private WorldGrid worldGrid;
	[SerializeField] private CaveManager caveManager;

	private Rigidbody2D rb;
	private Vector2 moveInput;     // raw input
	private Vector2 moveVector;    // normalized direction
	private Vector2 overworldReturnPosition;
	private bool ignoreCaveEntrance = false;

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

		bool canMove = false;

		if (caveManager.CurrentState == GameWorldState.Cave)
		{
			canMove = caveManager.IsWalkable(targetPos);
		}
		else
		{
			canMove = worldGrid.IsWalkable(targetPos);
		}

		if (canMove)
		{
			rb.MovePosition(targetPos);
		}


		CheckCaveEntrance();
		CheckCaveExit();
		Debug.Log("Checking cave entrance");
	}


	// Useful if you want facing direction for attacks/animations later
	public Vector2 GetFacingDirection()
	{
		if (moveVector.sqrMagnitude > 0.0001f) return moveVector.normalized;
		return Vector2.down; // default facing
	}

	private void CheckCaveEntrance()
	{
		if (ignoreCaveEntrance)
			return;

		Vector2Int gridPos = worldGrid.WorldToGrid(rb.position);

		if (worldGrid.GetTile(gridPos.x, gridPos.y) == WorldGrid.TileType.CaveEntrance)
		{
			int caveSeed = worldGrid.worldSeed + gridPos.x * 1000 + gridPos.y;
			EnterCave(caveSeed);
		}
	}


	private void EnterCave(int caveSeed)
	{
		overworldReturnPosition = rb.position;
		
		rb.position += Vector2.up * 0.5f; 
		
		caveManager.EnterCave(caveSeed);

		rb.position = caveManager.GetCaveSpawnPosition();

		// OPTIONAL: disable overworld visuals
		worldGrid.gameObject.SetActive(false);
	}

	private void CheckCaveExit()
	{
		if (caveManager.CurrentState != GameWorldState.Cave)
			return;

		Vector2Int gridPos = new Vector2Int(
			Mathf.FloorToInt(rb.position.x),
			Mathf.FloorToInt(rb.position.y)
		);

		if (caveManager.IsExitTile(gridPos))
		{
			ExitCave();
		}
	}
	private void ExitCave()
	{
		caveManager.ExitCave();

		rb.position = overworldReturnPosition;

		// Prevent immediate re-entry
		ignoreCaveEntrance = true;

		// Re-enable entrance after short delay
		Invoke(nameof(EnableCaveEntrance), 0.2f);
	}

	private void EnableCaveEntrance()
	{
		ignoreCaveEntrance = false;
	}
}
