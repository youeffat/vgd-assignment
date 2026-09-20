using System.Collections.Generic;
using UnityEngine;

namespace PDollarRecognizer
{
    public class Point
    {
        public Vector2 X;
        public string Name;
        public Point(Vector2 vector, string name) { X = vector; Name = name; }
    }

    public class Result
    {
        public string Match;
        public float Score;
        public Result(string match, float score) { Match = match; Score = score; }
    }

    public class Gesture
    {
        public List<Point> Points;
        public string Name;

        public Gesture(List<Vector2> points, string name)
        {
            Name = name;
            Points = Resample(points, 32);
            Points = ScaleTo(Points, 250.0f);
            Points = TranslateTo(Points, Vector2.zero);
        }

        private static List<Point> Resample(List<Vector2> points, int n)
        {
            float I = PathLength(points) / (n - 1);
            float D = 0.0f;
            List<Point> newPoints = new List<Point> { new Point(points[0], "") };
            for (int i = 1; i < points.Count; i++)
            {
                float d = Vector2.Distance(points[i - 1], points[i]);
                if ((D + d) >= I)
                {
                    float qx = points[i - 1].x + ((I - D) / d) * (points[i].x - points[i - 1].x);
                    float qy = points[i - 1].y + ((I - D) / d) * (points[i].y - points[i - 1].y);
                    newPoints.Add(new Point(new Vector2(qx, qy), ""));
                    points.Insert(i, new Vector2(qx, qy));
                    D = 0.0f;
                }
                else { D += d; }
            }
            if (newPoints.Count == n - 1)
                newPoints.Add(new Point(points[points.Count - 1], ""));
            return newPoints;
        }

        private static float PathLength(List<Vector2> points)
        {
            float d = 0;
            for (int i = 1; i < points.Count; i++)
                d += Vector2.Distance(points[i - 1], points[i]);
            return d;
        }

        private static List<Point> ScaleTo(List<Point> points, float size)
        {
            Rect B = BoundingBox(points);
            float boxWidth = Mathf.Max(B.width, 1e-6f);
            float boxHeight = Mathf.Max(B.height, 1e-6f);

            List<Point> newPoints = new List<Point>();
            foreach (var p in points)
            {
                float qx = p.X.x * (size / boxWidth);
                float qy = p.X.y * (size / boxHeight);
                newPoints.Add(new Point(new Vector2(qx, qy), p.Name));
            }
            return newPoints;
        }

        private static List<Point> TranslateTo(List<Point> points, Vector2 pt)
        {
            Vector2 c = Centroid(points);
            List<Point> newPoints = new List<Point>();
            foreach (var p in points)
            {
                float qx = p.X.x + pt.x - c.x;
                float qy = p.X.y + pt.y - c.y;
                newPoints.Add(new Point(new Vector2(qx, qy), p.Name));
            }
            return newPoints;
        }

        private static Vector2 Centroid(List<Point> points)
        {
            float x = 0.0f, y = 0.0f;
            foreach (var p in points) { x += p.X.x; y += p.X.y; }
            return new Vector2(x / points.Count, y / points.Count);
        }

        private static Rect BoundingBox(List<Point> points)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.X.x);
                maxX = Mathf.Max(maxX, p.X.x);
                minY = Mathf.Min(minY, p.X.y);
                maxY = Mathf.Max(maxY, p.X.y);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public class Recognizer
    {
        private List<Gesture> _trainingSet = new List<Gesture>();

        public void SavePattern(string name, List<Vector2> points)
        {
            _trainingSet.Add(new Gesture(points, name));
        }

        public Result Recognize(List<Vector2> points)
        {
            Gesture candidate = new Gesture(points, "");
            float bestDistance = float.MaxValue;
            string bestMatch = "Unknown";

            foreach (var template in _trainingSet)
            {
                float distance = GreedyCloudMatch(candidate.Points, template.Points);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMatch = template.Name;
                }
            }

            // Convert total cloud distance into an average distance per point
            float avgDistance = bestDistance / candidate.Points.Count;
            Debug.Log($"Average Distance per Point for {bestMatch}: {avgDistance:F2}");

            // Normalized score calculation
            float score = 1.0f - (avgDistance / 150.0f);
            return new Result(bestMatch, Mathf.Clamp01(score));
        }

        private float GreedyCloudMatch(List<Point> pts1, List<Point> pts2)
        {
            float e = 0.50f;
            int step = Mathf.FloorToInt(Mathf.Pow(pts1.Count, 1.0f - e));
            float minDist = float.MaxValue;

            for (int i = 0; i < pts1.Count; i += step)
            {
                float dist = CloudDistance(pts1, pts2, i);
                if (dist < minDist) minDist = dist;
            }
            return minDist;
        }

        private float CloudDistance(List<Point> pts1, List<Point> pts2, int startIndex)
        {
            bool[] matched = new bool[pts1.Count];
            float sum = 0;
            int i = startIndex;
            do
            {
                int index = -1;
                float min = float.MaxValue;
                for (int j = 0; j < pts2.Count; j++)
                {
                    if (!matched[j])
                    {
                        float d = Vector2.Distance(pts1[i].X, pts2[j].X);
                        if (d < min)
                        {
                            min = d;
                            index = j;
                        }
                    }
                }
                matched[index] = true;
                sum += min;
                i = (i + 1) % pts1.Count;
            } while (i != startIndex);

            return sum;
        }
    }
}