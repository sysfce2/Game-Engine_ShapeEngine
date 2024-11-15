﻿
using System.Drawing;
using System.Numerics;
using Clipper2Lib;
using ShapeEngine.Core.CollisionSystem;
using ShapeEngine.Core.Structs;
using ShapeEngine.Lib;
using ShapeEngine.Random;
using Size = ShapeEngine.Core.Structs.Size;

namespace ShapeEngine.Core.Shapes
{
    
    /// <summary>
    /// Points shoud be in CCW order.
    /// </summary>
    public class Polygon : Points, IEquatable<Polygon>
    {
        

        public override Polygon Copy() => new(this);

        #region Constructors
        public Polygon() { }

        public Polygon(int capacity) : base(capacity)
        {
            
        }
        
        /// <summary>
        /// Points should be in CCW order. Use Reverse if they are in CW order.
        /// </summary>
        /// <param name="points"></param>
        public Polygon(IEnumerable<Vector2> points) { AddRange(points); }
        public Polygon(Points points) : base(points.Count) { AddRange(points); }
        
        public Polygon(Polygon poly) : base(poly.Count) { AddRange(poly); }
        public Polygon(Polyline polyLine) : base(polyLine.Count) { AddRange(polyLine); }
        #endregion

        #region Equals & Hashcode
        public bool Equals(Polygon? other)
        {
            if (other == null) return false;
            
            if (Count != other.Count) return false;
            for (var i = 0; i < Count; i++)
            {
                if (!this[i].IsSimilar(other[i])) return false;
                //if (this[i] != other[i]) return false;
            }
            return true;
        }

        public override int GetHashCode() => Game.GetHashCode(this);

        #endregion

        #region Vertices

        public void FixWindingOrder()
        {
            if (IsClockwise())
            {
                Reverse();
            }
        }
        public void MakeClockwise()
        {
            if (IsClockwise()) return;
            Reverse();
        }
        public void MakeCounterClockwise()
        {
            if (!IsClockwise()) return;
            Reverse();
        }
        public void ReduceVertexCount(int newCount)
        {
            if (newCount < 3) Clear();//no points left to form a polygon

            while (Count > newCount)
            {
                float minD = 0f;
                int shortestID = 0;
                for (int i = 0; i < Count; i++)
                {
                    float d = (this[i] - this[(i + 1) % Count]).LengthSquared();
                    if (d > minD)
                    {
                        minD = d;
                        shortestID = i;
                    }
                }
                RemoveAt(shortestID);
            }

        }

        public void ReduceVertexCount(float factor)
        {
            ReduceVertexCount(Count - (int)(Count * factor));
        }
        public void IncreaseVertexCount(int newCount)
        {
            if (newCount <= Count) return;

            while (Count < newCount)
            {
                float maxD = 0f;
                int longestID = 0;
                for (int i = 0; i < Count; i++)
                {
                    float d = (this[i] - this[(i + 1) % Count]).LengthSquared();
                    if (d > maxD)
                    {
                        maxD = d;
                        longestID = i;
                    }
                }
                Vector2 m = (this[longestID] + this[(longestID + 1) % Count]) * 0.5f;
                this.Insert(longestID + 1, m);
            }
        }
        public Vector2 GetVertex(int index) => this[ShapeMath.WrapIndex(Count, index)];

        public void RemoveColinearVertices()
        {
            if (Count < 3) return;
            Points result = new();
            for (int i = 0; i < Count; i++)
            {
                var cur = this[i];
                var prev = Game.GetItem(this, i - 1);
                var next = Game.GetItem(this, i + 1);

                var prevCur = prev - cur;
                var nextCur = next - cur;
                if (prevCur.Cross(nextCur) != 0f) result.Add(cur);
            }
            Clear();
            AddRange(result);
        }
        public void RemoveDuplicates(float toleranceSquared = 0.001f)
        {
            if (Count < 3) return;
            Points result = new();

            for (var i = 0; i < Count; i++)
            {
                var cur = this[i];
                var next = Game.GetItem(this, i + 1);
                if ((cur - next).LengthSquared() > toleranceSquared) result.Add(cur);
            }
            Clear();
            AddRange(result);
        }
        public void Smooth(float amount, float baseWeight)
        {
            if (Count < 3) return;
            Points result = new();
            var centroid = GetCentroid();
            for (int i = 0; i < Count; i++)
            {
                var cur = this[i];
                var prev = this[ShapeMath.WrapIndex(Count, i - 1)];
                var next = this[ShapeMath.WrapIndex(Count, i + 1)];
                var dir = (prev - cur) + (next - cur) + ((cur - centroid) * baseWeight);
                result.Add(cur + dir * amount);
            }

            Clear();
            AddRange(result);
        }
        #endregion

        #region Shape
        
        public (Transform2D transform, Polygon shape) ToRelative()
        {
            var pos = GetCentroid();
            var maxLengthSq = 0f;
            for (int i = 0; i < this.Count; i++)
            {
                var lsq = (this[i] - pos).LengthSquared();
                if (maxLengthSq < lsq) maxLengthSq = lsq;
            }

            var size = MathF.Sqrt(maxLengthSq);
            var relativeShape = new Polygon(Count);
            for (int i = 0; i < this.Count; i++)
            {
                var w = this[i] - pos;
                relativeShape.Add(w / size); //transforms it to range 0 - 1
            }

            return (new Transform2D(pos, 0f, new Size(size, 0f), 1f), relativeShape);
        }

        public Points ToRelativePoints(Transform2D transform)
        {
            var points = new Points(Count);
            for (int i = 0; i < Count; i++)
            {
                var p = transform.RevertPosition(this[i]);
                points.Add(p);
            }

            return points;
        }
        public Polygon ToRelativePolygon(Transform2D transform)
        {
            var points = new Polygon(Count);
            for (int i = 0; i < Count; i++)
            {
                var p = transform.RevertPosition(this[i]);
                points.Add(p);
            }

            return points;
        }
        public List<Vector2> ToRelative(Transform2D transform)
        {
            var points = new List<Vector2>(Count);
            for (int i = 0; i < Count; i++)
            {
                var p = transform.RevertPosition(this[i]);
                points.Add(p);
            }

            return points;
        }


        public Triangle GetBoundingTriangle(float margin = 3f) => Polygon.GetBoundingTriangle(this, margin);

        public Triangulation Triangulate()
        {
            if (Count < 3) return new();
            if (Count == 3) return new() { new(this[0], this[1], this[2]) };

            Triangulation triangles = new();
            List<Vector2> vertices = new();
            vertices.AddRange(this);
            List<int> validIndices = new();
            for (int i = 0; i < vertices.Count; i++)
            {
                validIndices.Add(i);
            }
            while (vertices.Count > 3)
            {
                if (validIndices.Count <= 0) 
                    break;

                int i = validIndices[Rng.Instance.RandI(0, validIndices.Count)];
                var a = vertices[i];
                var b = Game.GetItem(vertices, i + 1);
                var c = Game.GetItem(vertices, i - 1);

                var ba = b - a;
                var ca = c - a;
                float cross = ba.Cross(ca);
                if (cross >= 0f)//makes sure that ear is not self intersecting
                {
                    validIndices.Remove(i);
                    continue;
                }

                Triangle t = new(a, b, c);

                bool isValid = true;
                foreach (var p in this)
                {
                    if (p == a || p == b || p == c) continue;
                    if (t.ContainsPoint(p))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    triangles.Add(t);
                    vertices.RemoveAt(i);

                    validIndices.Clear();
                    for (int j = 0; j < vertices.Count; j++)
                    {
                        validIndices.Add(j);
                    }
                    //break;
                }
            }


            triangles.Add(new(vertices[0], vertices[1], vertices[2]));


            return triangles;
        }

