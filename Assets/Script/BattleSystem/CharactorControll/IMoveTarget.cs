using System.Collections;
using UnityEngine;
public interface IMoveToTarget
{
    IEnumerator MoveToTarget(Vector3 targetPosition);
    IEnumerator ReturnToBase();
}
