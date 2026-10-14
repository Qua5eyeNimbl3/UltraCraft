package dev.skycraft.world;

import dev.skycraft.SkyCraft;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.tags.FluidTags;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;

/**
 * Frees a player who joins inside solid blocks. Before 0.1.8 a Nether or End portal put the player
 * back at their Overworld coordinates in the new dimension, usually deep in netherrack, and the save
 * kept them there. On join they are moved to the nearest open spot with a floor; if there is none
 * nearby they respawn (keep-inventory is on), which ULTRAKILL follows like any respawn.
 */
public final class SafeSpot {
	private static final int SEARCH_RADIUS = 24;

	private SafeSpot() {
	}

	public static void rescueIfBuried(ServerPlayer player) {
		ServerLevel level = player.level();
		if (level.noCollision(player, player.getBoundingBox())) {
			return;
		}
		BlockPos origin = player.blockPosition();
		for (int r = 1; r <= SEARCH_RADIUS; r++) {
			// Shells of growing size, so the closest open spot wins.
			for (int dy = -r; dy <= r; dy++) {
				for (int dx = -r; dx <= r; dx++) {
					for (int dz = -r; dz <= r; dz++) {
						if (Math.max(Math.abs(dx), Math.max(Math.abs(dy), Math.abs(dz))) != r) {
							continue;
						}
						BlockPos feet = origin.offset(dx, dy, dz);
						if (standable(level, player, feet)) {
							player.teleportTo(feet.getX() + 0.5, feet.getY(), feet.getZ() + 0.5);
							SkyCraft.LOG.info("UltraCraft: player was inside blocks at {}; moved to {}", origin, feet);
							return;
						}
					}
				}
			}
		}
		SkyCraft.LOG.warn("UltraCraft: player is inside blocks at {} with no open spot within {} blocks; respawning", origin, SEARCH_RADIUS);
		player.kill(level);
	}

	private static boolean standable(ServerLevel level, ServerPlayer player, BlockPos feet) {
		if (feet.getY() <= level.getMinY() + 1 || feet.getY() >= level.getMaxY() - 2) {
			return false;
		}
		if (level.dimension() == Level.NETHER && feet.getY() >= 124) {
			return false; // the bedrock roof: open, but nowhere a player belongs
		}
		BlockPos below = feet.below();
		if (!level.getBlockState(below).isFaceSturdy(level, below, Direction.UP)) {
			return false;
		}
		if (level.getFluidState(feet).is(FluidTags.LAVA) || level.getFluidState(feet.above()).is(FluidTags.LAVA) || level.getFluidState(below).is(FluidTags.LAVA)) {
			return false;
		}
		Vec3 at = new Vec3(feet.getX() + 0.5, feet.getY(), feet.getZ() + 0.5);
		return level.noCollision(player, player.getBoundingBox().move(at.subtract(player.position())));
	}
}
