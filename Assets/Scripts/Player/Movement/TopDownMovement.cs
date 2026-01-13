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
	[SerializeField] private DungeonManager dungeonManager;

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

		// Recommended for top-down movement to reduce unwanted sliding
		rb.linearDamping = 0f;
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
		// KEY FIX: cancel any physics shove/drift every tick
		rb.linearVelocity = Vector2.zero;
		rb.angularVelocity = 0f;

		if (moveVector == Vector2.zero) return;

		bool canMove = false;

		if (caveManager.CurrentState == GameWorldState.Cave)
		{
			canMove = caveManager.IsWalkable(targetPos);
		}
		else if (dungeonManager.CurrentState == GameWorldState.Dungeon)
		{
			canMove = dungeonManager.IsWalkable(targetPos);
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
		CheckDungeonEntrance();
		CheckDungeonExit();
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

	private void CheckDungeonEntrance()
	{
		if (ignoreDungeonEntrance)
			return;

		Vector2Int gridPos = worldGrid.WorldToGrid(rb.position);

		if (worldGrid.GetTile(gridPos.x, gridPos.y) == WorldGrid.TileType.DungeonEntrance)
		{
			int dungeonSeed = worldGrid.worldSeed + gridPos.x * 2000 + gridPos.y;
			EnterDungeon(dungeonSeed);
		}
	}

	private void EnterDungeon(int dungeonSeed)
	{
		overworldReturnPosition = rb.position;
		
		rb.position += Vector2.up * 0.5f; 
		
		dungeonManager.EnterDungeon(dungeonSeed);

		rb.position = dungeonManager.GetDungeonSpawnPosition();
	}

	private void CheckDungeonExit()
	{
		if (dungeonManager.CurrentState != GameWorldState.Dungeon)
			return;

		Vector2Int gridPos = new Vector2Int(
			Mathf.FloorToInt(rb.position.x),
			Mathf.FloorToInt(rb.position.y)
		);

		if (dungeonManager.IsExit(gridPos))
		{
			ExitDungeon();
		}
	}
	private void ExitDungeon()
	{
		dungeonManager.ExitDungeon();

		rb.position = overworldReturnPosition;

		// Prevent immediate re-entry
		ignoreDungeonEntrance = true;

		// Re-enable entrance after short delay
		Invoke(nameof(EnableDungeonEntrance), 0.2f);
	}

	private bool ignoreDungeonEntrance = false;
	private void EnableDungeonEntrance()
	{
		ignoreDungeonEntrance = false;
	}

}