using System;
using System.Collections.Generic;
namespace Курсова_1_курс
{
    public class Edge : IComparable<Edge>
    {
        public int U { get; set; }
        public int V { get; set; }
        public double Weight { get; set; }
        public static bool operator >(Edge e1, Edge e2) => e1.Weight > e2.Weight;
        public static bool operator <(Edge e1, Edge e2) => e1.Weight < e2.Weight;
        public int CompareTo(Edge other) => Weight.CompareTo(other.Weight);
    }
    public class Graph
    {
        private double[,] adjacencyMatrix;
        public int VerticesCount { get; private set; }
        public Graph(int vertices)
        {
            if (vertices <= 0 || vertices > Constants.MaxVertices)
                throw new GraphException($"Кількість вершин має бути від 1 до {Constants.MaxVertices}");
            VerticesCount = vertices;
            adjacencyMatrix = new double[vertices, vertices];
        }
        public double this[int row, int col]
        {
            get => adjacencyMatrix[row, col];
            set => adjacencyMatrix[row, col] = value;
        }
        public List<Edge> GetAllEdges()
        {
            var edges = new List<Edge>();
            for (int i = 0; i < VerticesCount; i++)
            {
                for (int j = i + 1; j < VerticesCount; j++)
                {
                    if (adjacencyMatrix[i, j] > 0)
                        edges.Add(new Edge { U = i, V = j, Weight = adjacencyMatrix[i, j] });
                }
            }
            return edges;
        }
    }
}