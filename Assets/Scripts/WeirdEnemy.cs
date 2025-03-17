using UnityEngine;
using System.Collections;

// The guy who randomly flies, and then targets you if you're close
public class WeirdEnemy : Enemy
{
    private Rigidbody2D _rb2d;

    [SerializeField]
    private float MovementSpeed = 4;
    [SerializeField]
    private float StopTime = 1f;
    [SerializeField]
    private Vector2 MovementTime = new Vector2(0.1f, 0.8f);
    [SerializeField]
    private float PlayerDistance = 3f;

    private bool FirstCycle;
    private Vector2 Direction;

    protected override void Start()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        base.Start();

        StartCoroutine(Move());
    }

    protected override void FixedUpdate()
    {
        _rb2d.MovePosition(_rb2d.position + Direction * Time.deltaTime * MovementSpeed);

        base.FixedUpdate();
    }

    IEnumerator Move()
    {
        var toPlayer = PlayerMovement.Instance.transform.position - transform.position;

        if (!FirstCycle)
        {
            yield return new WaitForSeconds(Random.Range(StopTime / 2f, StopTime));
            FirstCycle = true;
        }
        else
            yield return new WaitForSeconds(StopTime);

        if (!CurrentEffects.ContainsKey(StatusEffect.FEAR) && toPlayer.magnitude < PlayerDistance) Direction = toPlayer.normalized;
        else Direction = Random.insideUnitCircle;

        yield return new WaitForSeconds(Random.Range(MovementTime.x, MovementTime.y));
        Direction = Vector2.zero;

        StartCoroutine(Move());
    }

    protected override void OnDeath()
    {
        StopAllCoroutines();
        base.OnDeath();
        Destroy(gameObject);
    }
}