        /// <summary>
        /// Return the segments of the polygon. If the points are in ccw winding order the normals face outward when InsideNormals = false 
        /// and face inside otherwise.
        /// </summary>
        /// <returns></returns>
        public Segments GetEdges()
        {
            if (Count <= 1) return new();
            if (Count == 2) return new() { new(this[0], this[1]) };
            Segments segments = new(Count);
            for (int i = 0; i < Count; i++)
            {
                segments.Add(new(this[i], this[(i + 1) % Count]));
            }
            return segments;
        }
        public Circle GetBoundingCircle()
        {
            float maxD = 0f;
            int num = this.Count;
            Vector2 origin = new();
            for (int i = 0; i < num; i++) { origin += this[i]; }
            origin = origin / num;
            //origin *= (1f / (float)num);
            for (int i = 0; i < num; i++)
            {
                float d = (origin - this[i]).LengthSquared();
                if (d > maxD) maxD = d;
            }

            return new Circle(origin, MathF.Sqrt(maxD));
        }
        
        public Rect GetBoundingBox()
        {
            if (Count < 2) return new();
            var start = this[0];
            Rect r = new(start.X, start.Y, 0, 0);

            foreach (var p in this)
            {
                r = r.Enlarge(p);// ShapeRect.Enlarge(r, p);
            }
            return r;
        }
        public Polygon ToConvex() => Polygon.FindConvexHull(this);

        #endregion
        
        #region Math

        public Points? GetProjectedShapePoints(Vector2 v)
        {
            if (v.LengthSquared() <= 0f) return null;
            
            var points = new Points(Count);
            for (var i = 0; i < Count; i++)
            {
                points.Add(this[i]);
                points.Add(this[i] + v);
            }
            return points;
        }

        public Polygon? ProjectShape(Vector2 v)
        {
            if (v.LengthSquared() <= 0f) return null;
            
            var points = new Points(Count);
            for (var i = 0; i < Count; i++)
            {
                points.Add(this[i]);
                points.Add(this[i] + v);
            }
            
            return Polygon.FindConvexHull(points);
        }
        public Vector2 GetCentroid()
        {
            if (Count <= 0) return new();
            if (Count == 1) return this[0];
            if (Count == 2) return (this[0] + this[1]) / 2;
            if (Count == 3) return (this[0] + this[1] + this[2]) / 3;
            
            var centroid = new Vector2();
            var area = 0f;
            for (int i = Count - 1; i >= 0; i--)
            {
                var a = this[i];
                var index = ShapeMath.WrapIndex(Count, i - 1);
                var b = this[index];
                float cross = a.X * b.Y - b.X * a.Y; //clockwise 
                area += cross;
                centroid += (a + b) * cross;
            }

            area *= 0.5f;
            return centroid / (area * 6);

            //return GetCentroidMean();
            // Vector2 result = new();

            // for (int i = 0; i < Count; i++)
            // {
            // var a = this[i];
            // var b = this[(i + 1) % Count];
            //// float factor = a.X * b.Y - b.X * a.Y; //clockwise 
            // float factor = a.Y * b.X - a.X * b.Y; //counter clockwise
            // result.X += (a.X + b.X) * factor;
            // result.Y += (a.Y + b.Y) * factor;
            // }

            // return result * (1f / (GetArea() * 6f));
        }

        public float GetPerimeter()
        {
            if (this.Count < 3) return 0f;
            float length = 0f;
            for (int i = 0; i < Count; i++)
            {
                Vector2 w = this[(i + 1)%Count] - this[i];
                length += w.Length();
            }
            return length;
        }
        public float GetPerimeterSquared()
        {
            if (Count < 3) return 0f;
            var lengthSq = 0f;
            for (var i = 0; i < Count; i++)
            {
                var w = this[(i + 1)%Count] - this[i];
                lengthSq += w.LengthSquared();
            }
            return lengthSq;
        }
        public float GetArea()
        {
            if (Count < 3) return 0f;
            var area = 0f;
            for (int i = Count - 1; i >= 0; i--)//backwards to be clockwise
            {
                var a = this[i];
                var index = ShapeMath.WrapIndex(Count, i - 1);
                var b = this[index];
                float cross = a.X * b.Y - b.X * a.Y; //clockwise 
                area += cross;
            }

            return area/ 2f;
        }
        public bool IsClockwise() => GetArea() < 0f;

        public bool IsConvex()
        {
            int num = this.Count;
            bool isPositive = false;

            for (int i = 0; i < num; i++)
            {
                int prevIndex = (i == 0) ? num - 1 : i - 1;
                int nextIndex = (i == num - 1) ? 0 : i + 1;
                var d0 = this[i] - this[prevIndex];
                var d1 = this[nextIndex] - this[i];
                var newIsP = d0.Cross(d1) > 0f;
                if (i == 0) isPositive = true;
                else if (isPositive != newIsP) return false;
            }
            return true;
        }

        public Points ToPoints()
        {
            return new(this);
        }
        public Vector2 GetCentroidMean()
        {
            if (Count <= 0) return new(0f);
            if (Count == 1) return this[0];
            if (Count == 2) return (this[0] + this[1]) / 2;
            if (Count == 3) return (this[0] + this[1] + this[2]) / 3;
            var total = new Vector2(0f);
            foreach (var p in this) { total += p; }
            return total / Count;
        }
        /// <summary>
        /// Computes the length of this polygon's apothem. This will only be valid if
        /// the polygon is regular. More info: http://en.wikipedia.org/wiki/Apothem
        /// </summary>
        /// <returns>Return the length of the apothem.</returns>
        public float GetApothem() => (this.GetCentroid() - (this[0].Lerp(this[1], 0.5f))).Length();

        #endregion
        
        #region Transform
        public void SetPosition(Vector2 newPosition)
        {
            var centroid = GetCentroid();
            var delta = newPosition - centroid;
            ChangePosition(delta);
        }
        public void ChangeRotation(float rotRad)
        {
            if (Count < 3) return;
            var origin = GetCentroid();
            for (int i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                this[i] = origin + w.Rotate(rotRad);
            }
        }
        public void SetRotation(float angleRad)
        {
            if (Count < 3) return;

            var origin = GetCentroid();
            var curAngle = (this[0] - origin).AngleRad();
            var rotRad = ShapeMath.GetShortestAngleRad(curAngle, angleRad);
            ChangeRotation(rotRad, origin);
        }
        public void ScaleSize(float scale)
        {
            if (Count < 3) return;
            var origin = GetCentroid();
            for (int i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                this[i] = origin + w * scale;
            }
        }
        public void ChangeSize(float amount)
        {
            if (Count < 3) return;
            var origin = GetCentroid();
            for (var i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                this[i] = origin + w.ChangeLength(amount);
            }
            
        }
        public void SetSize(float size)
        {
            if (Count < 3) return;
            var origin = GetCentroid();
            for (var i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                this[i] = origin + w.SetLength(size);
            }

        }

        
        public Polygon? SetPositionCopy(Vector2 newPosition)
        {
            if (Count < 3) return null;
            var centroid = GetCentroid();
            var delta = newPosition - centroid;
            return ChangePositionCopy(delta);
        }
        public new Polygon? ChangePositionCopy(Vector2 offset)
        {
            if (Count < 3) return null;
            var newPolygon = new Polygon(this.Count);
            for (int i = 0; i < Count; i++)
            {
                newPolygon.Add(this[i] + offset);
            }

            return newPolygon;
        }
        public new Polygon? ChangeRotationCopy(float rotRad, Vector2 origin)
        {
            if (Count < 3) return null;
            var newPolygon = new Polygon(this.Count);
            for (var i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                newPolygon.Add(origin + w.Rotate(rotRad));
            }

            return newPolygon;
        }

