// Purpose: Locks and clears movement animation parameters during specific animation states.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockMoveState : StateMachineBehaviour
{
    private PlayerController pc;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        pc = animator.GetComponentInParent<PlayerController>();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        pc.canMove = false;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        pc.canMove = true;
    }

}

