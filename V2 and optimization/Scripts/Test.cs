// Script note: Small test script for manually checking Unity calls or Animator triggers.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：
//***************************************** 
// Class responsibility: Test contains this script's gameplay behavior.
public class Test : MonoBehaviour
{
    public int student;
    private float b;
    private bool a;
    public static bool isEvening;

    // Initializes gameplay state when the scene starts.
    void Start()
    {
        //Debug.Log(student);
        //Debug.Log(b);
        //Debug.Log(a);
    }

    // Runs per-frame input, state, AI, or UI updates.
    void Update()
    {

    }

    // Runs a manually triggered test action.
    public void TestTrigger()
    {
        Debug.Log("TestTrigger");
    }

    // Static test entry point.
    public static void LearnUnity()
    {
        Debug.Log("LearnUnity");
    }
}