        public Polygon? ChangeRotationCopy(float rotRad)
        {
            if (Count < 3) return null;
            return ChangeRotationCopy(rotRad, GetCentroid());
        }

        public new Polygon? SetRotationCopy(float angleRad, Vector2 origin)
        {
            if (Count < 3) return null;
            var curAngle = (this[0] - origin).AngleRad();
            var rotRad = ShapeMath.GetShortestAngleRad(curAngle, angleRad);
            return ChangeRotationCopy(rotRad, origin);
        }
        public Polygon? SetRotationCopy(float angleRad)
        {
            if (Count < 3) return null;

            var origin = GetCentroid();
            var curAngle = (this[0] - origin).AngleRad();
            var rotRad = ShapeMath.GetShortestAngleRad(curAngle, angleRad);
            return ChangeRotationCopy(rotRad, origin);
        }
        public Polygon? ScaleSizeCopy(float scale)
        {
            if (Count < 3) return null;
            return ScaleSizeCopy(scale, GetCentroid());
        }
        public new Polygon? ScaleSizeCopy(float scale, Vector2 origin)
        {
            if (Count < 3) return null;
            var newPolygon = new Polygon(this.Count);
            
            for (var i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                newPolygon.Add( origin + w * scale);
            }

            return newPolygon;
        }
        public new Polygon? ScaleSizeCopy(Vector2 scale, Vector2 origin)
        {
            if (Count < 3) return null;
            var newPolygon = new Polygon(this.Count);
            
            for (var i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                newPolygon.Add(origin + w * scale);
            }

            return newPolygon;
        }
        public new Polygon? ChangeSizeCopy(float amount, Vector2 origin)
        {
            if (Count < 3) return null;
            var newPolygon = new Polygon(this.Count);
            
            for (var i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                newPolygon.Add(origin + w.ChangeLength(amount));
            }

            return newPolygon;

        }
        public Polygon? ChangeSizeCopy(float amount)
        {
            if (Count < 3) return null;
            return ChangeSizeCopy(amount, GetCentroid());

        }

        public new Polygon? SetSizeCopy(float size, Vector2 origin)
        {
            if (Count < 3) return null;
            var newPolygon = new Polygon(this.Count);
            
            for (var i = 0; i < Count; i++)
            {
                var w = this[i] - origin;
                newPolygon.Add(origin + w.SetLength(size));
            }

            return newPolygon;
        }
        public Polygon? SetSizeCopy(float size)
        {
            if (Count < 3) return null;
            return SetSizeCopy(size, GetCentroid());

        }

       
        public new Polygon? SetTransformCopy(Transform2D transform, Vector2 origin)
        {
            if (Count < 3) return null;
            var newPolygon = SetPositionCopy(transform.Position);
            if (newPolygon == null) return null;
            newPolygon.SetRotation(transform.RotationRad, origin);
            newPolygon.SetSize(transform.ScaledSize.Length, origin);
            return newPolygon;
        }
        
        public new Polygon? ApplyOffsetCopy(Transform2D offset, Vector2 origin)
        {
            if (Count < 3) return null;
            
            var newPolygon = ChangePositionCopy(offset.Position);
            if (newPolygon == null) return null;
            newPolygon.ChangeRotation(offset.RotationRad, origin);
            newPolygon.ChangeSize(offset.ScaledSize.Length, origin);
            return newPolygon;
        }
        
        #endregion
        
        #region Clipping

        public void UnionShapeSelf(Polygon b, FillRule fillRule = FillRule.NonZero)
        {
            var result = Clipper.Union(this.ToClipperPaths(), b.ToClipperPaths(), fillRule);
            if (result.Count > 0)
            {
                this.Clear();
                foreach (var p in result[0])
                {
                    this.Add(p.ToVec2());
                }
            }
            

        }
        
        public bool MergeShapeSelf(Polygon other, float distanceThreshold)
        {
            var cd = GetClosestDistanceTo(other);
            if (cd.DistanceSquared < distanceThreshold * distanceThreshold)
            {
                var fillShape = Polygon.Generate(cd.A, 7, distanceThreshold, distanceThreshold * 2);
                UnionShapeSelf(fillShape, FillRule.NonZero);
                UnionShapeSelf(other, FillRule.NonZero);
            }

            return false;
        }
        public Polygon? MergeShape(Polygon other, float distanceThreshold)
        {
            var cd = GetClosestDistanceTo(other);
            if (cd.DistanceSquared < distanceThreshold * distanceThreshold)
            {
                var fillShape = Polygon.Generate(cd.A, 7, distanceThreshold, distanceThreshold * 2);
                var result = ShapeClipper.Union(this, fillShape, FillRule.NonZero);
                if (result.Count > 0)
                {
                    result = ShapeClipper.Union(result[0].ToPolygon(), other, FillRule.NonZero);
                    if (result.Count > 0) return result[0].ToPolygon();
                }
            }

            return null;
        }
        public (Polygons newShapes, Polygons cutOuts) CutShape(Polygon cutShape)
        {
            var cutOuts = ShapeClipper.Intersect(this, cutShape).ToPolygons(true);
            var newShapes = ShapeClipper.Difference(this, cutShape).ToPolygons(true);

            return (newShapes, cutOuts);
        }
        public (Polygons newShapes, Polygons cutOuts) CutShapeMany(Polygons cutShapes)
        {
            var cutOuts = ShapeClipper.IntersectMany(this, cutShapes).ToPolygons(true);
            var newShapes = ShapeClipper.DifferenceMany(this, cutShapes).ToPolygons(true);
            return (newShapes, cutOuts);
        }

        
        public (Polygons newShapes, Polygons overlaps) CombineShape(Polygon other)
        {
            var overlaps = ShapeClipper.Intersect(this, other).ToPolygons(true);
            var newShapes = ShapeClipper.Union(this, other).ToPolygons(true);
            return (newShapes, overlaps);
        }
        public (Polygons newShapes, Polygons overlaps) CombineShape(Polygons others)
        {
            var overlaps = ShapeClipper.IntersectMany(this, others).ToPolygons(true);
            var newShapes = ShapeClipper.UnionMany(this, others).ToPolygons(true);
            return (newShapes, overlaps);
        }
        public (Polygons newShapes, Polygons cutOuts) CutShapeSimple(Vector2 cutPos, float minCutRadius, float maxCutRadius, int pointCount = 16)
        {
            var cut = Generate(cutPos, pointCount, minCutRadius, maxCutRadius);
            return this.CutShape(cut);
        }
        public (Polygons newShapes, Polygons cutOuts) CutShapeSimple(Segment cutLine, float minSectionLength = 0.025f, float maxSectionLength = 0.1f, float minMagnitude = 0.05f, float maxMagnitude = 0.25f)
        {
            var cut = Generate(cutLine, minMagnitude, maxMagnitude, minSectionLength, maxSectionLength);
            return this.CutShape(cut);
        }
        #endregion
        
        #region Random

