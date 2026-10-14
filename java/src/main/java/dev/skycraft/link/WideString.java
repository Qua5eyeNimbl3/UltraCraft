package dev.skycraft.link;

import java.lang.foreign.Arena;
import java.lang.foreign.MemorySegment;
import java.nio.charset.StandardCharsets;

/** A Windows WCHAR string needs two terminator bytes, including at an allocation boundary. */
final class WideString {
    static MemorySegment allocate(Arena arena, String value) {
        byte[] encoded = value.getBytes(StandardCharsets.UTF_16LE);
        MemorySegment result = arena.allocate(encoded.length + 2L, 2);
        MemorySegment.copy(MemorySegment.ofArray(encoded), 0, result, 0, encoded.length);
        return result;
    }
}
