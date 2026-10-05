package dev.skycraft.combat;

import dev.skycraft.Design;
import dev.skycraft.SkyCraft;
import dev.skycraft.link.Proto;
import dev.skycraft.link.SkyLink;
import java.util.HashMap;
import java.util.Map;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Mob;

/**
 * Natural hostile spawns become ULTRAKILL enemies. Vanilla still decides when and where a monster may
 * appear (light, biome, spacing, mob cap); once it has accepted a position for a mapped type, the
 * Minecraft mob is dropped and the host is asked to spawn the native enemy there. Passive mobs and
 * unmapped monsters are untouched.
 */
public final class NativeSpawns {
	private static final Map<String, Integer> INDEX = new HashMap<>();
	private static long nextAllowedTick;
	private static int replaced;

	static {
		String[] pairs = Design.Natural_Night.Replacements.split(",");
		for (int i = 0; i < pairs.length; i++) {
			INDEX.put(pairs[i].split(":")[0].trim(), i);
		}
	}

	private NativeSpawns() {
	}

	/**
	 * Mapped monsters already in the world (saved before this replacement existed, or from spawners
	 * and structures) are removed too, unless named or otherwise kept on purpose. Every two seconds.
	 */
	public static void sweep(ServerLevel level) {
		if (!SkyLink.active() || !Boolean.getBoolean("ultracraft.hostMovement") || level.getGameTime() % 40 != 0) {
			return;
		}
		java.util.List<Mob> doomed = new java.util.ArrayList<>();
		for (net.minecraft.world.entity.Entity e : level.getAllEntities()) {
			if (e instanceof Mob mob && !mob.isPersistenceRequired() && !mob.hasCustomName()
				&& INDEX.containsKey(BuiltInRegistries.ENTITY_TYPE.getKey(mob.getType()).getPath())) {
				doomed.add(mob);
			}
		}
		doomed.forEach(Mob::discard);
		if (!doomed.isEmpty()) {
			SkyCraft.LOG.info("UltraCraft: removed {} Minecraft hostile mobs that ULTRAKILL enemies replace", doomed.size());
		}
	}

	/** Called after vanilla accepted {@code mob}'s natural spawn position. True vetoes the Minecraft spawn. */
	public static boolean replace(ServerLevel level, Mob mob) {
		if (!SkyLink.active() || !Boolean.getBoolean("ultracraft.hostMovement")) {
			return false;
		}
		Integer index = INDEX.get(BuiltInRegistries.ENTITY_TYPE.getKey(mob.getType()).getPath());
		if (index == null) {
			return false;
		}
		ServerPlayer player = level.getServer().getPlayerList().getPlayers().isEmpty() ? null : level.getServer().getPlayerList().getPlayers().getFirst();
		long tick = level.getGameTime();
		if (player != null && player.level() == level && tick >= nextAllowedTick) {
			double dx = mob.getX() - player.getX(), dz = mob.getZ() - player.getZ(), dy = mob.getY() - player.getY();
			double horizontal = Math.sqrt(dx * dx + dz * dz);
			if (horizontal >= Design.Natural_Night.MinDistanceBlocks && horizontal <= Design.Natural_Night.MaxDistanceBlocks
				&& Math.abs(dy) <= Design.Natural_Night.MaxHeightDifferenceBlocks) {
				nextAllowedTick = tick + Design.Natural_Night.CooldownSeconds * 20L;
				SkyLink.pushEvent(Proto.EV_NATIVE_SPAWN, index, (float) mob.getX(), (float) mob.getY(), (float) mob.getZ(), 0.0F, 0);
				if (replaced++ < 20) {
					SkyCraft.LOG.info("UltraCraft: natural {} becomes a native enemy at {} {} {}", mob.getType().getDescriptionId(), (int) mob.getX(), (int) mob.getY(), (int) mob.getZ());
				}
			}
		}
		return true; // too far, too soon or replaced: no Minecraft monster either way
	}
}
