// Purpose: Handles legacy player attack collision and damage behavior.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public int damage = 25;
    public float attackHitDelay = 0.2f;
    public float attackRadius = 2f;

    [Header("References")]
    public Transform attackPoint;
    public string targetTag = "Enemy";

    private bool isAttackChecking = false;

    public void StartAttack()
    {
        Debug.Log("StartAttack called");

        if (isAttackChecking) return;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttackChecking = true;

        Debug.Log("AttackRoutine started");

        yield return new WaitForSeconds(attackHitDelay);

        if (attackPoint == null)
        {
            Debug.LogWarning("AttackPoint is missing.");
            isAttackChecking = false;
            yield break;
        }

        Debug.Log("Checking hit at: " + attackPoint.position);

        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            attackRadius,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Debug.Log("Hit count: " + hits.Length);

        HashSet<DummyHealth> damagedTargets = new HashSet<DummyHealth>();

        foreach (Collider col in hits)
        {
            Debug.Log("Hit collider: " + col.name);

            DummyHealth target = col.GetComponentInParent<DummyHealth>();

            if (target == null) continue;
            if (damagedTargets.Contains(target)) continue;

            if (!string.IsNullOrEmpty(targetTag) && !target.CompareTag(targetTag))
                continue;

            Debug.Log("Damage applied to: " + target.name);

            target.TakeDamage(damage);
            damagedTargets.Add(target);
        }

        yield return new WaitForSeconds(0.05f);
        isAttackChecking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}

