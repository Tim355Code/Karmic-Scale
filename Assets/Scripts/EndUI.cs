using UnityEngine;
using TMPro;

// All this does is set the text on the end screen
public class EndUI : MonoBehaviour
{
    public static EndUI Instance;
    private bool LoadingGame;

    public TMP_Text EndText;

    private void Awake()
    {
        Instance = this;
    }

    public void SetEndText(GameStats stats)
    {
        string[] endings = new string[] { "EVIL", "PURE", "NEUTRAL" };
        string[] difficulty = new string[] { "NORMAL", "EASIER"};

        float minutes = (stats.EndTime - stats.StartTime) / 60f;

        EndText.text = $"Congratulations! You got the <b>{endings[stats.Ending]}</b> ending!\n\nCompleted on <b>{difficulty[stats.Difficulty]} difficulty.</b>\n\nYou played for <b>{minutes:#.00}</b> minutes.\nCollected <b>{stats.ItemCount}</b> items.\nGot hit <b>{stats.HitCount}</b> times.";
    }

    public void LoadMainMenu()
    {
        if (LoadingGame) return;
        LoadingGame = true;
        GameMaster.Singleton.LoadMenu();
    }
}
