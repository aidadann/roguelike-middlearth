using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
	public int maxHP = 50;
	public int currentHP;

	public System.Action<int, int> OnHPChanged; // current, max
	public System.Action OnDied;

	private void Awake()
	{
		currentHP = maxHP;
		OnHPChanged?.Invoke(currentHP, maxHP);
	}

	public void TakeDamage(int amount)
	{
		if (currentHP <= 0) return;

		currentHP -= Mathf.Abs(amount);
		currentHP = Mathf.Max(0, currentHP);

		OnHPChanged?.Invoke(currentHP, maxHP);

		if (currentHP == 0)
		{
			OnDied?.Invoke();
			// For now: simple kill
			Destroy(gameObject);
		}
	}
}
