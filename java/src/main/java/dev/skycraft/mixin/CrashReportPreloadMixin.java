package dev.skycraft.mixin;

import net.minecraft.CrashReport;
import net.minecraft.util.MemoryReserve;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/**
 * Minecraft builds a throwaway crash report at start to warm its classes, which queries every
 * piece of hardware through WMI. On PCs with broken WMI or performance counters that query hung
 * for 20 to 45 minutes before the world could open. A real crash still writes the full report;
 * only the warm-up is skipped (its memory reserve is kept).
 */
@Mixin(CrashReport.class)
public abstract class CrashReportPreloadMixin {
	@Inject(method = "preload", at = @At("HEAD"), cancellable = true, require = 0)
	private static void skycraft$skipHardwareProbe(CallbackInfo ci) {
		MemoryReserve.allocate();
		ci.cancel();
	}
}