        public Vector2 GetRandomPointInside()
        {
            var triangles = Triangulate();
            List<WeightedItem<Triangle>> items = new();
            foreach (var t in triangles)
            {
                items.Add(new(t, (int)t.GetArea()));
            }
            var item = Rng.Instance.PickRandomItem(items.ToArray());
            return item.GetRandomPointInside();
        }
        public Points GetRandomPointsInside(int amount)
        {
            var triangles = Triangulate();
            WeightedItem<Triangle>[] items = new WeightedItem<Triangle>[triangles.Count];
            for (int i = 0; i < items.Length; i++)
            {
                var t = triangles[i];
                items[i] = new(t, (int)t.GetArea());
            }


            List<Triangle> pickedTriangles = Rng.Instance.PickRandomItems(amount, items);
            Points randomPoints = new();
            foreach (var tri in pickedTriangles) randomPoints.Add(tri.GetRandomPointInside());

            return randomPoints;
        }
        public Vector2 GetRandomVertex() { return Rng.Instance.RandCollection(this); }
        public Segment GetRandomEdge() => GetEdges().GetRandomSegment();
        public Vector2 GetRandomPointOnEdge() => GetRandomEdge().GetRandomPoint();
        public Points GetRandomPointsOnEdge(int amount) => GetEdges().GetRandomPoints(amount);
        public Vector2 GetRandomPointConvex()
        {
            var edges = GetEdges();
            var ea = Rng.Instance.RandCollection(edges, true);
            var eb = Rng.Instance.RandCollection(edges);

            var pa = ea.Start.Lerp(ea.End, Rng.Instance.RandF());
            var pb = eb.Start.Lerp(eb.End, Rng.Instance.RandF());
            return pa.Lerp(pb, Rng.Instance.RandF());
        }

        #endregion

        #region Static
        internal static bool ContainsPointCheck(Vector2 a, Vector2 b, Vector2 pointToCheck)
        {
            if (a.Y < pointToCheck.Y && b.Y >= pointToCheck.Y || b.Y < pointToCheck.Y && a.Y >= pointToCheck.Y)
            {
                if (a.X + (pointToCheck.Y - a.Y) / (b.Y - a.Y) * (b.X - a.X) < pointToCheck.X)
                {
                    return true;
                }
            }
            return false;
        }
        
        
        /// <summary>
        /// Triangulates a set of points. Only works with non self intersecting shapes.
        /// </summary>
        /// <param name="points">The points to triangulate. Can be any set of points. (polygons as well) </param>
        /// <returns></returns>
        public static Triangulation TriangulateDelaunay(IEnumerable<Vector2> points)
        {
            var enumerable = points.ToList();
            var supraTriangle = GetBoundingTriangle(enumerable, 2f);
            return TriangulateDelaunay(enumerable, supraTriangle);
        }
        /// <summary>
        /// Triangulates a set of points. Only works with non self intersecting shapes.
        /// </summary>
        /// <param name="points">The points to triangulate. Can be any set of points. (polygons as well) </param>
        /// <param name="supraTriangle">The triangle that encapsulates all the points.</param>
        /// <returns></returns>
        public static Triangulation TriangulateDelaunay(IEnumerable<Vector2> points, Triangle supraTriangle)
        {
            Triangulation triangles = new() { supraTriangle };

            foreach (var p in points)
            {
                Triangulation badTriangles = new();

                //Identify 'bad triangles'
                for (int triIndex = triangles.Count - 1; triIndex >= 0; triIndex--)
                {
                    Triangle triangle = triangles[triIndex];

                    //A 'bad triangle' is defined as a triangle who's CircumCentre contains the current point
                    var circumCircle = triangle.GetCircumCircle();
                    float distSq = Vector2.DistanceSquared(p, circumCircle.Center);
                    if (distSq < circumCircle.Radius * circumCircle.Radius)
                    {
                        badTriangles.Add(triangle);
                        triangles.RemoveAt(triIndex);
                    }
                }

                Segments allEdges = new();
                foreach (var badTriangle in badTriangles) { allEdges.AddRange(badTriangle.GetEdges()); }

                Segments uniqueEdges = GetUniqueSegmentsDelaunay(allEdges);
                //Create new triangles
                for (int i = 0; i < uniqueEdges.Count; i++)
                {
                    var edge = uniqueEdges[i];
                    triangles.Add(new(p, edge));
                }
            }

            //Remove all triangles that share a vertex with the supra triangle to recieve the final triangulation
            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                var t = triangles[i];
                if (t.SharesVertex(supraTriangle)) triangles.RemoveAt(i);
            }


