using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// This class is used to cover the screen in white or black between scenes being loaded
public class LoadedUI : MonoBehaviour
{
    public static LoadedUI Singleton;

    [SerializeField]
    private Image Cover;

    void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeInScreen(bool fadeWhite = false, float duration = 1f)
    {
        StartCoroutine(FadeCoroutine(duration, true, fadeWhite));
    }

    public void FadeOutScreen(bool fadeWhite = false, float duration = 1f)
    {
        StartCoroutine(FadeCoroutine(duration, false, fadeWhite));
    }

    IEnumerator FadeCoroutine(float duration, bool fadeIn, bool fadeWhite)
    {
        float startColor = fadeWhite ? 1 : 0;
        Cover.enabled = true;
        var interpolator = new Interpolator<float>(duration, fadeIn ? 0 : 1, fadeIn ? 1 : 0);

        while (!interpolator.HasFinished)
        {
            Cover.color = new Color(startColor, startColor, startColor, interpolator.Update(Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }

        if (!fadeIn) Cover.enabled = false;
    }
}
