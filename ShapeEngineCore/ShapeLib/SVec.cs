﻿//using Raylib_CsLo;
using System.Numerics;


namespace ShapeLib
{
    public static class SVec
    {
        public static float Cross(this Vector2 value1, Vector2 value2)
        {
            return value1.X * value2.Y
                   - value1.Y * value2.X;
        }
        public static bool IsNan(this Vector2 v) { return float.IsNaN(v.X) || float.IsNaN(v.Y); }
        public static Vector2 Right() { return new(1.0f, 0.0f); }
        public static Vector2 Left() { return new(-1.0f, 0.0f); }
        public static Vector2 Up() { return new(0.0f, -1.0f); }
        public static Vector2 Down() { return new(0.0f, 1.0f); }
        public static Vector2 One() { return new(1.0f, 1.0f); }
        public static Vector2 Zero() { return new(0.0f, 0.0f); }

        //Perpendicular & Rotation
        public static Vector2 GetPerpendicularRight(this Vector2 v)
        {
            return new(v.Y, -v.X);
        }
        public static Vector2 GetPerpendicularLeft(this Vector2 v)
        {
            return new(-v.Y, v.X);
        }
        public static Vector2 Rotate90CCW(this Vector2 v)
        {
            return GetPerpendicularLeft(v);
            //return new(-v.Y, v.X);
        }
        public static Vector2 Rotate90CW(this Vector2 v)
        {
            return GetPerpendicularRight(v);
        }

        public static Vector2 VecFromAngleRad(float angleRad)
        {
            return SVec.Rotate(SVec.Right(), angleRad);
        }
        public static Vector2 VecFromAngleDeg(float angleDeg)
        {
            return VecFromAngleRad(angleDeg * SUtils.DEGTORAD);
        }
        
        //Projection
        public static float ProjectionTime(this Vector2 v, Vector2 onto) { return (v.X * onto.X + v.Y * onto.Y) / onto.LengthSquared(); }
        public static Vector2 ProjectionPoint(this Vector2 point, Vector2 v, float t) { return point + v * t; }
        public static Vector2 Project(this Vector2 project, Vector2 onto)
        {
            float d = Vector2.Dot(onto, onto);
            if (d > 0.0f)
            {
                float dp = Vector2.Dot(project, onto);
                return onto * (dp / d);
            }
            return onto;
        }
        public static bool Parallel(this Vector2 a, Vector2 b)
        {
            Vector2 rotated = Rotate90CCW(a);
            return Vector2.Dot(rotated, b) == 0.0f;
        }
        
        public static float Max(this Vector2 v) { return MathF.Max(v.X, v.Y); }
        public static float Min(this Vector2 v) { return MathF.Min(v.X, v.Y); }
        public static Vector2 LerpDirection(this Vector2 from, Vector2 to, float t)
        {
            float angleA = SVec.AngleRad(from);
            float angle = SUtils.GetShortestAngleRad(angleA, SVec.AngleRad(to));
            return SVec.Rotate(from, SUtils.LerpFloat(0, angle, t));
        }
        public static Vector2 Lerp(this Vector2 from, Vector2 to, float t) { return Vector2.Lerp(from, to, t); } //RayMath.Vector2Lerp(v1, v2, t);
        public static Vector2 MoveTowards(this Vector2 from, Vector2 to, float maxDistance) 
        {
            
            Vector2 result = new();
            float difX = to.X - from.X;
            float difY = to.Y - from.Y;
            float lengthSq = difX * difX + difY * difY;
            if (lengthSq == 0f || (maxDistance >= 0f && lengthSq <= maxDistance * maxDistance))
            {
                return to;
            }

            float length = MathF.Sqrt(lengthSq);
            result.X = from.X + difX / length * maxDistance;
            result.Y = from.Y + difY / length * maxDistance;
            return result;
        }
        public static Vector2 Floor(this Vector2 v) { return new(MathF.Floor(v.X), MathF.Floor(v.Y)); }
        public static Vector2 Ceil(this Vector2 v) { return new(MathF.Ceiling(v.X), MathF.Ceiling(v.Y)); }
        public static Vector2 Round(this Vector2 v) { return new(MathF.Round(v.X), MathF.Round(v.Y)); }
        public static Vector2 Abs(this Vector2 v) { return Vector2.Abs(v); }
        public static Vector2 Negate(this Vector2 v) { return Vector2.Negate(v); } //RayMath.Vector2Negate(v);
        public static Vector2 Min(this Vector2 v1, Vector2 v2) { return Vector2.Min(v1, v2); }
        public static Vector2 Max(this Vector2 v1, Vector2 v2) { return Vector2.Max(v1, v2); }
        public static Vector2 Clamp(this Vector2 v, Vector2 min, Vector2 max) { return Vector2.Clamp(v, min, max); }
        public static Vector2 Normalize(this Vector2 v) { return Vector2.Normalize(v); } //return value / value.Length(); // RayMath.Vector2Normalize(v); }//return Vector2.Normalize(v); } //Vector2 normalize returns NaN sometimes???
        public static Vector2 Reflect(this Vector2 v, Vector2 n) { return Vector2.Reflect(v, n); } //RayMath.Vector2Reflect(v, n);
        //public static Vector2 Scale(this Vector2 v, float amount) { return RayMath.Vector2Scale(v, amount); }
        public static Vector2 ScaleUniform(this Vector2 v, float distance)
        {
            float length = v.Length();
            if (length <= 0) return v;

            float scale = 1f + (distance / v.Length());
            return v * scale; // Scale(v, scale);
        }
        public static Vector2 SquareRoot(this Vector2 v) { return Vector2.SquareRoot(v); }
        public static Vector2 Rotate(this Vector2 v, float angleRad) 
        {
            Vector2 result = new();
            float num = MathF.Cos(angleRad);
            float num2 = MathF.Sin(angleRad);
            result.X = v.X * num - v.Y * num2;
            result.Y = v.X * num2 + v.Y * num;
            return result;

            //return RayMath.Vector2Rotate(v, angleRad); 
        } //radians
        public static Vector2 RotateDeg(this Vector2 v, float angleDeg) { return Rotate(v, angleDeg * SUtils.DEGTORAD); }
        public static float AngleDeg(this Vector2 v1, Vector2 v2) { return AngleRad(v1, v2) * SUtils.RADTODEG; }
        public static float AngleDeg(this Vector2 v) { return AngleRad(v) * SUtils.RADTODEG; }
        public static float AngleRad(this Vector2 v) { return AngleRad(Zero(), v); }
        public static float AngleRad(this Vector2 v1, Vector2 v2) { return MathF.Atan2(v2.Y, v2.X) - MathF.Atan2(v1.Y, v1.X); }// return RayMath.Vector2Angle(v1, v2); }
        public static float Distance(this Vector2 v1, Vector2 v2) { return Vector2.Distance(v1, v2); }// RayMath.Vector2Distance(v1, v2); }
        public static float Dot(this Vector2 v1, Vector2 v2) { return Vector2.Dot(v1, v2); }// RayMath.Vector2DotProduct(v1, v2); }
        //public static float Length(Vector2 v) { return v.Length(); } //RayMath.Vector2Length(v);
        //public static float LengthSquared(Vector2 v) { return v.LengthSquared(); } //RayMath.Vector2LengthSqr(v);


    }
}
