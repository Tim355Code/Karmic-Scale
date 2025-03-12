using UnityEngine;
using System.Collections;

// The thing you touch to load the next floor, spawns after defeating the boss
public class Exit : MonoBehaviour
{
    [SerializeField]
    private float StartY = 1f;
    [SerializeField]
    private float TravelY = 2f;
    [SerializeField]
    private float AnimationDuration = 2f;

    bool Triggered = false;
    bool CanBeTriggered = false;

    // Bad way to prevent you instantly going to next floor if it spawns on top of you
    private void Start()
    {
        Invoke(nameof(ResetTrigger), 1f);
    }

    void ResetTrigger()
    {
        CanBeTriggered = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (Triggered || !CanBeTriggered) return;

        if (collision.CompareTag("Player"))
        {
            GetComponent<Collider2D>().enabled = false;
            Triggered = true;

            PlayerMovement.LockControls = true;
            AudioManager.Instance.FadeOutMusic();
            GameMaster.Singleton.GoToNextFloor();

            StartCoroutine(AnimationCoroutine());
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) CanBeTriggered = true;
    }

    // Garbage way of animating the player "walking down" the stairs
    IEnumerator AnimationCoroutine()
    {
        var interpolator = new Interpolator<float>(AnimationDuration, transform.position.y + StartY, transform.position.y + StartY - TravelY);

        while (!interpolator.HasFinished)
        {
            PlayerMovement.Instance.transform.position = new Vector3(transform.position.x, interpolator.Update(Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }
    }
}
