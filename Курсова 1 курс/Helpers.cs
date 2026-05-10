using System;
namespace Курсова_1_курс
{
    public static class Constants
    {
        public const string DefaultExportFileName = "MST_Results.txt";
        public const int NodeRadius = 15;
        public const int MaxVertices = 20;
    }
    public class GraphException : Exception
    {
        public GraphException(string message) : base(message) { }
    }
}