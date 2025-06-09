using UnityEngine;

public class curvetest : MonoBehaviour
{
    public AnimationCurve plainCurve;
    public AnimationCurve mountainCurve;

    void Start()
    {
        plainCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.05f),
            new Keyframe(1f, 0.1f)
        );

        mountainCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.3f, 0.1f),
            new Keyframe(0.6f, 0.6f),
            new Keyframe(1f, 1f)
        );
    }

}
