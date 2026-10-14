namespace UltraCraft.Link;

/// <summary>Validate counts before native texture/mesh uploads or pointer arithmetic.</summary>
internal static unsafe class PayloadChecks
{
    public static bool Texture(uint width, uint height, uint bytes, uint headerBytes) =>
        width > 0 && height > 0 && width <= 16384 && height <= 16384 &&
        (ulong)headerBytes + (ulong)width * height * 4 <= bytes;

    public static bool Model(uint batches, uint vertices, Proto.RenBatch* data)
    {
        if (batches > 4096 || vertices > 1000000) return false;
        for (uint i = 0; i < batches; i++)
            if (data[i].first > vertices || data[i].count > vertices - data[i].first || data[i].count % 3 != 0)
                return false;
        return true;
    }
}
