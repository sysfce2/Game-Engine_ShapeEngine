﻿
using System.Numerics;
using ShapeEngine.Lib;
using ShapeEngine.Random;

namespace ShapeEngine.Core
{
    /// <summary>
    /// Class that represents a triangle by holding three points. Points a, b, c should be in ccw order!
    /// </summary>
    public struct Triangle : IShape, IEquatable<Triangle>
    {
        #region Members
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;
        #endregion

        #region Getter Setter
        public Vector2 SideA { get { return B - A; } }
        public Vector2 SideB { get { return C - B; } }
        public Vector2 SideC { get { return A - C; } }
        public Segment SegmentA { get { return new Segment(A, B, FlippedNormals); } }
        public Segment SegmentB { get { return new Segment(B, C, FlippedNormals); } }
        public Segment SegmentC { get { return new Segment(C, A, FlippedNormals); } }
           
        public bool FlippedNormals { get; set; } = false;
        #endregion

        #region Constructors
        /// <summary>
        /// Points should be in ccw order!
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public Triangle(Vector2 a, Vector2 b, Vector2 c, bool flippedNormals = false) 
        { 
            this.A = a; 
            this.B = b; 
            this.C = c;
            this.FlippedNormals = flippedNormals;
        }
        public Triangle(Vector2 p, Segment s, bool flippedNormals = false)
        {
            Vector2 w = s.Displacement;
            Vector2 v = p - s.Start;
            float cross = w.Cross(v);
            if(cross <= 0f)
            {
                A = s.Start;
                B = s.End;
                C = p;
            }
            else
            {
                A = s.End;
                B = s.Start;
                C = p;
            }
            this.FlippedNormals = flippedNormals;
        }
        #endregion

        #region Public

        public bool Contains(Segment other)
        {
            return Contains(other.Start) && Contains(other.End);
        }
        public bool Contains(Circle other)
        {
            var points = other.GetVertices(8);
            return Contains(points);
        }
        public bool Contains(Rect other)
        {
            return Contains(other.TopLeft) &&
                Contains(other.BottomLeft) &&
                Contains(other.BottomRight) &&
                Contains(other.TopRight);
        }
        public bool Contains(Triangle other)
        {
            return Contains(other.A) &&
                Contains(other.B) &&
                Contains(other.C);
        }
        public bool Contains(Points points)
        {
            if (points.Count <= 0) return false;
            foreach (var p in points)
            {
                if (!Contains(p)) return false;
            }
            return true;
        }


        public readonly Triangle Floor() { return new(A.Floor(), B.Floor(), C.Floor(), FlippedNormals); }
        public readonly Triangle Ceiling() { return new(A.Ceiling(), B.Ceiling(), C.Ceiling(), FlippedNormals); }
        public readonly Triangle Round() { return new(A.Round(), B.Round(), C.Round(), FlippedNormals); }
        public readonly Triangle Truncate() { return new(A.Truncate(), B.Truncate(), C.Truncate(), FlippedNormals); }
        
        public readonly bool SharesVertex(Vector2 p) { return A == p || B == p || C == p; }
        public readonly bool SharesVertex(IEnumerable<Vector2> points)
        {
            foreach (var p in points)
            {
                if (SharesVertex(p)) return true;
            }
            return false;
        }
        public readonly bool SharesVertex(Triangle t) { return SharesVertex(t.A) || SharesVertex(t.B) || SharesVertex(t.C); }
        
        public readonly bool IsValid() { return GetArea() > 0f; }
        public readonly bool IsNarrow(float narrowValue = 0.2f)
        {
            Points points = new() { A, B, C };
            for (int i = 0; i < 3; i++)
            {
                Vector2 a = points[i];
                Vector2 b = SUtils.GetItem(points, i + 1);
                Vector2 c = SUtils.GetItem(points, i - 1);

                Vector2 ba = (b - a).Normalize();
                Vector2 ca = (c - a).Normalize();
                float cross = ba.Cross(ca);
                if (MathF.Abs(cross) < narrowValue) return true;
            }
            return false;
        }
        /// <summary>
        /// Returns a point inside the triangle.
        /// </summary>
        /// <param name="t">The triangle to find a point in.</param>
        /// <param name="f1">First value in the range 0 - 1.</param>
        /// <param name="f2">Second value in the range 0 - 1.</param>
        /// <returns></returns>
        public readonly Vector2 GetPoint(float f1, float f2)
        {
            if ((f1 + f2) > 1)
            {
                f1 = 1f - f1;
                f2 = 1f - f2;
            }
            Vector2 ac = (C - A) * f1;
            Vector2 ab = (B - A) * f2;
            return A + ac + ab;
            //float f1Sq = MathF.Sqrt(f1);
            //float x = (1f - f1Sq) * t.a.X + (f1Sq * (1f - f2)) * t.b.X + (f1Sq * f2) * t.c.X;
            //float y = (1f - f1Sq) * t.a.Y + (f1Sq * (1f - f2)) * t.b.Y + (f1Sq * f2) * t.c.Y;
            //return new(x, y);
        }
        public readonly Circle GetCircumCircle()
        {
            Vector2 SqrA = new Vector2(A.X * A.X, A.Y * A.Y);
            Vector2 SqrB = new Vector2(B.X * B.X, B.Y * B.Y); 
            Vector2 SqrC = new Vector2(C.X * C.X, C.Y * C.Y);

            float D = (A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y)) * 2f;
            float x = ((SqrA.X + SqrA.Y) * (B.Y - C.Y) + (SqrB.X + SqrB.Y) * (C.Y - A.Y) + (SqrC.X + SqrC.Y) * (A.Y - B.Y)) / D;
            float y = ((SqrA.X + SqrA.Y) * (C.X - B.X) + (SqrB.X + SqrB.Y) * (A.X - C.X) + (SqrC.X + SqrC.Y) * (B.X - A.X)) / D;

