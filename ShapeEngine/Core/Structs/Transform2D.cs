
using System.Numerics;
using ShapeEngine.Lib;

namespace ShapeEngine.Core.Structs;


public readonly struct Transform2D : IEquatable<Transform2D>
{
    #region Members
    public readonly Vector2 Position;
    public readonly float RotationRad;
    public readonly Size BaseSize;
    public readonly Size ScaledSize;
    public readonly Vector2 Scale2d;
    
    /// <summary>
    /// Is the same as ScaleX. Can be used if only 1 component scale is needed.
    /// </summary>
    public float Scale => Scale2d.X;
    public float ScaleX => Scale2d.X;
    public float ScaleY => Scale2d.Y;
    
    
    public float RotationDeg => RotationRad * ShapeMath.RADTODEG;
    public Vector2 GetDirection() => ShapeVec.VecFromAngleRad(RotationRad);
    #endregion
    
    #region Constructors
    public Transform2D()
    {
        this.Position = new(0f);
        this.RotationRad = 0f;
        this.BaseSize = new(0f);
        this.Scale2d = new(1f, 1f);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(Vector2 pos)
    {
        this.Position = pos;
        this.RotationRad = 0f;
        this.BaseSize = new(0f);
        this.Scale2d = new(1f, 1f);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(float rotRad)
    {
        this.Position = new(0f);
        this.RotationRad = rotRad;
        this.BaseSize = new(0f);
        this.Scale2d = new(1f, 1f);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(Vector2 pos, float rotRad)
    {
        this.Position = pos; 
        this.RotationRad = rotRad;
        this.BaseSize = new(0f);
        this.Scale2d = new(1f, 1f);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(Vector2 pos, float rotRad, float scale)
    {
        this.Position = pos; 
        this.RotationRad = rotRad;
        this.BaseSize = new(0f);
        this.Scale2d = new(scale, scale);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(Vector2 pos, float rotRad, Size baseSize)
    {
        this.Position = pos; 
        this.RotationRad = rotRad; 
        this.BaseSize = baseSize;
        this.Scale2d = new(1f, 1f);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(Vector2 pos, float rotRad, Size baseSize, float scale)
    {
        this.Position = pos; 
        this.RotationRad = rotRad; 
        this.BaseSize = baseSize;
        this.Scale2d = new(scale, scale);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    
    public Transform2D(Vector2 pos, float rotRad, float scaleX, float scaleY)
    {
        this.Position = pos; 
        this.RotationRad = rotRad;
        this.BaseSize = new(0f);
        this.Scale2d = new(scaleX, scaleY);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(Vector2 pos, float rotRad, Size baseSize, float scaleX, float scaleY)
    {
        this.Position = pos; 
        this.RotationRad = rotRad; 
        this.BaseSize = baseSize;
        this.Scale2d = new(scaleX, scaleY);
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    public Transform2D(Vector2 pos, float rotRad, Size baseSize, Vector2 scale2d)
    {
        this.Position = pos; 
        this.RotationRad = rotRad; 
        this.BaseSize = baseSize;
        this.Scale2d = scale2d;
        this.ScaledSize = this.BaseSize * this.Scale2d;
    }
    #endregion

    #region Lerp

    public Transform2D Lerp(Transform2D to, float f)
    {
        return new Transform2D
        (
            Position.Lerp(to.Position, f),
            ShapeMath.LerpFloat(RotationRad, to.RotationRad, f),
            BaseSize.Lerp(to.BaseSize, f)
        );
    }
    public Transform2D PowLerp(Transform2D to, float remainder, float dt)
    {
        var scalar = MathF.Pow(remainder, dt);
        
        return new Transform2D
        (
            Position + (to.Position - Position) * scalar,
            RotationRad + (to.RotationRad - RotationRad) * scalar,
            BaseSize + (to.BaseSize - BaseSize) * scalar
        );
    }
    public Transform2D ExpDecayLerpComplex(Transform2D to, float decay, float dt)
    {
        var scalar = MathF.Exp(-decay * dt);
        
        return new Transform2D
        (
            Position + (to.Position - Position) * scalar,
            RotationRad + (to.RotationRad - RotationRad) * scalar,
            BaseSize + (to.BaseSize - BaseSize) * scalar
        );
    }
    public Transform2D ExpDecayLerp(Transform2D to, float f, float dt)
    {
        var decay = ShapeMath.LerpFloat(1, 25, f);
        var scalar = MathF.Exp(-decay * dt);
        
        return new Transform2D
        (
            Position + (to.Position - Position) * scalar,
            RotationRad + (to.RotationRad - RotationRad) * scalar,
            BaseSize + (to.BaseSize - BaseSize) * scalar
        );
    }


    #endregion
    
    #region Math
    public Vector2 RevertPosition(Vector2 position)
    {
        var w = (position - Position).Rotate(-RotationRad) / ScaledSize.Length;
        return Position + w;
    }

    public Vector2 ApplyTransformTo(Vector2 relative)
    {
        if (relative.LengthSquared() == 0f) return Position;
        return Position + (relative * ScaledSize.Length).Rotate(RotationRad);
    }

    public readonly Transform2D ChangePosition(Vector2 amount) => new(Position + amount, RotationRad, BaseSize, Scale2d);
    public readonly Transform2D ChangePositionX(float amount) => new(Position with { X = Position.X + amount }, RotationRad, BaseSize, Scale2d);
    public readonly Transform2D ChangePositionY(float amount) => new(Position with { Y = Position.Y + amount }, RotationRad, BaseSize, Scale2d);
    
    public readonly Transform2D ChangeSize(Size amount) => new(Position, RotationRad, BaseSize + amount, Scale2d);
    public readonly Transform2D ChangeSize(float amount) => new(Position, RotationRad, BaseSize + new Vector2(amount), Scale2d);
    public readonly Transform2D ChangeSizeX(float amount) => new(Position, RotationRad, new Size(Position.X + amount, BaseSize.Height), Scale2d);
    public readonly Transform2D ChangeSizeY(float amount) => new(Position, RotationRad, new Size(BaseSize.Width, Position.Y + amount), Scale2d);
    
    public readonly Transform2D MultiplyScale(float factor) => new(Position, RotationRad, BaseSize, Scale2d * factor);
    
    public readonly Transform2D ChangeScale(float amount) => new(Position, RotationRad, BaseSize, Scale2d.X + amount, Scale2d.Y + amount);
    
    public readonly Transform2D MultiplyScale2d(Vector2 factor) => new(Position, RotationRad, BaseSize, Scale2d * factor);
    
    public readonly Transform2D ChangeScale2d(Vector2 amount) => new(Position, RotationRad, BaseSize, Scale2d + amount);
    
    public readonly Transform2D ChangeRotationRad(float amount) => new(Position, RotationRad + amount, BaseSize, Scale2d);
    public readonly Transform2D ChangeRotationDeg(float amount) => new(Position, RotationRad + (amount * ShapeMath.DEGTORAD), BaseSize, Scale2d);
    
    public readonly Transform2D SetPosition(Vector2 newPosition) => new(newPosition, RotationRad, BaseSize, Scale2d);
    public readonly Transform2D SetRotationRad(float newRotationRad) => new(Position, newRotationRad, BaseSize, Scale2d);
    public Transform2D WrapRotationRad() => new(Position, ShapeMath.WrapAngleRad(RotationRad), BaseSize, Scale2d);
    public readonly Transform2D SetSize(Size newSize) => new(Position, RotationRad, newSize, Scale2d);
    public readonly Transform2D SetSize(float newSize) => new(Position, RotationRad, new(newSize), Scale2d);
    
    public readonly Transform2D SetScale(float newScale) => new(Position, RotationRad, BaseSize, newScale);
    
    public Transform2D AddOffset(Transform2D offset)
    {
        return new
        (
            Position + offset.Position,
            RotationRad + offset.RotationRad,
            BaseSize + offset.BaseSize,
            Scale2d * offset.Scale2d
        );
    }
    public Transform2D RemoveOffset(Transform2D offset)
    {
        return new
        (
            Position - offset.Position,
            RotationRad - offset.RotationRad,
            BaseSize - offset.BaseSize,
            Scale2d.DivideSafe(offset.Scale2d)
        );
    }

    #endregion

    #region Operators

    public readonly Transform2D Multiply(float factor)
    {
        return new
        (
            Position * factor,
            RotationRad * factor,
            BaseSize * factor,
            Scale2d * factor
        );
    }
    public readonly Transform2D Divide(float divisor)
    {
        if(divisor == 0) return this;
        return new
        (
            Position / divisor,
            RotationRad / divisor,
            BaseSize / divisor,
            Scale2d / divisor
        );
    }

   
    public static Transform2D operator +(Transform2D left, Transform2D right)
    {
        return new
        (
            left.Position + right.Position,
            left.RotationRad + right.RotationRad,
            left.BaseSize + right.BaseSize,
            left.Scale2d + right.Scale2d
        );
    }
    public static Transform2D operator -(Transform2D left, Transform2D right)
    {
        return new
        (
            left.Position - right.Position,
            left.RotationRad - right.RotationRad,
            left.BaseSize - right.BaseSize,
            left.Scale2d - right.Scale2d
        );
    }
    public static Transform2D operator /(Transform2D left, Transform2D right)
    {
        return new
        (
            left.Position / right.Position,
            left.RotationRad / right.RotationRad,
            left.BaseSize / right.BaseSize,
            left.Scale2d.DivideSafe(right.Scale2d)
        );
    }
    public static Transform2D operator *(Transform2D left, Transform2D right)
    {
        return new
        (
            left.Position * right.Position,
            left.RotationRad * right.RotationRad,
            left.BaseSize * right.BaseSize,
            left.Scale2d * right.Scale2d
        );
    }
    
    /// <summary>
    /// Applies to Position only!
    /// </summary>
    public static Transform2D operator +(Transform2D left, Vector2 right)
    {
        return new
        (
            left.Position + right,
            left.RotationRad,
            left.BaseSize,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to Position only!
    /// </summary>
    public static Transform2D operator -(Transform2D left, Vector2 right)
    {
        return new
        (
            left.Position - right,
            left.RotationRad,
            left.BaseSize,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to Position only!
    /// </summary>
    public static Transform2D operator *(Transform2D left, Vector2 right)
    {
        return new
        (
            left.Position * right,
            left.RotationRad,
            left.BaseSize,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to Position only!
    /// </summary>
    public static Transform2D operator /(Transform2D left, Vector2 right)
    {
        return new
        (
            left.Position / right,
            left.RotationRad,
            left.BaseSize,
            left.Scale2d
        );
    }
    
    /// <summary>
    /// Applies to rotation only!
    /// </summary>
    public static Transform2D operator +(Transform2D left, float right)
    {
        return new
        (
            left.Position,
            left.RotationRad + right,
            left.BaseSize,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to rotation only!
    /// </summary>
    public static Transform2D operator -(Transform2D left, float right)
    {
        return new
        (
            left.Position,
            left.RotationRad - right,
            left.BaseSize,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to rotation only!
    /// </summary>
    public static Transform2D operator *(Transform2D left, float right)
    {
        return new
        (
            left.Position,
            left.RotationRad * right,
            left.BaseSize,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to rotation only!
    /// </summary>
    public static Transform2D operator /(Transform2D left, float right)
    {
        return new
        (
            left.Position,
            left.RotationRad /right,
            left.BaseSize ,
            left.Scale2d
        );
    }
    
    /// <summary>
    /// Applies to BaseSize only!
    /// </summary>
    public static Transform2D operator +(Transform2D left, Size right)
    {
        return new
        (
            left.Position,
            left.RotationRad,
            left.BaseSize + right,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to BaseSize only!
    /// </summary>
    public static Transform2D operator -(Transform2D left, Size right)
    {
        return new
        (
            left.Position,
            left.RotationRad,
            left.BaseSize - right,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to BaseSize only!
    /// </summary>
    public static Transform2D operator *(Transform2D left, Size right)
    {
        return new
        (
            left.Position,
            left.RotationRad,
            left.BaseSize * right,
            left.Scale2d
        );
    }
    /// <summary>
    /// Applies to BaseSize only!
    /// </summary>
    public static Transform2D operator /(Transform2D left, Size right)
    {
        return new
        (
            left.Position,
            left.RotationRad,
            left.BaseSize / right,
            left.Scale2d
        );
    }
    
    
    public static bool operator ==(Transform2D left, Transform2D right) => right.Equals(left);
    public static bool operator !=(Transform2D left, Transform2D right) => !(left == right);
    
    #endregion

    #region Equals & Hash Code
    public bool Equals(Transform2D other) => Position.Equals(other.Position) && RotationRad.Equals(other.RotationRad) && BaseSize.Equals(other.BaseSize) && Scale2d.Equals(other.Scale2d);
    public override bool Equals(object? obj) => obj is Transform2D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Position, RotationRad, BaseSize, Scale2d);
    #endregion
}