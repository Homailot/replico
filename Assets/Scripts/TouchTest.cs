using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchTest : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        StartCoroutine(testTouch());
    }

    IEnumerator testTouch()
    {
        yield return new WaitForSeconds(0.2f);
        Debug.Log(Input.touchCount);
    }
}
