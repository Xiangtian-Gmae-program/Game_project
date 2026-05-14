# What we learned: Physics Callbacks and Singleton Pattern in a damage chain

Group: Awake()
Scripts studied: Damage.cs, Health.cs, Enemy.cs, GameManager.cs, UIManager.cs

---

## Concept 1 — Physics Callbacks: OnTriggerEnter2D

When we first read Damage.cs, we looked for where DealDamage() gets called from.
Tracing it back led us to OnTriggerEnter2D() — a function that nobody in the project
calls directly. Unity calls it automatically when two 2D colliders overlap. This was
the entry point we were not expecting: the entire damage chain starts not from our
code, but from a physics event the engine fires on its own.

### Code evidence

```csharp
// Damage.cs — Unity calls this when a collider enters the trigger
private void OnTriggerEnter2D(Collider2D collision)
{
    if (dealDamageOnTriggerEnter)
    {
        DealDamage(collision.gameObject);
    }
}
```

---

## Concept 2 — Singleton Pattern

We noticed that the event chain reaches GameManager and UIManager through
GameManager.instance and UIManager.instance. This is the Singleton pattern —
it means only one instance of that class is allowed to exist at a time, and
any other script can access it directly without needing a reference assigned
in the Inspector. Both GameManager.cs and UIManager.cs set this up inside
Awake(). If a second instance tries to start, it destroys itself immediately.

### Code evidence

```csharp
// GameManager.cs — Singleton setup in Awake()
public static GameManager instance = null;

private void Awake()
{
    if (instance == null)
    {
        instance = this;
    }
    else
    {
        DestroyImmediate(this);
    }
}
```

---

## Event chain

```
Unity physics event: two colliders overlap
→ Damage.OnTriggerEnter2D(collision)        [Damage.cs]
→ DealDamage(collision.gameObject)          [Damage.cs]
→ Health.TakeDamage(damageAmount)           [Health.cs]
→ CheckDeath()                              [Health.cs]
→ Die()                                     [Health.cs]

If the damaged object is an enemy:
→ Enemy.DoBeforeDestroy()                   [Enemy.cs]
→ GameManager.AddScore(scoreValue)          [GameManager.cs]
→ GameManager.UpdateUIElements()            [GameManager.cs]
→ UIManager.instance.UpdateUI()             [UIManager.cs]
→ score changes on screen

If the damaged object is the player:
→ GameManager.instance.GameOver()           [GameManager.cs]
→ UIManager.instance.GoToPage(gameOverPageIndex) [UIManager.cs]
→ game over screen appears
```

---

## Why this matters

Before reading these scripts, we assumed damage handling would be centralized —
one script checking every frame whether something had been hit. Instead, we found
that a single physics event silently passes responsibility across five scripts
without any of them needing to know the full picture. Damage.cs only knows it
hit something. Health.cs only knows it lost health. GameManager only knows a
score changed. Each script reacts to what it receives and passes the result on.

The Singleton pattern is what makes this handoff possible across unrelated scripts.
GameManager.instance and UIManager.instance give any script a direct line to those
managers without any setup in the Inspector. The risk is that this convenience
depends entirely on the instance being there — if the GameObject is destroyed,
every script that calls GameManager.instance crashes silently. That is why the
project consistently guards calls with if (GameManager.instance != null).

We also noticed a precise distinction in Damage.cs: collision.gameObject is the
object that was hit, while gameObject is the object owning the Damage script.
Swapping these two would send damage to the wrong target with no error message,
making it a hard bug to find without knowing how Unity separates the two.

---

## Improvement idea

In Damage.cs, OnTriggerStay2D() currently applies damage every physics step
while objects remain in contact, which may be too fast and difficult to balance.
A better approach is to add a configurable damage tick interval so that
damage-over-time is predictable and tunable from the Inspector.

```csharp
// Damage.cs
[Header("Damage Over Time")]
public float damageTickInterval = 0.2f;
private float nextDamageTime = 0f;

private void OnTriggerStay2D(Collider2D collision)
{
    if (!dealDamageOnTriggerStay) return;
    if (Time.time < nextDamageTime) return;

    DealDamage(collision.gameObject);
    nextDamageTime = Time.time + damageTickInterval;
}
```

This keeps damage-over-time behavior consistent across frame rates and gives
designers clear control over how often damage is applied while objects remain
in contact.

---

## Sources

- [Unity Scripting API: MonoBehaviour](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.html) — https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.html
- [Unity Manual: Event function execution order](https://docs.unity3d.com/6000.0/Documentation/Manual/execution-order.html) — https://docs.unity3d.com/6000.0/Documentation/Manual/execution-order.html
- [Unity Scripting API: OnTriggerEnter2D](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnTriggerEnter2D.html) — https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnTriggerEnter2D.html
- [Unity Scripting API: UnityEvent](https://docs.unity.cn/ScriptReference/Events.UnityEvent.html) — https://docs.unity.cn/ScriptReference/Events.UnityEvent.html
- [Unity Scripting API: Time.deltaTime](https://docs.unity.cn/ScriptReference/Time-deltaTime.html) — https://docs.unity.cn/ScriptReference/Time-deltaTime.html

---

## Reflection

Reading these scripts changed how we think about finding bugs. If damage
is not being applied, there is no point looking at TakeDamage() first —
the real question is whether OnTriggerEnter2D() fired at all, which depends
on collider settings, trigger flags, and team IDs. The event chain gives us
a map: if the chain breaks at any step, we know exactly where to look.

The part that surprised us most was how easy it is to mistake helper functions
for Unity events. DealDamage(), TakeDamage(), and DoBeforeDestroy() handle most
of the visible work, so they look like the important entry points — but Unity
never touches any of them. The only function Unity actually calls in this chain
is OnTriggerEnter2D(). Everything else is just one function calling the next.
