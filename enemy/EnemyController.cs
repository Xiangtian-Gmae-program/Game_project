using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;
//*****************************************
//创建人： Trigger 
//功能说明：
//***************************************** 
public class EnemyController : PlayerController
{
    public PlayerController target;
    public NavMeshAgent agent;
    public float attackCD;
    public float lastAttackTime;

    public override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        animator.SetFloat("MoveState", 1);
        //animator.SetFloat("MoveX",1);
        //animator.SetBool("***",true);
        //animator.SetInteger("***",-1);
        //animator.SetTrigger("***");
    }

    private void Update()
    {
        if (!isDead)
        {
            float dis = Vector3.Distance(target.transform.position, transform.position);
            if (dis<=2)//距离小于攻击距离，则攻击
            {
                transform.LookAt(new Vector3(target.transform.position.x,transform.position.y,target.transform.position.z));//一直面向玩家
                animator.SetFloat("MoveY", 0);
                agent.isStopped = true;
                if (Time.time-lastAttackTime>=attackCD)//如果攻击间隔大于攻击CD
                {
                    target.TakeDamage(1);
                    animator.SetTrigger("Attack");
                    lastAttackTime = Time.time;
                }                
            }
            else if(dis > 2 && dis <= 8)//进入追踪距离开始追踪
            {
                if (canMove)//非攻击状态下才可移动
                {
                    animator.SetFloat("MoveY", 1);
                    agent.SetDestination(target.transform.position);
                    agent.isStopped = false;
                }
                else//如果正在攻击即使满足追踪条件也要等当次攻击完成
                {
                    agent.isStopped = true;
                }
            }

        }
    }
}
