using System.Numerics;
using ShapeEngine.Lib;

namespace ShapeEngine.Screen;

public class CameraTweenOffset : ICameraTween
{
    private float duration;
    private float timer;
    private TweenType tweenType;
    private Vector2 from;
    private Vector2 to;
    private Vector2 cur;
    public CameraTweenOffset(Vector2 from, Vector2 to, float duration, TweenType tweenType)
    {
        this.duration = duration;
        this.timer = 0f;
        this.tweenType = tweenType;
        this.from = from;
        this.to = to;
        this.cur = from;
    }

    public bool Update(float dt)
    {
        if (duration <= 0f) return true;
        float t = Clamp(timer / duration, 0f, 1f);
        timer += dt;
        cur = ShapeTween.Tween(from, to, t, tweenType);

        return t >= 1f;
    }

    public Vector2 GetOffset() { return cur; }
}