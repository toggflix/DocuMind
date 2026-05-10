using System;

namespace DocuMind.Core.Utilities
{
    public static class VectorMath
    {
        public static float CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length == 0 || vectorB.Length == 0) return 0.0f;

            int length = Math.Min(vectorA.Length, vectorB.Length);
            double dotProduct = 0.0d;
            double magnitudeA = 0.0d;
            double magnitudeB = 0.0d;

            for (int i = 0; i < length; i++)
            {
                double a = vectorA[i];
                double b = vectorB[i];
                dotProduct += a * b;
                magnitudeA += a * a;
                magnitudeB += b * b;
            }

            if (magnitudeA <= double.Epsilon || magnitudeB <= double.Epsilon)
            {
                return 0.0f;
            }

            return (float)(dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB)));
        }

        public static byte[] ConvertFloatToByteArray(float[] floats)
        {
            if (floats.Length == 0) return Array.Empty<byte>();

            var bytes = new byte[floats.Length * sizeof(float)];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[] ConvertByteArrayToFloat(byte[] bytes)
        {
            if (bytes.Length < sizeof(float)) return Array.Empty<float>();

            int validByteLength = bytes.Length - (bytes.Length % sizeof(float));
            var floats = new float[validByteLength / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, floats, 0, validByteLength);
            return floats;
        }
    }
}
