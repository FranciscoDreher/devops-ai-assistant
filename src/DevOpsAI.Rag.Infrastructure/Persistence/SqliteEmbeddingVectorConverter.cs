using System.Buffers.Binary;

namespace DevOpsAI.Rag.Infrastructure.Persistence;

internal static class SqliteEmbeddingVectorConverter
{
    public static byte[] Serialize(ReadOnlyMemory<float> vector)
    {
        var bytes = new byte[checked(vector.Length * sizeof(float))];
        var values = vector.Span;

        for (var index = 0; index < values.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                bytes.AsSpan(index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(values[index]));
        }

        return bytes;
    }

    public static float[] Deserialize(byte[] vectorBlob, int dimensions)
    {
        ArgumentNullException.ThrowIfNull(vectorBlob);

        if (dimensions <= 0 || vectorBlob.Length != checked(dimensions * sizeof(float)))
        {
            throw new InvalidDataException("A stored embedding vector has an invalid dimension or byte length.");
        }

        var vector = new float[dimensions];
        for (var index = 0; index < vector.Length; index++)
        {
            var bits = BinaryPrimitives.ReadInt32LittleEndian(
                vectorBlob.AsSpan(index * sizeof(float), sizeof(float)));
            vector[index] = BitConverter.Int32BitsToSingle(bits);

            if (!float.IsFinite(vector[index]))
            {
                throw new InvalidDataException("A stored embedding vector contains a non-finite value.");
            }
        }

        return vector;
    }
}
