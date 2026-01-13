using UnityEngine;

public static class WorldSeed
{
    private const string SeedKey = "WORLD_SEED";

    public static int GetOrCreateSeed()
    {
        if (PlayerPrefs.HasKey(SeedKey))
        {
            return PlayerPrefs.GetInt(SeedKey);
        }

        int newSeed = Random.Range(1, int.MaxValue);
        PlayerPrefs.SetInt(SeedKey, newSeed);
        PlayerPrefs.Save();
        return newSeed;
    }

    public static void ClearSeed()
    {
        PlayerPrefs.DeleteKey(SeedKey);
        PlayerPrefs.Save();
    }
}