            Vector2 center = new Vector2(x, y);
            float r = (A - center).Length();
            return new(center, r);
        }

        public readonly Triangulation Triangulate(int pointCount)
        {
            if (pointCount < 0) return new() { new(A, B, C, FlippedNormals) };

            Points points = new() { A, B, C };

            for (int i = 0; i < pointCount; i++)
            {
                float f1 = SRNG.randF();
                float f2 = SRNG.randF();
                Vector2 randPoint = GetPoint(f1, f2);
                points.Add(randPoint);
            }

            return Polygon.TriangulateDelaunay(points);
        }
        public readonly Triangulation Triangulate(float minArea)
        {
            if (minArea <= 0) return new() { new(A,B,C,FlippedNormals) };

            float triArea = GetArea();
            float pieceCount = triArea / minArea;
            int points = (int)MathF.Floor((pieceCount - 1f) * 0.5f);
            return Triangulate(points);
        }
        public readonly Triangulation Triangulate(Vector2 p)
        {
            return new()
            {
                new(A, B, p),
                new(B, C, p),
                new(C, A, p)
            };
        }
        
        public readonly Triangle GetInsideTriangle(float abF, float bcF, float caF)
        {
            Vector2 newA = SVec.Lerp(A, B, abF);
            Vector2 newB = SVec.Lerp(B, C, bcF);
            Vector2 newC = SVec.Lerp(C, A, caF);
            return new(newA, newB, newC);
        }

        
        public Triangle Rotate(float rad) { return Rotate(GetCentroid(), rad); }
        public readonly Triangle Rotate(Vector2 pivot, float rad)
        {
            Vector2 newA = pivot + (A - pivot).Rotate(rad);
            Vector2 newB = pivot + (B - pivot).Rotate(rad);
            Vector2 newC = pivot + (C - pivot).Rotate(rad);
            return new(newA, newB, newC, FlippedNormals);
        }
        public readonly Triangle Scale(float scale) { return new(A * scale, B * scale, C * scale, FlippedNormals); }
        public readonly Triangle Scale(Vector2 scale) { return new(A * scale, B * scale, C * scale, FlippedNormals); }
        public readonly Triangle Scale(Vector2 pivot, float scale)
        {
            Vector2 newA = pivot + (A - pivot) * scale;
            Vector2 newB = pivot + (B - pivot) * scale;
            Vector2 newC = pivot + (C - pivot) * scale;
            return new(newA, newB, newC, FlippedNormals);
        }
        public readonly Triangle Scale(Vector2 pivot, Vector2 scale)
        {
            Vector2 newA = pivot + (A - pivot) * scale;
            Vector2 newB = pivot + (B - pivot) * scale;
            Vector2 newC = pivot + (C - pivot) * scale;
            return new(newA, newB, newC, FlippedNormals);
        }
        public readonly Triangle Move(Vector2 offset) { return new(A + offset, B + offset, C + offset, FlippedNormals); }
        #endregion

        #region Static
        public static bool IsPointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            Vector2 ab = b - a;
            Vector2 bc = c - b;
            Vector2 ca = a - c;

            Vector2 ap = p - a;
            Vector2 bp = p - b;
            Vector2 cp = p - c;

            float c1 = SVec.Cross(ab, ap);
            float c2 = SVec.Cross(bc, bp);
            float c3 = SVec.Cross(ca, cp);

