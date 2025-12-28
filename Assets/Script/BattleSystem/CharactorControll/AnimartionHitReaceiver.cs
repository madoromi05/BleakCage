using UnityEngine;

public class AnimationHitReceiver : MonoBehaviour
{
    private IAttackHitNotifier notifier;

    public void Setup(IAttackHitNotifier notifier)
    {
        this.notifier = notifier;
    }

    public void OnAnimationHit()
    {
        notifier?.NotifyAttackHit();
    }
}
