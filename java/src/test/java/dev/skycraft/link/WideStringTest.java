package dev.skycraft.link;

import java.lang.foreign.Arena;
import java.lang.foreign.ValueLayout;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class WideStringTest {
    @Test void windowsNamesAlwaysHaveFullWideTerminator() {
        try (Arena arena = Arena.ofConfined()) {
            for (String name : new String[]{"", "Local\\UltraCraft_minecraft", "\u03bb\ud83c\udf0d"}) {
                var value = WideString.allocate(arena, name);
                assertEquals(name.getBytes(java.nio.charset.StandardCharsets.UTF_16LE).length + 2, value.byteSize());
                assertEquals(0, value.get(ValueLayout.JAVA_BYTE, value.byteSize()-1));
                assertEquals(0, value.get(ValueLayout.JAVA_BYTE, value.byteSize()-2));
                assertEquals(0, value.address() % 2);
            }
        }
    }
}
