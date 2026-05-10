
using System;
using System.Collections.Generic;

namespace Курсова_1_курс
{
    public abstract class MstAlgorithm
    {
        public abstract string Name { get; }
        // Змінено тип totalWeight на double для підтримки дробів
        public abstract List<Edge> FindMST(Graph graph, out double totalWeight, out int iterations);
    }

    public class KruskalAlgorithm : MstAlgorithm
    {
        public override string Name => "Метод Крускала";

        public override List<Edge> FindMST(Graph graph, out double totalWeight, out int iterations)
        {
            totalWeight = 0;
            int iterCount = 0;
            var result = new List<Edge>();
            var edges = graph.GetAllEdges();

            edges.Sort();

            int[] parent = new int[graph.VerticesCount];
            for (int i = 0; i < graph.VerticesCount; i++) parent[i] = i;

            int Find(int i)
            {
                iterCount++;
                if (parent[i] == i)
                    return i;
                return parent[i] = Find(parent[i]);
            }

            foreach (var edge in edges)
            {
                iterCount++;
                int rootU = Find(edge.U);
                int rootV = Find(edge.V);

                if (rootU != rootV)
                {
                    result.Add(edge);
                    totalWeight += edge.Weight;
                    parent[rootU] = rootV;
                }
            }

            iterations = iterCount;
            return result;
        }
    }

    public class PrimAlgorithm : MstAlgorithm
    {
        public override string Name => "Метод Прима";

        public override List<Edge> FindMST(Graph graph, out double totalWeight, out int iterations)
        {
            totalWeight = 0;
            iterations = 0;
            var result = new List<Edge>();
            int verticesCount = graph.VerticesCount;

            double[] minWeight = new double[verticesCount]; // Змінено на double
            int[] parent = new int[verticesCount];
            bool[] inMST = new bool[verticesCount];

            // Ініціалізація масивів
            for (int i = 0; i < verticesCount; i++)
            {
                minWeight[i] = double.MaxValue; // Використовуємо максимум для double
                parent[i] = -1;
            }

            // Починаємо з нульової вершини
            minWeight[0] = 0;

            for (int count = 0; count < verticesCount; count++)
            {
                double min = double.MaxValue; // Змінено на double
                int u = -1;

                // Шукаємо вершину з мінімальною вагою ребра, яка ще не в остовному дереві
                for (int v = 0; v < verticesCount; v++)
                {
                    iterations++;
                    if (!inMST[v] && minWeight[v] < min)
                    {
                        min = minWeight[v];
                        u = v;
                    }
                }

                // Якщо граф незв'язний
                if (u == -1) break;

                inMST[u] = true;

                // Додаємо ребро до результату (крім першої ітерації)
                if (parent[u] != -1)
                {
                    result.Add(new Edge { U = parent[u], V = u, Weight = graph[parent[u], u] });
                    totalWeight += graph[parent[u], u];
                }

                // Оновлюємо ваги суміжних вершин
                for (int v = 0; v < verticesCount; v++)
                {
                    iterations++;
                    double weight = graph[u, v]; // Змінено на double
                    if (weight > 0 && !inMST[v] && weight < minWeight[v])
                    {
                        parent[v] = u;
                        minWeight[v] = weight;
                    }
                }
            }
            return result;
        }
    }

    public class BoruvkaAlgorithm : MstAlgorithm
    {
        public override string Name => "Метод Борувки";

        public override List<Edge> FindMST(Graph graph, out double totalWeight, out int iterations)
        {
            totalWeight = 0;
            int iterCount = 0;
            var result = new List<Edge>();
            var edges = graph.GetAllEdges();
            int verticesCount = graph.VerticesCount;

            int[] parent = new int[verticesCount];
            int[] cheapest = new int[verticesCount];

            for (int i = 0; i < verticesCount; i++)
                parent[i] = i;

            int Find(int i)
            {
                iterCount++;
                if (parent[i] == i)
                    return i;
                return parent[i] = Find(parent[i]);
            }

            void Union(int i, int j)
            {
                int rootI = Find(i);
                int rootJ = Find(j);
                if (rootI != rootJ)
                    parent[rootI] = rootJ;
            }

            int numTrees = verticesCount;

            while (numTrees > 1)
            {
                for (int i = 0; i < verticesCount; i++)
                    cheapest[i] = -1;

                for (int i = 0; i < edges.Count; i++)
                {
                    iterCount++;
                    int u = edges[i].U;
                    int v = edges[i].V;
                    double weight = edges[i].Weight; // Змінено на double

                    int setU = Find(u);
                    int setV = Find(v);

                    if (setU != setV)
                    {
                        if (cheapest[setU] == -1 || edges[cheapest[setU]].Weight > weight)
                            cheapest[setU] = i;

                        if (cheapest[setV] == -1 || edges[cheapest[setV]].Weight > weight)
                            cheapest[setV] = i;
                    }
                }

                bool edgeAdded = false;

                for (int i = 0; i < verticesCount; i++)
                {
                    iterCount++;
                    if (cheapest[i] != -1)
                    {
                        int edgeIndex = cheapest[i];
                        int u = edges[edgeIndex].U;
                        int v = edges[edgeIndex].V;
                        double weight = edges[edgeIndex].Weight; // Змінено на double

                        int setU = Find(u);
                        int setV = Find(v);

                        if (setU != setV)
                        {
                            result.Add(edges[edgeIndex]);
                            totalWeight += weight;
                            Union(setU, setV);
                            numTrees--;
                            edgeAdded = true;
                        }
                    }
                }
                if (!edgeAdded) break;
            }

            iterations = iterCount;
            return result;
        }
    }
}