using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Controls the flow of scenes, holds the game stats so they can be transfered to end screen, handles loading between levels
public class GameMaster : MonoBehaviour
{
    public static GameMaster Singleton;

    public int CurrentFloor = 0;
    public int CurrentDifficulty = 0;

    public int StartFloor = 4;

    public GameStats Stats;

    void Awake()
    {
        if (Singleton == null)
        {
            CustomInput.Initialize();
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AudioManager.Instance.PlayMusic(Music.MENU);
        if (SceneManager.GetActiveScene().name == "Game") StartFirstFloor(0);
    }

    public void StartFirstFloor(int difficulty)
    {
        CurrentDifficulty = difficulty;
        StartCoroutine(StartFirstFloorCoroutine());
    }

    public void GoToNextFloor()
    {
        StartCoroutine(TransitionToNewFloor());
    }

    // Hacky way to get it to work no matter where you load from :)
    IEnumerator StartFirstFloorCoroutine()
    {
        PlayerMovement.LockControls = true;

        LoadedUI.Singleton.FadeInScreen();
        yield return new WaitForSeconds(1.1f);

        CurrentFloor = StartFloor;

        if (!SceneManager.GetSceneByName("Game").isLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
            yield return new WaitUntil(() => operation == null || operation.isDone);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Game"));
        }

        if (SceneManager.GetSceneByName("Menu").isLoaded)
        {
            var operation = SceneManager.UnloadSceneAsync(0);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        if (!SceneManager.GetSceneByName("Floor").isLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(2));

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        FloorManager.Instance.GenerateFloor(CurrentFloor);

        GameManager.Instance.OnFloorLoaded();

        LoadedUI.Singleton.FadeOutScreen();
        yield return new WaitForSeconds(0.5f);
        if (CurrentFloor < 3)
            AudioManager.Instance.PlayMusic(Music.FLOOR);
        else
            AudioManager.Instance.PlayMusic(Music.FLOOR_2);

        Stats = new GameStats { Difficulty = CurrentDifficulty, StartTime = Time.unscaledTime };
        PlayerMovement.LockControls = false;
    }

    IEnumerator TransitionToNewFloor()
    {
        PlayerMovement.LockControls = true;

        LoadedUI.Singleton.FadeInScreen();
        yield return new WaitForSeconds(1.1f);

        CurrentFloor++;

        var operation = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByBuildIndex(2));
        yield return new WaitUntil(() => operation.isDone);

        PlayerMovement.Instance.transform.position = Vector3.zero;
        GameUI.Instance.ClearMinimap();

        var operation2 = SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        yield return new WaitUntil(() => operation2.isDone);

        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(2));

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        FloorManager.Instance.GenerateFloor(CurrentFloor);

        GameManager.Instance.OnFloorLoaded();

        LoadedUI.Singleton.FadeOutScreen();

        yield return new WaitForSeconds(0.5f);
        if (CurrentFloor < 3)
            AudioManager.Instance.PlayMusic(Music.FLOOR);
        else
            AudioManager.Instance.PlayMusic(Music.FLOOR_2);

        PlayerMovement.LockControls = false;
    }

    public void LoadMenu(bool whiteFade = false)
    {
        AudioManager.Instance.SetMusicPitch(1f);
        StartCoroutine(LoadMenuCoroutine(whiteFade));
    }

    IEnumerator LoadMenuCoroutine(bool whiteFade)
    {
        PlayerMovement.LockControls = true;
        yield return new WaitForSeconds(3f);

        LoadedUI.Singleton.FadeInScreen(whiteFade);
        yield return new WaitForSeconds(1.1f);

        if (SceneManager.GetSceneByName("Floor").isLoaded)
        {
            var operation = SceneManager.UnloadSceneAsync(2);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        if (!SceneManager.GetSceneByName("Menu").isLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(0, LoadSceneMode.Additive);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        if (SceneManager.GetSceneByName("End").isLoaded)
        {
            var operation = SceneManager.UnloadSceneAsync(3);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        if (SceneManager.GetSceneByName("Game").isLoaded)
        {
            var operation = SceneManager.UnloadSceneAsync(1);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        AudioManager.Instance.PlayMusic(Music.MENU);
        LoadedUI.Singleton.FadeOutScreen(whiteFade);
    }

    public void LoadEndScreen(bool whiteFade)
    {
        Stats.ItemCount = GameManager.Instance.CollectedItems.Count;
        Stats.Ending = GameManager.Instance.LockedIn ? GameManager.Instance.Evil > 0 ? 0 : 1 : 2;
        Stats.EndTime = Time.unscaledTime;
        StartCoroutine(LoadEndScreenCoroutine(whiteFade));
    }

    public IEnumerator LoadEndScreenCoroutine(bool whiteFade)
    {
        PlayerMovement.LockControls = true;
        yield return new WaitForSeconds(1.5f);

        LoadedUI.Singleton.FadeInScreen(whiteFade);
        yield return new WaitForSeconds(1.1f);

        if (SceneManager.GetSceneByName("Floor").isLoaded)
        {
            var operation = SceneManager.UnloadSceneAsync(2);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        if (!SceneManager.GetSceneByName("End").isLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(3, LoadSceneMode.Additive);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        if (SceneManager.GetSceneByName("Game").isLoaded)
        {
            var operation = SceneManager.UnloadSceneAsync(1);
            yield return new WaitUntil(() => operation == null || operation.isDone);
        }

        LoadedUI.Singleton.FadeOutScreen(whiteFade);

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        EndUI.Instance.SetEndText(Stats);
    }
}

public struct GameStats
{
    public float StartTime;
    public float EndTime;
    public int HitCount;
    public int Ending;
    public int ItemCount;
    public int Difficulty;
}