            if (c1 < 0f && c2 < 0f && c3 < 0f)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Equality & HashCode
        public bool IsSimilar(Triangle other)
        {
            return 
                (A == other.A && B == other.B && C == other.C) || 
                (C == other.A && A == other.B && B == other.C) || 
                (B == other.A && C == other.B && A == other.C) ||
                (B == other.A && A == other.B && C == other.C) ||
                (C == other.A && B == other.B && A == other.C) ||
                (A == other.A && C == other.B && B == other.C);
        }
        public bool Equals(Triangle other)
        {

            return A == other.A && B == other.B && C == other.C;
        }
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(A, B, C);
        }
        public static bool operator ==(Triangle left, Triangle right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(Triangle left, Triangle right)
        {
            return !(left == right);
        }
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Triangle t) return Equals(t);
            return false;
        }
        #endregion

        #region IShape
        public Vector2 GetCentroid() { return (A + B + C) / 3; }
        public Points GetVertices() { return new(A, B, C); }
        public Polygon ToPolygon() { return new(A, B, C); }
        public Polyline ToPolyline() { return new(A, B, C); }
        public Segments GetEdges() { return new() { SegmentA, SegmentB, SegmentC }; }
        public Triangulation Triangulate() { return this.Triangulate(GetCentroid()); }
        public Circle GetBoundingCircle() { return GetCircumCircle(); } // ToPolygon().GetBoundingCircle(); }
        public float GetCircumference() { return MathF.Sqrt(GetCircumferenceSquared()); }
        public float GetCircumferenceSquared() { return SideA.LengthSquared() + SideB.LengthSquared() + SideC.LengthSquared(); }
        public readonly float GetArea() 
        {
            //float al = A.Length();
            //float bl = B.Length();
            //float cl = C.Length();
            //
            //
            //float i = (al + bl + cl) / 2f;
            //float area1 = MathF.Sqrt(i * (i - al) * (i - bl) * (i - cl));
            //float area2 = MathF.Abs((a.X - c.X) * (b.Y - c.Y) - (a.Y - c.Y) * (b.X - c.X)) / 2f;
            //if(MathF.Abs(area1 - area2) > 1)
            //{
            //    int breakpoint = 0;
            //}

            return MathF.Abs((A.X - C.X) * (B.Y - C.Y) - (A.Y - C.Y) * (B.X - C.X)) / 2f;
        }
        public Rect GetBoundingBox() { return new Rect(A.X, A.Y, 0, 0).Enlarge(B).Enlarge(C); }
        public bool Contains(Vector2 p) { return IsPointInTriangle(A, B, C, p); }
        public CollisionPoint GetClosestPoint(Vector2 p) { return ToPolygon().GetClosestPoint(p); }
        public Vector2 GetClosestVertex(Vector2 p) { return ToPolygon().GetClosestVertex(p); }
        public Vector2 GetRandomPoint() { return this.GetPoint(SRNG.randF(), SRNG.randF()); }
        public Points GetRandomPoints(int amount)
        {
            var points = new Points();
            for (int i = 0; i < amount; i++)
            {
                points.Add(GetRandomPoint());
            }
            return points;
        }
        public Vector2 GetRandomVertex() { return SRNG.randCollection(GetVertices(), false); }
        public Segment GetRandomEdge() 
        {
            var edges = GetEdges();
            List<WeightedItem<Segment>> items = new(edges.Count);
            foreach (var edge in edges)
            {
                items.Add(new(edge, (int)edge.LengthSquared));
            }
            return SRNG.PickRandomItem(items.ToArray());
            //return SRNG.randCollection(GetEdges(), false); 
        }
        public Vector2 GetRandomPointOnEdge() { return GetRandomEdge().GetRandomPoint(); }
        public Points GetRandomPointsOnEdge(int amount)
        {
            List<WeightedItem<Segment>> items = new(amount);
            var edges = GetEdges();
            foreach (var edge in edges)
            {
                items.Add(new(edge, (int)edge.LengthSquared));
            }
            var pickedEdges = SRNG.PickRandomItems(amount, items.ToArray());
            var randomPoints = new Points();
            foreach (var edge in pickedEdges)
            {
                randomPoints.Add(edge.GetRandomPoint());
            }
            return randomPoints;
        }
        public void DrawShape(float linethickness, Raylib_CsLo.Color color) => this.DrawLines(linethickness, color);
        #endregion

        #region Overlap
        public bool OverlapShape(Segments segments)
        {
            foreach (var seg in segments)
            {
                if (seg.OverlapShape(this)) return true;
            }
            return false;
        }
        public bool OverlapShape(Segment s) { return ToPolygon().OverlapShape(s); }
        public bool OverlapShape(Circle c) { return ToPolygon().OverlapShape(c); }
        public bool OverlapShape(Triangle b) { return ToPolygon().OverlapShape(b.ToPolygon()); }
        public bool OverlapShape(Rect r) { return ToPolygon().OverlapShape(r); }
        public bool OverlapShape(Polygon poly) { return OverlapShape(poly); }
        public bool OverlapShape(Polyline pl) { return pl.OverlapShape(this); }


        #endregion

        #region Intersect
        public CollisionPoints IntersectShape(Segment s) { return GetEdges().IntersectShape(s); }
        public CollisionPoints IntersectShape(Circle c) { return ToPolygon().IntersectShape(c); }
        public CollisionPoints IntersectShape(Triangle b) { return ToPolygon().IntersectShape(b.ToPolygon()); }
        public CollisionPoints IntersectShape(Rect r) { return ToPolygon().IntersectShape(r.ToPolygon()); }
        public CollisionPoints IntersectShape(Polygon p) { return ToPolygon().IntersectShape(p); }
        public CollisionPoints IntersectShape(Polyline pl) { return GetEdges().IntersectShape(pl.GetEdges()); }
        #endregion
    }
}
