package dev.skycraft.world;

import dev.skycraft.Design;
import it.unimi.dsi.fastutil.longs.Long2FloatOpenHashMap;
import it.unimi.dsi.fastutil.longs.Long2LongOpenHashMap;
import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.tags.BlockTags;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockState;

/**
 * ULTRAKILL weapons mining Java blocks (server thread). Bullets, pellets, nails and the thrown
 * chainsaw add damage in Minecraft hardness units to the block they hit; the crack shows while it
 * builds up and the block breaks with its normal drops (as if mined with the right diamond tool)
 * once the damage reaches its hardness. The railcannon and the player's explosions break a sphere.
 */
public final class WeaponMining {
	private static final Long2FloatOpenHashMap DAMAGE = new Long2FloatOpenHashMap();
	private static final Long2LongOpenHashMap LAST_HIT = new Long2LongOpenHashMap();
	private static int brokenLogs;

	private WeaponMining() {
	}

	/** One hit on {@code pos}. {@code saw}: the chainsaw (fast on wood, slow on everything else). */
	public static void damage(ServerPlayer player, BlockPos pos, float power, boolean saw) {
		ServerLevel level = player.level();
		BlockState state = level.getBlockState(pos);
		if (state.isAir() || !state.getFluidState().isEmpty() && state.getBlock() instanceof net.minecraft.world.level.block.LiquidBlock) {
			return;
		}
		float hardness = state.getDestroySpeed(level, pos);
		if (hardness < 0.0F) {
			return; // bedrock, barriers, portals
		}
		if (saw) {
			power *= (float) (state.is(BlockTags.MINEABLE_WITH_AXE) ? Design.Mining_Guns.SawWoodMultiplier : Design.Mining_Guns.SawOtherMultiplier);
		}
		long key = pos.asLong();
		float done = DAMAGE.get(key) + power;
		float need = Math.max(hardness, 0.05F);
		if (done >= need) {
			DAMAGE.remove(key);
			LAST_HIT.remove(key);
			level.destroyBlockProgress(crackId(pos), pos, -1);
			breakBlock(level, player, pos, state, true);
			return;
		}
		DAMAGE.put(key, done);
		LAST_HIT.put(key, level.getGameTime());
		level.destroyBlockProgress(crackId(pos), pos, Math.min(9, (int) (done / need * 10.0F)));
	}

	/** Railcannon / explosion: every breakable block within {@code radius} blocks of {@code center}. */
	public static void blast(ServerPlayer player, double cx, double cy, double cz, float radius) {
		ServerLevel level = player.level();
		int r = (int) Math.ceil(radius);
		BlockPos.MutableBlockPos pos = new BlockPos.MutableBlockPos();
		int broken = 0;
		for (int dx = -r; dx <= r; dx++) {
			for (int dy = -r; dy <= r; dy++) {
				for (int dz = -r; dz <= r; dz++) {
					pos.set(Math.floor(cx) + dx, Math.floor(cy) + dy, Math.floor(cz) + dz);
					double ox = pos.getX() + 0.5 - cx, oy = pos.getY() + 0.5 - cy, oz = pos.getZ() + 0.5 - cz;
					if (ox * ox + oy * oy + oz * oz > radius * radius) {
						continue;
					}
					BlockState state = level.getBlockState(pos);
					if (state.isAir() || state.getBlock() instanceof net.minecraft.world.level.block.LiquidBlock) {
						continue;
					}
					float hardness = state.getDestroySpeed(level, pos);
					if (hardness < 0.0F || hardness > Design.Mining_Guns.MaxBreakableHardness) {
						continue;
					}
					boolean drop = level.getRandom().nextFloat() < Design.Mining_Guns.BlastDropChance;
					breakBlock(level, player, pos.immutable(), state, drop);
					broken++;
				}
			}
		}
		if (brokenLogs++ < 20) {
			dev.skycraft.SkyCraft.LOG.info("UltraCraft: weapon blast of radius {} broke {} blocks", radius, broken);
		}
	}

	private static void breakBlock(ServerLevel level, ServerPlayer player, BlockPos pos, BlockState state, boolean drop) {
		if (drop) {
			Block.dropResources(state, level, pos, level.getBlockEntity(pos), player, toolFor(state));
		}
		level.destroyBlock(pos, false, player, 512); // break particles and sound, no second set of drops
	}

	/** Drops as if mined with the right tool, so stone gives cobblestone and ores their items. */
	private static ItemStack toolFor(BlockState state) {
		if (state.is(BlockTags.MINEABLE_WITH_PICKAXE)) {
			return new ItemStack(Items.DIAMOND_PICKAXE);
		}
		if (state.is(BlockTags.MINEABLE_WITH_AXE)) {
			return new ItemStack(Items.DIAMOND_AXE);
		}
		if (state.is(BlockTags.MINEABLE_WITH_SHOVEL)) {
			return new ItemStack(Items.DIAMOND_SHOVEL);
		}
		if (state.is(BlockTags.MINEABLE_WITH_HOE)) {
			return new ItemStack(Items.DIAMOND_HOE);
		}
		return ItemStack.EMPTY;
	}

	/** Partly damaged blocks heal after a while, like vanilla mining that stops. */
	public static void tick(ServerLevel level) {
		if (LAST_HIT.isEmpty() || level.getGameTime() % 20 != 0) {
			return;
		}
		long limit = level.getGameTime() - Design.Mining_Guns.DamageDecaySeconds * 20L;
		var it = LAST_HIT.long2LongEntrySet().fastIterator();
		while (it.hasNext()) {
			var e = it.next();
			if (e.getLongValue() < limit) {
				BlockPos pos = BlockPos.of(e.getLongKey());
				level.destroyBlockProgress(crackId(pos), pos, -1);
				DAMAGE.remove(e.getLongKey());
				it.remove();
			}
		}
	}

	private static int crackId(BlockPos pos) {
		long k = pos.asLong();
		return (int) (k ^ (k >>> 32)) | 0x40000000; // never a real entity id
	}
}