            return triangles;
        }
        private static Segments GetUniqueSegmentsDelaunay(Segments segments)
        {
            Segments uniqueEdges = new();
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                var edge = segments[i];
                if (IsSimilar(segments, edge))
                {
                    uniqueEdges.Add(edge);
                }
            }
            return uniqueEdges;
        }
        private static bool IsSimilar(Segments segments, Segment seg)
        {
            var counter = 0;
            foreach (var segment in segments)
            {
                if (segment.IsSimilar(seg)) counter++;
                if (counter > 1) return false;
            }
            return true;
        }
        

        /// <summary>
        /// Get a rect that encapsulates all points.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Rect GetBoundingBox(IEnumerable<Vector2> points)
        {
            var enumerable = points as Vector2[] ?? points.ToArray();
            if (enumerable.Length < 2) return new();
            var start = enumerable.First();
            Rect r = new(start.X, start.Y, 0, 0);

            foreach (var p in enumerable)
            {
                r = r.Enlarge(p);
            }
            return r;
        }
        /// <summary>
        /// Get a triangle the encapsulates all points.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="marginFactor"> A factor for scaling the final triangle.</param>
        /// <returns></returns>
        public static Triangle GetBoundingTriangle(IEnumerable<Vector2> points, float marginFactor = 1f)
        {
            var bounds = GetBoundingBox(points);
            float dMax = bounds.Size.Max() * marginFactor; // SVec.Max(bounds.BottomRight - bounds.BottomLeft) + margin; //  Mathf.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY) * Margin;
            Vector2 center = bounds.Center;

            ////The float 0.866 is an arbitrary value determined for optimum supra triangle conditions.
            //float x1 = center.X - 0.866f * dMax;
            //float x2 = center.X + 0.866f * dMax;
            //float x3 = center.X;
            //
            //float y1 = center.Y - 0.5f * dMax;
            //float y2 = center.Y - 0.5f * dMax;
            //float y3 = center.Y + dMax;
            //
            //Vector2 a = new(x1, y1);
            //Vector2 b = new(x2, y2);
            //Vector2 c = new(x3, y3);

            Vector2 a = new Vector2(center.X, bounds.BottomLeft.Y + dMax);
            Vector2 b = new Vector2(center.X - dMax * 1.25f, bounds.TopLeft.Y - dMax / 4);
            Vector2 c = new Vector2(center.X + dMax * 1.25f, bounds.TopLeft.Y - dMax / 4);


            return new Triangle(a, b, c);
        }
        
        
        public static List<Vector2> GetSegmentAxis(Polygon p, bool normalized = false)
        {
            if (p.Count <= 1) return new();
            else if (p.Count == 2)
            {
                return new() { p[1] - p[0] };
            }
            List<Vector2> axis = new();
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 start = p[i];
                Vector2 end = p[(i + 1) % p.Count];
                Vector2 a = end - start;
                axis.Add(normalized ? ShapeVec.Normalize(a) : a);
            }
            return axis;
        }
        public static List<Vector2> GetSegmentAxis(Segments edges, bool normalized = false)
        {
            List<Vector2> axis = new();
            foreach (var seg in edges)
            {
                axis.Add(normalized ? seg.Dir : seg.Displacement);
            }
            return axis;
        }

        
        
        public static Polygon GetShape(Points relative, Transform2D transform)
        {
            if (relative.Count < 3) return new();
            Polygon shape = new();
            for (int i = 0; i < relative.Count; i++)
            {
                shape.Add(transform.ApplyTransformTo(relative[i]));
                // shape.Add(pos + ShapeVec.Rotate(relative[i], rotRad) * scale);
            }
            return shape;
        }
        public static Polygon GenerateRelative(int pointCount, float minLength, float maxLength)
        {
            Polygon poly = new();
            float angleStep = ShapeMath.PI * 2.0f / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float randLength = Rng.Instance.RandF(minLength, maxLength);
                var p = ShapeVec.Rotate(ShapeVec.Right(), -angleStep * i) * randLength;
                poly.Add(p);
            }
            return poly;
        }
        
        public static Polygon Generate(Vector2 center, int pointCount, float minLength, float maxLength)
        {
            Polygon points = new();
            float angleStep = ShapeMath.PI * 2.0f / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float randLength = Rng.Instance.RandF(minLength, maxLength);
                Vector2 p = ShapeVec.Rotate(ShapeVec.Right(), -angleStep * i) * randLength;
                p += center;
                points.Add(p);
            }
            return points;
        }
        /// <summary>
        /// Generates a polygon around the given segment. Points are generated ccw around the segment beginning with the segment start.
        /// </summary>
        /// <param name="segment">The segment to build a polygon around.</param>
        /// <param name="magMin">The minimum perpendicular magnitude factor for generating a point. (0-1)</param>
        /// <param name="magMax">The maximum perpendicular magnitude factor for generating a point. (0-1)</param>
        /// <param name="minSectionLength">The minimum factor of the length between points along the line.(0-1)</param>
        /// <param name="maxSectionLength">The maximum factor of the length between points along the line.(0-1)</param>
        /// <returns>Returns the a generated polygon.</returns>
        public static Polygon Generate(Segment segment, float magMin = 0.1f, float magMax = 0.25f, float minSectionLength = 0.025f, float maxSectionLength = 0.1f)
        {
            Polygon poly = new() { segment.Start };
            var dir = segment.Dir;
            var dirRight = dir.GetPerpendicularRight();
            var dirLeft = dir.GetPerpendicularLeft();
            float len = segment.Length;
            float minSectionLengthSq = (minSectionLength * len) * (minSectionLength * len);
            Vector2 cur = segment.Start;
            while (true)
            {
                cur += dir * Rng.Instance.RandF(minSectionLength, maxSectionLength) * len;
                if ((cur - segment.End).LengthSquared() < minSectionLengthSq) break;
                poly.Add(cur + dirRight * Rng.Instance.RandF(magMin, magMax));
            }
            cur = segment.End;
            poly.Add(cur);
            while (true)
            {
                cur -= dir * Rng.Instance.RandF(minSectionLength, maxSectionLength) * len;
                if ((cur - segment.Start).LengthSquared() < minSectionLengthSq) break;
                poly.Add(cur + dirLeft * Rng.Instance.RandF(magMin, magMax));
            }
            return poly;
        }

        
        #endregion

        #region Closest

        public new ClosestDistance GetClosestDistanceTo(Vector2 p)
        {
            if (Count <= 0) return new();
            if (Count == 1) return new(this[0], p);
            if (Count == 2) return new(Segment.GetClosestPoint(this[0], this[1], p), p);
            if (Count == 3) return new(Triangle.GetClosestPoint(this[0], this[1], this[2], p), p);
            if (Count == 4) return new(Quad.GetClosestPoint(this[0], this[1], this[2], this[3], p), p);

            var cp = new Vector2();
            var minDisSq = float.PositiveInfinity;
            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];
                var next = Segment.GetClosestPoint(start, end, p);
                var disSq = (next - p).LengthSquared();
                if (disSq < minDisSq)
                {
                    minDisSq = disSq;
                    cp = next;
                }

            }

            return new(cp, p);
        }

        public ClosestDistance GetClosestDistanceTo(Segment segment)
        {
            if (Count <= 0) return new();
            if (Count == 1)
            {
                var cp = Segment.GetClosestPoint(segment.Start, segment.End, this[0]);
                return new(this[0], cp);
            }
            if (Count == 2) return new Segment(this[0], this[1]).GetClosestDistanceTo(segment);
            if (Count == 3) return new Triangle(this[0], this[1], this[2]).GetClosestDistanceTo(segment);
            if (Count == 4) return new Quad(this[0], this[1], this[2], this[3]).GetClosestDistanceTo(segment);
            
            ClosestDistance closestDistance = new(new(), new(), float.PositiveInfinity);
            
            for (var i = 0; i < Count; i++)
            {
                var p1 = this[i];
                var p2 = this[(i + 1) % Count];
                
                var next = Segment.GetClosestPoint(segment.Start, segment.End, p1);
                var cd = new ClosestDistance(p1, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(segment.Start, segment.End, p2);
                cd = new ClosestDistance(p2, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, segment.Start);
                cd = new ClosestDistance(next, segment.Start);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, segment.End);
                cd = new ClosestDistance(next, segment.End);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
            }
            
            return closestDistance;
        }
        public ClosestDistance GetClosestDistanceTo(Circle circle)
        {
            if (Count <= 0) return new();
            if (Count == 1)
            {
                var cp = Circle.GetClosestPoint(circle.Center, circle.Radius, this[0]);
                return new(this[0], cp);
            }
            if (Count == 2) return new Segment(this[0], this[1]).GetClosestDistanceTo(circle);
            if (Count == 3) return new Triangle(this[0], this[1], this[2]).GetClosestDistanceTo(circle);
            if (Count == 4) return new Quad(this[0], this[1], this[2], this[3]).GetClosestDistanceTo(circle);
            
            Vector2 closestPoint = new();
            Vector2 displacement = new();
            float minDisSq = float.PositiveInfinity;
            
            for (var i = 0; i < Count; i++)
            {
                var p1 = this[i];
                var p2 = this[(i + 1) % Count];
                
                var next = Segment.GetClosestPoint(p1, p2, circle.Center);
                var w = (next - circle.Center);
                var disSq = w.LengthSquared();
                if (disSq < minDisSq)
                {
                    minDisSq = disSq;
                    displacement = w;
                    closestPoint = next;
                }
            }

            var dir = displacement.Normalize();
            return new(closestPoint, circle.Center + dir * circle.Radius);
        }
        public ClosestDistance GetClosestDistanceTo(Triangle triangle)
        {
            if (Count <= 0) return new();
            if (Count == 1)
            {
                var cp = Triangle.GetClosestPoint(triangle.A, triangle.B, triangle.C, this[0]);
                return new(this[0], cp);
            }
            if (Count == 2) return new Segment(this[0], this[1]).GetClosestDistanceTo(triangle);
            if (Count == 3) return new Triangle(this[0], this[1], this[2]).GetClosestDistanceTo(triangle);
            if (Count == 4) return new Quad(this[0], this[1], this[2], this[3]).GetClosestDistanceTo(triangle);
            
            ClosestDistance closestDistance = new(new(), new(), float.PositiveInfinity);
            
            for (var i = 0; i < Count; i++)
            {
                var p1 = this[i];
                var p2 = this[(i + 1) % Count];
                
                var next = Triangle.GetClosestPoint(triangle.A, triangle.B, triangle.C, p1);
                var cd = new ClosestDistance(p1, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Triangle.GetClosestPoint(triangle.A, triangle.B, triangle.C, p2);
                cd = new ClosestDistance(p2, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, triangle.A);
                cd = new ClosestDistance(next, triangle.A);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, triangle.B);
                cd = new ClosestDistance(next, triangle.B);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, triangle.C);
                cd = new ClosestDistance( next, triangle.C);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
            }
            return closestDistance;
        }
        public ClosestDistance GetClosestDistanceTo(Quad quad)
        {
            if (Count <= 0) return new();
            if (Count == 1)
            {
                var cp = Quad.GetClosestPoint(quad.A, quad.B, quad.C, quad.D, this[0]);
                return new(this[0], cp);
            }
            if (Count == 2) return new Segment(this[0], this[1]).GetClosestDistanceTo(quad);
            if (Count == 3) return new Triangle(this[0], this[1], this[2]).GetClosestDistanceTo(quad);
            if (Count == 4) return new Quad(this[0], this[1], this[2], this[3]).GetClosestDistanceTo(quad);
            
            ClosestDistance closestDistance = new(new(), new(), float.PositiveInfinity);
            
            for (var i = 0; i < Count; i++)
            {
                var p1 = this[i];
                var p2 = this[(i + 1) % Count];
                
                var next = Quad.GetClosestPoint(quad.A, quad.B, quad.C, quad.D, p1);
                var cd = new ClosestDistance(p1, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Quad.GetClosestPoint(quad.A, quad.B, quad.C, quad.D, p2);
                cd = new ClosestDistance(p2, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, quad.A);
                cd = new ClosestDistance(next, quad.A);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, quad.B);
                cd = new ClosestDistance(next, quad.B);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, quad.C);
                cd = new ClosestDistance(next, quad.C);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, quad.D);
                cd = new ClosestDistance(next, quad.D);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
            }
            return closestDistance;
        }
        public ClosestDistance GetClosestDistanceTo(Rect rect)
        {
            if (Count <= 0) return new();
            if (Count == 1)
            {
                var cp = Quad.GetClosestPoint(rect.A, rect.B, rect.C, rect.D, this[0]);
                return new(this[0], cp);
            }
            if (Count == 2) return new Segment(this[0], this[1]).GetClosestDistanceTo(rect);
            if (Count == 3) return new Triangle(this[0], this[1], this[2]).GetClosestDistanceTo(rect);
            if (Count == 4) return new Quad(this[0], this[1], this[2], this[3]).GetClosestDistanceTo(rect);
            
            ClosestDistance closestDistance = new(new(), new(), float.PositiveInfinity);
            
            for (var i = 0; i < Count; i++)
            {
                var p1 = this[i];
                var p2 = this[(i + 1) % Count];
                
                var next = Quad.GetClosestPoint(rect.A, rect.B, rect.C, rect.D, p1);
                var cd = new ClosestDistance(p1, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Quad.GetClosestPoint(rect.A, rect.B, rect.C, rect.D, p2);
                cd = new ClosestDistance(p2, next);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, rect.A);
                cd = new ClosestDistance(next, rect.A);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, rect.B);
                cd = new ClosestDistance(next, rect.B);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, rect.C);
                cd = new ClosestDistance(next, rect.C);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                
                next = Segment.GetClosestPoint(p1, p2, rect.D);
                cd = new ClosestDistance(next, rect.D);
                if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
            }
            return closestDistance;
        }
        public ClosestDistance GetClosestDistanceTo(Polygon polygon)
        {
            if (Count <= 0 || polygon.Count <= 0) return new();
            if (Count == 1) return polygon.GetClosestDistanceTo(this[0]).ReversePoints();
            if (Count == 2) return new Segment(this[0], this[1]).GetClosestDistanceTo(polygon);
            if (Count == 3) return new Triangle(this[0], this[1], this[2]).GetClosestDistanceTo(polygon);
            if (Count == 4) return new Quad(this[0], this[1], this[2], this[3]).GetClosestDistanceTo(polygon);
            if (polygon.Count == 1) return GetClosestDistanceTo(polygon[0]);
            if (polygon.Count == 2) return GetClosestDistanceTo(new Segment(polygon[0], polygon[1]));
            if (polygon.Count == 3) return GetClosestDistanceTo(new Triangle(polygon[0], polygon[1], polygon[2]));
            if (polygon.Count == 4) return GetClosestDistanceTo(new Quad(polygon[0], polygon[1], polygon[2], polygon[3]));
            
            ClosestDistance closestDistance = new(new(), new(), float.PositiveInfinity);
            
            for (var i = 0; i < Count; i++)
            {
                var self1 = this[i];
                var self2 = this[(i + 1) % Count];

                for (var j = 0; j < polygon.Count; j++)
                {
                    var other1 = polygon[j];
                    var other2 = polygon[(j + 1) % polygon.Count];

                    var next = Segment.GetClosestPoint(self1, self2, other1);
                    var cd = new ClosestDistance(next, other1);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                    
                    next = Segment.GetClosestPoint(self1, self2, other2);
                    cd = new ClosestDistance(next, other2);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                    
                    next = Segment.GetClosestPoint(other1, other2, self1);
                    cd = new ClosestDistance(self1, next);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                    
                    next = Segment.GetClosestPoint(other1, other2, self2);
                    cd = new ClosestDistance(self2, next);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                }
            }
            return closestDistance;
        }
        public ClosestDistance GetClosestDistanceTo(Polyline polyline)
        {
            if (Count <= 0 || polyline.Count <= 0) return new();
            if (Count == 1) return polyline.GetClosestDistanceTo(this[0]).ReversePoints();
            if (Count == 2) return new Segment(this[0], this[1]).GetClosestDistanceTo(polyline);
            if (Count == 3) return new Triangle(this[0], this[1], this[2]).GetClosestDistanceTo(polyline);
            if (Count == 4) return new Quad(this[0], this[1], this[2], this[3]).GetClosestDistanceTo(polyline);
            if (polyline.Count == 1) return GetClosestDistanceTo(polyline[0]);
            if (polyline.Count == 2) return GetClosestDistanceTo(new Segment(polyline[0], polyline[1]));
            
            ClosestDistance closestDistance = new(new(), new(), float.PositiveInfinity);
            
            for (var i = 0; i < Count; i++)
            {
                var self1 = this[i];
                var self2 = this[(i + 1) % Count];

                for (var j = 0; j < polyline.Count - 1; j++)
                {
                    var other1 = polyline[j];
                    var other2 = polyline[(j + 1) % polyline.Count];

                    var next = Segment.GetClosestPoint(self1, self2, other1);
                    var cd = new ClosestDistance(next, other1);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                    
                    next = Segment.GetClosestPoint(self1, self2, other2);
                    cd = new ClosestDistance(next, other2);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                    
                    next = Segment.GetClosestPoint(other1, other2, self1);
                    cd = new ClosestDistance(self1, next);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                    
                    next = Segment.GetClosestPoint(other1, other2, self2);
                    cd = new ClosestDistance(self2, next);
                    if (cd.DistanceSquared < closestDistance.DistanceSquared) closestDistance = cd;
                }
            }
            return closestDistance;
        }
        
        
        // public (Vector2 a, Vector2 b) GetClosestDistance(Polygon other)
        // {
        //     List<ClosestPoint> otherPoints = new();
        //     List<ClosestPoint> selfPoints = new();
        //     
        //     foreach (var p in this)
        //     {
        //         var cp = other.GetClosestPoint(p);
        //         otherPoints.Add(cp);
        //     }
        //     foreach (var p in other)
        //     {
        //         var cp = GetClosestPoint(p);
        //         selfPoints.Add(cp);
        //     }
        //
        //     float minDisQq = float.PositiveInfinity;
        //     Vector2 cSelf = new();
        //     Vector2 cOther = new();
        //     foreach (var sp in selfPoints)
        //     {
        //         foreach (var op in otherPoints)
        //         {
        //             var dSq = (sp.Closest.Point - op.Closest.Point).LengthSquared();
        //             if (dSq < minDisQq)
        //             {
        //                 minDisQq = dSq;
        //                 cSelf = sp.Closest.Point;
        //                 cOther = op.Closest.Point;
        //             }
        //         }
        //     }
        //
        //     return (cSelf, cOther);
        // }
        //
        
        public int GetClosestEdgePointByIndex(Vector2 p)
        {
            if (Count <= 0) return -1;
            if (Count == 1) return 0;

            float minD = float.PositiveInfinity;
            int closestIndex = -1;

            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];
                var edge = new Segment(start, end);

                Vector2 closest = edge.GetClosestCollisionPoint(p).Point;
                float d = (closest - p).LengthSquared();
                if (d < minD)
                {
                    closestIndex = i;
                    minD = d;
                }
            }
            return closestIndex;
        }
        // internal ClosestPoint GetClosestPoint(Vector2 p)
        // {
        //     var cp = GetEdges().GetClosestCollisionPoint(p);
        //     return new(cp, (cp.Point - p).Length());
        // }
        //
        public CollisionPoint GetClosestCollisionPoint(Vector2 p) => GetEdges().GetClosestCollisionPoint(p);

        public ClosestSegment GetClosestSegment(Vector2 p)
        {
            if (Count <= 1) return new();

            var closestSegment = new Segment(this[0], this[1]);
            var closestDistance = closestSegment.GetClosestDistanceTo(p);
            
            for (var i = 1; i < Count; i++)
            {
                var p1 = this[i];
                var p2 = this[(i + 1) % Count];
                var segment = new Segment(p1, p2);
                var cd = segment.GetClosestDistanceTo(p);
                if (cd.DistanceSquared < closestDistance.DistanceSquared)
                {
                    closestDistance = cd;
                    closestSegment = segment;
                }

            }

            return new(closestSegment, closestDistance);
        }
        #endregion
        
        #region Contains
        public bool ContainsPoint(Vector2 p) { return IsPointInPoly(p); }

        public bool ContainsCollisionObject(CollisionObject collisionObject)
        {
            if (!collisionObject.HasColliders) return false;
            foreach (var collider in collisionObject.Colliders)
            {
                if (!ContainsCollider(collider)) return false;
            }

            return true;
        }
        public bool ContainsCollider(Collider collider)
        {
            switch (collider.GetShapeType())
            {
                case ShapeType.Circle: return ContainsShape(collider.GetCircleShape());
                case ShapeType.Segment: return ContainsShape(collider.GetSegmentShape());
                case ShapeType.Triangle: return ContainsShape(collider.GetTriangleShape());
                case ShapeType.Quad: return ContainsShape(collider.GetQuadShape());
                case ShapeType.Rect: return ContainsShape(collider.GetRectShape());
                case ShapeType.Poly: return ContainsShape(collider.GetPolygonShape());
                case ShapeType.PolyLine: return ContainsShape(collider.GetPolylineShape());
            }

            return false;
        }
        public bool ContainsShape(Segment segment) => ContainsPoints(segment.Start, segment.End);
        public bool ContainsShape(Circle circle) => ContainsPoints(circle.Top, circle.Left, circle.Bottom, circle.Right);
        public bool ContainsShape(Rect rect) => ContainsPoints(rect.TopLeft, rect.BottomLeft, rect.BottomRight, rect.TopRight);
        public bool ContainsShape(Triangle triangle) => ContainsPoints(triangle.A, triangle.B, triangle.C);
        public bool ContainsShape(Quad quad) => ContainsPoints(quad.A, quad.B, quad.C, quad.D);
        public bool ContainsShape(Points points)
        {
            if (points.Count <= 0) return false;
            foreach (var p in points)
            {
                if (!ContainsPoint(p)) return false;
            }
            return true;
        }
        
        public bool IsPointInPoly(Vector2 p)
        {
            var oddNodes = false;
            int num = Count;
            int j = num - 1;
            for (int i = 0; i < num; i++)
            {
                var vi = this[i];
                var vj = this[j];
                if (ContainsPointCheck(vi, vj, p)) oddNodes = !oddNodes;
                j = i;
            }

            return oddNodes;
        }
        public bool ContainsPoints(Vector2 a, Vector2 b)
        {
            var oddNodesA = false;
            var oddNodesB = false;
            int num = Count;
            int j = num - 1;
            for (var i = 0; i < num; i++)
            {
                var vi = this[i];
                var vj = this[j];
                if(ContainsPointCheck(vi, vj, a)) oddNodesA = !oddNodesA;
                if(ContainsPointCheck(vi, vj, b)) oddNodesB = !oddNodesB;
                
                j = i;
            }

            return oddNodesA && oddNodesB;
        }
        public bool ContainsPoints(Vector2 a, Vector2 b, Vector2 c)
        {
            var oddNodesA = false;
            var oddNodesB = false;
            var oddNodesC = false;
            int num = Count;
            int j = num - 1;
            for (int i = 0; i < num; i++)
            {
                var vi = this[i];
                var vj = this[j];
                if(ContainsPointCheck(vi, vj, a)) oddNodesA = !oddNodesA;
                if(ContainsPointCheck(vi, vj, b)) oddNodesB = !oddNodesB;
                if(ContainsPointCheck(vi, vj, c)) oddNodesC = !oddNodesC;
                
                j = i;
            }

            return oddNodesA && oddNodesB && oddNodesC;
        }
        public bool ContainsPoints(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var oddNodesA = false;
            var oddNodesB = false;
            var oddNodesC = false;
            var oddNodesD = false;
            int num = Count;
            int j = num - 1;
            for (int i = 0; i < num; i++)
            {
                var vi = this[i];
                var vj = this[j];
                if(ContainsPointCheck(vi, vj, a)) oddNodesA = !oddNodesA;
                if(ContainsPointCheck(vi, vj, b)) oddNodesB = !oddNodesB;
                if(ContainsPointCheck(vi, vj, c)) oddNodesC = !oddNodesC;
                if(ContainsPointCheck(vi, vj, d)) oddNodesD = !oddNodesD;
                
                j = i;
            }

            return oddNodesA && oddNodesB && oddNodesC && oddNodesD;
        }

        
        #endregion
        
        #region Overlap
        
        public bool Overlap(Collider collider)
        {
            if (!collider.Enabled) return false;

            switch (collider.GetShapeType())
            {
                case ShapeType.Circle:
                    var c = collider.GetCircleShape();
                    return OverlapShape(c);
                case ShapeType.Segment:
                    var s = collider.GetSegmentShape();
                    return OverlapShape(s);
                case ShapeType.Triangle:
                    var t = collider.GetTriangleShape();
                    return OverlapShape(t);
                case ShapeType.Rect:
                    var r = collider.GetRectShape();
                    return OverlapShape(r);
                case ShapeType.Quad:
                    var q = collider.GetQuadShape();
                    return OverlapShape(q);
                case ShapeType.Poly:
                    var p = collider.GetPolygonShape();
                    return OverlapShape(p);
                case ShapeType.PolyLine:
                    var pl = collider.GetPolylineShape();
                    return OverlapShape(pl);
            }

            return false;
        }
        public bool OverlapShape(Segment s) => s.OverlapShape(this);
        public bool OverlapShape(Circle c) => c.OverlapShape(this);
        public bool OverlapShape(Triangle t) => t.OverlapShape(this);
        public bool OverlapShape(Rect r) => r.OverlapShape(this);
        public bool OverlapShape(Quad q) => q.OverlapShape(this);
        public bool OverlapShape(Polygon b)
        {
            if (Count < 3 || b.Count < 3) return false;
            
            var oddNodesThis = false;
            var oddNodesB = false;
            var containsPointBCheckFinished = false;

            var pointToCeckThis = this[0];
            var pointToCeckB = b[0];
            
            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];
                
                for (int j = 0; j < b.Count; j++)
                {
                    var bStart = b[j];
                    var bEnd = b[(j + 1) % b.Count];
                    if (Segment.OverlapSegmentSegment(start, end, bStart, bEnd)) return true;
                    
                    if (containsPointBCheckFinished) continue;
                    if(Polygon.ContainsPointCheck(bStart, bEnd, pointToCeckThis)) oddNodesB = !oddNodesB;
                }

                if (!containsPointBCheckFinished)
                {
                    if (oddNodesB) return true;
                    containsPointBCheckFinished = true;
                }
               
                if(Polygon.ContainsPointCheck(start, end, pointToCeckB)) oddNodesThis = !oddNodesThis;
            }

            return oddNodesThis || oddNodesB;
        }
        public bool OverlapShape(Polyline pl)
        {
            if (Count < 3 || pl.Count < 2) return false;
            
            var oddNodes = false;
            var pointToCeck = pl[0];

            
            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];
                
                for (int j = 0; j < pl.Count - 1; j++)
                {
                    var bStart = pl[j];
                    var bEnd = pl[(j + 1) % pl.Count];
                    if (Segment.OverlapSegmentSegment(start, end, bStart, bEnd)) return true;
                }

                if(Polygon.ContainsPointCheck(start, end, pointToCeck)) oddNodes = !oddNodes;
            }

            return oddNodes;
        }
        public bool OverlapShape(Segments segments)
        {
            if (Count < 3 || segments.Count <= 0) return false;
            
            var oddNodes = false;
            var pointToCeck = segments[0].Start;

            
            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];

                foreach (var seg in segments)
                {
                    if (Segment.OverlapSegmentSegment(start, end, seg.Start, seg.End)) return true;
                }

                if(Polygon.ContainsPointCheck(start, end, pointToCeck)) oddNodes = !oddNodes;
            }

            return oddNodes;
        }


        #endregion

        #region Intersect
        public CollisionPoints? Intersect(Collider collider)
        {
            if (!collider.Enabled) return null;

            switch (collider.GetShapeType())
            {
                case ShapeType.Circle:
                    var c = collider.GetCircleShape();
                    return IntersectShape(c);
                case ShapeType.Segment:
                    var s = collider.GetSegmentShape();
                    return IntersectShape(s);
                case ShapeType.Triangle:
                    var t = collider.GetTriangleShape();
                    return IntersectShape(t);
                case ShapeType.Rect:
                    var r = collider.GetRectShape();
                    return IntersectShape(r);
                case ShapeType.Quad:
                    var q = collider.GetQuadShape();
                    return IntersectShape(q);
                case ShapeType.Poly:
                    var p = collider.GetPolygonShape();
                    return IntersectShape(p);
                case ShapeType.PolyLine:
                    var pl = collider.GetPolylineShape();
                    return IntersectShape(pl);
            }

            return null;
        }
        public CollisionPoints? IntersectShape(Segment s)
        {
            if (Count < 3) return null;
            CollisionPoints? points = null;
            CollisionPoint? colPoint = null;
            for (var i = 0; i < Count; i++)
            {
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],s.Start, s.End);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Circle c)
        {
            if (Count < 3) return null;
            
            CollisionPoints? points = null;
            (CollisionPoint? a, CollisionPoint? b) result;
            
            for (var i = 0; i < Count; i++)
            {
                result = Segment.IntersectSegmentCircle(this[i], this[(i + 1) % Count], c.Center, c.Radius);
                if (result.a != null || result.b != null)
                {
                    points ??= new();
                    if(result.a != null) points.Add((CollisionPoint)result.a);
                    if(result.b != null) points.Add((CollisionPoint)result.b);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Triangle t)
        {
            if (Count < 3) return null;

            CollisionPoints? points = null;
            CollisionPoint? colPoint = null;
            for (var i = 0; i < Count; i++)
            {
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], t.A, t.B);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], t.B, t.C);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], t.C, t.A);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Rect r)
        {
            if (Count < 3) return null;

            CollisionPoints? points = null;

            var a = r.TopLeft;
            var b = r.BottomLeft;
            var c = r.BottomRight;
            var d = r.TopRight;
            
            for (var i = 0; i < Count; i++)
            {
                var colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], a, b);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], b, c);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], c, d);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], d, a);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Quad q)
        {
            if (Count < 3) return null;

            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],q.A, q.B);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count], q.B, q.C);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],q.C, q.D);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],q.D, q.A);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Polygon b)
        {
            if (Count < 3 || b.Count < 3) return null;
            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                for (var j = 0; j < b.Count; j++)
                {
                    var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],b[j], b[(j + 1) % b.Count]);
                    if (colPoint != null)
                    {
                        points ??= new();
                        points.Add((CollisionPoint)colPoint);
                    }
                }
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Polyline pl)
        {
            if (Count < 3 || pl.Count < 2) return null;
            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                for (var j = 0; j < pl.Count - 1; j++)
                {
                    var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],pl[j], pl[(j + 1) % pl.Count]);
                    if (colPoint != null)
                    {
                        points ??= new();
                        points.Add((CollisionPoint)colPoint);
                    }
                }
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Segments segments)
        {
            if (Count < 3 || segments.Count <= 0) return null;
            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                foreach (var seg in segments)
                {
                    var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],seg.Start, seg.End);
                    if (colPoint != null)
                    {
                        points ??= new();
                        points.Add((CollisionPoint)colPoint);
                    }
                }
            }
            return points;
        }

        #endregion

        #region Convex Hull
        //ALternative algorithms
            //https://en.wikipedia.org/wiki/Graham_scan
            //https://en.wikipedia.org/wiki/Chan%27s_algorithm
            
        //GiftWrapping
        //https://www.youtube.com/watch?v=YNyULRrydVI -> coding train
        //https://en.wikipedia.org/wiki/Gift_wrapping_algorithm -> wiki
        public static Polygon FindConvexHull(List<Vector2> points) => ConvexHull_JarvisMarch(points);
        public static Polygon FindConvexHull(Points points) => ConvexHull_JarvisMarch(points);
        public static Polygon FindConvexHull(params Vector2[] points) => ConvexHull_JarvisMarch(points.ToList());
        public static Polygon FindConvexHull(Polygon points) => ConvexHull_JarvisMarch(points);
        public static Polygon FindConvexHull(params Polygon[] shapes)
        {
            var allPoints = new List<Vector2>();
            foreach (var shape in shapes)
            {
                allPoints.AddRange(shape);
            }
            return ConvexHull_JarvisMarch(allPoints);
        }
        
        #endregion
        
        #region Jarvis March Algorithm (Find Convex Hull)

        //SOURCE https://github.com/allfii/ConvexHull/tree/master
        
        private static int Turn_JarvisMarch(Vector2 p, Vector2 q, Vector2 r)
        {
            return ((q.X - p.X) * (r.Y - p.Y) - (r.X - p.X) * (q.Y - p.Y)).CompareTo(0);
            // return ((q.getX() - p.getX()) * (r.getY() - p.getY()) - (r.getX() - p.getX()) * (q.getY() - p.getY())).CompareTo(0);
        }
        private static Vector2 NextHullPoint_JarvisMarch(List<Vector2> points, Vector2 p)
        {
            // const int TurnLeft = 1;
            const int turnRight = -1;
            const int turnNone = 0;
            var q = p;
            int t;
            foreach (var r in points)
            {
                t = Turn_JarvisMarch(p, q, r);
                if (t == turnRight || t == turnNone && p.DistanceSquared(r) > p.DistanceSquared(q)) // dist(p, r) > dist(p, q))
                    q = r;
            }
            return q;
        }
        private static Polygon ConvexHull_JarvisMarch(List<Vector2> points)
        {
            var hull = new List<Vector2>();
            foreach (var p in points)
            {
                if (hull.Count == 0)
                    hull.Add(p);
                else
                {
                    if (hull[0].X > p.X)
                        hull[0] = p;
                    else if (ShapeMath.EqualsF(hull[0].X, p.X))
                        if (hull[0].Y > p.Y)
                            hull[0] = p;
                }
            }
            var counter = 0;
            while (counter < hull.Count)
            {
                var q = NextHullPoint_JarvisMarch(points, hull[counter]);
                if (q != hull[0])
                {
                    hull.Add(q);
                }
                counter++;
            }
            return new Polygon(hull);
        }
        #endregion
    }
}

