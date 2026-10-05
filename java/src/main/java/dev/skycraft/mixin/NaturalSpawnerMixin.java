package dev.skycraft.mixin;

import dev.skycraft.combat.NativeSpawns;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.Mob;
import net.minecraft.world.level.NaturalSpawner;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Mapped natural hostile spawns become ULTRAKILL enemies (NativeSpawns), after vanilla's own checks pass. */
@Mixin(NaturalSpawner.class)
public abstract class NaturalSpawnerMixin {
	@Inject(method = "isValidPositionForMob", at = @At("RETURN"), cancellable = true)
	private static void ultracraft$nativeEnemies(ServerLevel level, Mob mob, double distance, CallbackInfoReturnable<Boolean> cir) {
		if (cir.getReturnValueZ() && NativeSpawns.replace(level, mob)) {
			cir.setReturnValue(false);
		}
	}
}
