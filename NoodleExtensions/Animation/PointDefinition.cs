﻿namespace NoodleExtensions.Animation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using static NoodleExtensions.Plugin;

    public class PointDefinition
    {
        private List<PointData> _points;
        private List<PointData> _linearPoints;

        public static PointDefinition DynamicToPointData(dynamic dyn)
        {
            IEnumerable<List<object>> points = ((IEnumerable<object>)dyn)
                        ?.Cast<List<object>>();
            if (points == null)
            {
                return null;
            }

            PointDefinition pointData = new PointDefinition();
            foreach (List<object> rawPoint in points)
            {
                int flagIndex = -1;
                int cachedCount = rawPoint.Count;
                for (int i = cachedCount - 1; i > 0; i--)
                {
                    if (rawPoint[i] is string)
                    {
                        flagIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                Functions easing = Functions.easeLinear;
                bool spline = false;
                List<object> copiedList = rawPoint.ToList();
                if (flagIndex != -1)
                {
                    List<string> flags = rawPoint.GetRange(flagIndex, cachedCount - flagIndex).Cast<string>().ToList();
                    copiedList.RemoveRange(flagIndex, cachedCount - flagIndex);

                    string easingString = flags.Where(n => n.StartsWith("ease")).FirstOrDefault();
                    if (easingString != null)
                    {
                        easing = (Functions)Enum.Parse(typeof(Functions), easingString);
                    }

                    // TODO: add more spicy splines
                    string splineString = flags.Where(n => n.StartsWith("spline")).FirstOrDefault();
                    if (splineString == "splineCatmullRom")
                    {
                        spline = true;
                    }
                }

                if (copiedList.Count() == 2)
                {
                    Vector2 vector = new Vector2(Convert.ToSingle(copiedList[0]), Convert.ToSingle(copiedList[1]));
                    pointData.LinearAdd(new PointData(vector, easing));
                }
                else if (copiedList.Count() == 4)
                {
                    Vector4 vector = new Vector4(Convert.ToSingle(copiedList[0]), Convert.ToSingle(copiedList[1]), Convert.ToSingle(copiedList[2]), Convert.ToSingle(copiedList[3]));
                    pointData.Add(new PointData(vector, easing, spline));
                }
                else
                {
                    Vector5 vector = new Vector5(Convert.ToSingle(copiedList[0]), Convert.ToSingle(copiedList[1]), Convert.ToSingle(copiedList[2]), Convert.ToSingle(copiedList[3]), Convert.ToSingle(copiedList[4]));
                    pointData.Add(new PointData(vector, easing));
                }
            }

            return pointData;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder("{ ");
            if (_points != null)
            {
                _points.ForEach(n => stringBuilder.Append($"{n.Point.ToString()} "));
            }
            else
            {
                if (_linearPoints != null)
                {
                    _linearPoints.ForEach(n => stringBuilder.Append($"{n.LinearPoint.ToString()} "));
                }
            }

            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }

        public Vector3 Interpolate(float time)
        {
            if (_points == null || _points.Count == 0)
            {
                return _vectorZero;
            }

            if (time <= 0)
            {
                return _points.First().Point;
            }

            int pointsCount = _points.Count;
            for (int i = 0; i < pointsCount; i++)
            {
                if (_points[i].Point.w > time)
                {
                    if (i == 0)
                    {
                        return _points.First().Point;
                    }

                    float normalTime = (time - _points[i - 1].Point.w) / (_points[i].Point.w - _points[i - 1].Point.w);
                    normalTime = Easings.Interpolate(normalTime, _points[i].Easing);
                    if (_points[i].Smooth)
                    {
                        return SmoothVectorLerp(_points, i - 1, i, normalTime);
                    }
                    else
                    {
                        return Vector3.LerpUnclamped(_points[i - 1].Point, _points[i].Point, normalTime);
                    }
                }
            }

            return _points.Last().Point;
        }

        public Quaternion InterpolateQuaternion(float time)
        {
            if (_points == null || _points.Count == 0)
            {
                return _quaternionIdentity;
            }

            if (time <= 0)
            {
                return Quaternion.Euler(_points.First().Point);
            }

            int pointsCount = _points.Count;
            for (int i = 0; i < pointsCount; i++)
            {
                if (_points[i].Point.w > time)
                {
                    if (i == 0)
                    {
                        return Quaternion.Euler(_points.First().Point);
                    }

                    Quaternion quaternionOne = Quaternion.Euler(_points[i - 1].Point);
                    Quaternion quaternionTwo = Quaternion.Euler(_points[i].Point);
                    float normalTime = (time - _points[i - 1].Point.w) / (_points[i].Point.w - _points[i - 1].Point.w);
                    normalTime = Easings.Interpolate(normalTime, _points[i].Easing);
                    return Quaternion.LerpUnclamped(quaternionOne, quaternionTwo, normalTime);
                }
            }

            return Quaternion.Euler(_points.Last().Point);
        }

        // Kind of a sloppy way of implementing this, but hell if it works
        public float InterpolateLinear(float time)
        {
            if (_linearPoints == null || _linearPoints.Count == 0)
            {
                return 0;
            }

            if (time <= 0)
            {
                return _linearPoints.First().LinearPoint.x;
            }

            int pointsCount = _linearPoints.Count;
            for (int i = 0; i < pointsCount; i++)
            {
                if (_linearPoints[i].LinearPoint.y > time)
                {
                    if (i == 0)
                    {
                        return _linearPoints.First().LinearPoint.x;
                    }

                    float normalTime = (time - _linearPoints[i - 1].LinearPoint.y) / (_linearPoints[i].LinearPoint.y - _linearPoints[i - 1].LinearPoint.y);
                    normalTime = Easings.Interpolate(normalTime, _linearPoints[i].Easing);
                    return Mathf.LerpUnclamped(_linearPoints[i - 1].LinearPoint.x, _linearPoints[i].LinearPoint.x, normalTime);
                }
            }

            return _linearPoints.Last().LinearPoint.x;
        }

        public Vector4 InterpolateVector4(float time)
        {
            if (_points == null || _points.Count == 0)
            {
                return Vector4.zero;
            }

            if (time <= 0)
            {
                return _points.First().Point;
            }

            int pointsCount = _points.Count;
            for (int i = 0; i < pointsCount; i++)
            {
                if (_points[i].Vector4Point.v > time)
                {
                    if (i == 0)
                    {
                        return _points.First().Vector4Point;
                    }

                    float normalTime = (time - _points[i - 1].Vector4Point.v) / (_points[i].Vector4Point.v - _points[i - 1].Vector4Point.v);
                    normalTime = Easings.Interpolate(normalTime, _points[i].Easing);
                    return Vector4.LerpUnclamped(_points[i - 1].Vector4Point, _points[i].Vector4Point, normalTime);
                }
            }

            return _points.Last().Vector4Point;
        }

        private static Vector3 SmoothVectorLerp(List<PointData> points, int a, int b, float time)
        {
            // Catmull-Rom Spline
            Vector3 p0 = a - 1 < 0 ? points[a].Point : points[a - 1].Point;
            Vector3 p1 = points[a].Point;
            Vector3 p2 = points[b].Point;
            Vector3 p3 = b + 1 > points.Count - 1 ? points[b].Point : points[b + 1].Point;

            float t = time;

            float tt = t * t;
            float ttt = tt * t;

            float q0 = -ttt + (2.0f * tt) - t;
            float q1 = (3.0f * ttt) - (5.0f * tt) + 2.0f;
            float q2 = (-3.0f * ttt) + (4.0f * tt) + t;
            float q3 = ttt - tt;

            Vector3 c = 0.5f * ((p0 * q0) + (p1 * q1) + (p2 * q2) + (p3 * q3));

            return c;
        }

        private void Add(PointData point)
        {
            if (_points == null)
            {
                _points = new List<PointData>();
            }

            _points.Add(point);
        }

        private void LinearAdd(PointData point)
        {
            if (_linearPoints == null)
            {
                _linearPoints = new List<PointData>();
            }

            _linearPoints.Add(point);
        }

        private struct Vector5
        {
            internal Vector5(float x, float y, float z, float w, float v)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
                this.v = v;
            }

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable SA1300 // Element should begin with upper-case letter
            internal float x { get; }

            internal float y { get; }

            internal float z { get; }

            internal float w { get; }

            internal float v { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter

            public static implicit operator Vector4(Vector5 vector)
            {
                return new Vector4(vector.x, vector.y, vector.z, vector.w);
            }
        }

        private class PointData
        {
            internal PointData(Vector4 point, Functions easing = Functions.easeLinear, bool smooth = false)
            {
                Point = point;
                Easing = easing;
                Smooth = smooth;
            }

            internal PointData(Vector2 point, Functions easing = Functions.easeLinear)
            {
                LinearPoint = point;
                Easing = easing;
            }

            internal PointData(Vector5 point, Functions easing = Functions.easeLinear)
            {
                Vector4Point = point;
                Easing = easing;
            }

            internal Vector4 Point { get; }

            internal Vector5 Vector4Point { get; }

            internal Vector2 LinearPoint { get; }

            internal Functions Easing { get; }

            internal bool Smooth { get; }
        }
    }

    internal class PointDefinitionManager
    {
        internal IDictionary<string, PointDefinition> PointData { get; private set; } = new Dictionary<string, PointDefinition>();

        internal void AddPoint(string pointDataName, PointDefinition pointData)
        {
            if (!PointData.TryGetValue(pointDataName, out _))
            {
                PointData.Add(pointDataName, pointData);
            }
            else
            {
                NoodleLogger.Log($"Duplicate point defintion name, {pointDataName} could not be registered!", IPA.Logging.Logger.Level.Error);
            }
        }
    }
}
