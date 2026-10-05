package dev.skycraft;

import dev.skycraft.combat.SkyCombat;
import net.fabricmc.api.ModInitializer;
import net.minecraft.world.entity.EquipmentSlot;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerLifecycleEvents;
import net.fabricmc.fabric.api.networking.v1.ServerPlayConnectionEvents;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.gamerules.GameRules;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public final class SkyCraft implements ModInitializer {
	public static final String MOD_ID = "skycraft";
	public static final String WORLD_NAME = Design.World_Fresh.Name;
	public static final Logger LOG = LoggerFactory.getLogger(MOD_ID);
	private static final String KIT2_TAG = "skycraft_builder_kit";

	@Override
	public void onInitialize() {
		SkyCombat.init();
		dev.skycraft.net.SkyNet.init();
		dev.skycraft.world.SkyDig.init();
		ServerLifecycleEvents.SERVER_STARTED.register(SkyCraft::configureServer);
		ServerPlayConnectionEvents.JOIN.register((handler, sender, server) -> {



		});
	}

	/**
	 * Session settings for the solo bridge. Mob spawning and difficulty are applied once per world
	 * (marker file in the save): afterwards /difficulty and /gamerule changes are the player's.
	 */
	private static void configureServer(MinecraftServer server) {
		GameRules rules = server.getGameRules();
		rules.set(GameRules.ADVANCE_TIME, true, server);
		rules.set(GameRules.ADVANCE_WEATHER, false, server);
		rules.set(GameRules.PLAYER_MOVEMENT_CHECK, false, server);
		rules.set(GameRules.KEEP_INVENTORY, true, server);
		rules.set(GameRules.IMMEDIATE_RESPAWN, true, server);
		rules.set(GameRules.SHOW_ADVANCEMENT_MESSAGES, false, server);
		java.nio.file.Path marker = server.getWorldPath(net.minecraft.world.level.storage.LevelResource.ROOT).resolve("ultracraft-world.properties");
		if (!java.nio.file.Files.exists(marker)) {
			rules.set(GameRules.SPAWN_MOBS, true, server);
			rules.set(GameRules.SPAWN_MONSTERS, true, server);
			rules.set(GameRules.SPAWN_PHANTOMS, false, server);
			rules.set(GameRules.SPAWN_PATROLS, true, server);
			rules.set(GameRules.SPAWN_WANDERING_TRADERS, true, server);
			// A new world starts at noon; afterwards day and night carry on across sessions.
			server.getCommands().performPrefixedCommand(server.createCommandSourceStack().withSuppressedOutput(), "time set noon");
			server.setDifficulty(net.minecraft.world.Difficulty.valueOf(Design.World_Fresh.Difficulty.toUpperCase(java.util.Locale.ROOT)), true);
			try {
				java.nio.file.Files.writeString(marker, "# UltraCraft applied mob spawning and difficulty " + Design.World_Fresh.Difficulty + " once; later changes are kept.\nmobs=1\n");
			} catch (java.io.IOException e) {
				LOG.warn("UltraCraft: couldn't write {}", marker, e);
			}
			LOG.info("UltraCraft: enabled natural mob spawning, difficulty {}", Design.World_Fresh.Difficulty);
		}
		LOG.info("SkyCraft: mirror world configured");
	}

	/**
	 * Local multiplayer test guests (tools/fake_guest.py; named Guest, Guest2, ...) wear a random
	 * mix of iron and diamond armour, so they're easy to tell apart.
	 */
	private static void dressTestGuest(ServerPlayer player) {
		if (!player.getName().getString().startsWith("Guest")) {
			return;
		}
		var random = player.getRandom();
		EquipmentSlot[] slots = { EquipmentSlot.HEAD, EquipmentSlot.CHEST, EquipmentSlot.LEGS, EquipmentSlot.FEET };
		net.minecraft.world.item.Item[][] pieces = {
			{ Items.IRON_HELMET, Items.DIAMOND_HELMET },
			{ Items.IRON_CHESTPLATE, Items.DIAMOND_CHESTPLATE },
			{ Items.IRON_LEGGINGS, Items.DIAMOND_LEGGINGS },
			{ Items.IRON_BOOTS, Items.DIAMOND_BOOTS },
		};
		for (int i = 0; i < slots.length; i++) {
			player.setItemSlot(slots[i], new ItemStack(pieces[i][random.nextBoolean() ? 1 : 0]));
		}
		LOG.info("SkyCraft: dressed test guest {} in iron and diamond", player.getName().getString());
	}

	private static void giveStarterKit(ServerPlayer player) {
		if (!player.getInventory().isEmpty()) {
			return;
		}
		player.getInventory().add(new ItemStack(Items.DIAMOND_SWORD));
		player.getInventory().add(new ItemStack(Items.DIAMOND_PICKAXE));
		player.getInventory().add(new ItemStack(Items.BOW));
		player.getInventory().add(new ItemStack(Items.COOKED_BEEF, 32));
		player.getInventory().add(new ItemStack(Items.OAK_PLANKS, 64));
		player.getInventory().add(new ItemStack(Items.TORCH, 32));
		player.getInventory().add(new ItemStack(Items.ARROW, 64));
		player.setItemSlot(net.minecraft.world.entity.EquipmentSlot.OFFHAND, new ItemStack(Items.SHIELD));
		LOG.info("SkyCraft: gave starter kit to {}", player.getName().getString());
	}

	/**
	 * Once per player: armor (Skyrim's enemies hit back now) and building materials, since there is
	 * no Minecraft terrain to mine in Skyrim.
	 */
	private static void giveBuilderKit(ServerPlayer player) {
		if (player.entityTags().contains(KIT2_TAG)) {
			return;
		}
		equipIfEmpty(player, EquipmentSlot.HEAD, Items.IRON_HELMET);
		equipIfEmpty(player, EquipmentSlot.CHEST, Items.IRON_CHESTPLATE);
		equipIfEmpty(player, EquipmentSlot.LEGS, Items.IRON_LEGGINGS);
		equipIfEmpty(player, EquipmentSlot.FEET, Items.IRON_BOOTS);
		var inventory = player.getInventory();
		inventory.add(new ItemStack(Items.COBBLESTONE, 64));
		inventory.add(new ItemStack(Items.STONE_BRICKS, 64));
		inventory.add(new ItemStack(Items.OAK_LOG, 64));
		inventory.add(new ItemStack(Items.GLASS, 64));
		inventory.add(new ItemStack(Items.OAK_STAIRS, 64));
		inventory.add(new ItemStack(Items.OAK_SLAB, 64));
		inventory.add(new ItemStack(Items.OAK_DOOR, 8));
		inventory.add(new ItemStack(Items.LADDER, 32));
		inventory.add(new ItemStack(Items.LANTERN, 16));
		inventory.add(new ItemStack(Items.CRAFTING_TABLE));
		inventory.add(new ItemStack(Items.WATER_BUCKET));
		inventory.add(new ItemStack(Items.ARROW, 64));
		inventory.add(new ItemStack(Items.GOLDEN_APPLE, 4));
		player.addTag(KIT2_TAG);
		LOG.info("SkyCraft: gave builder kit to {}", player.getName().getString());
	}

	private static void equipIfEmpty(ServerPlayer player, EquipmentSlot slot, net.minecraft.world.item.Item item) {
		if (player.getItemBySlot(slot).isEmpty()) {
			player.setItemSlot(slot, new ItemStack(item));
		} else {
			player.getInventory().add(new ItemStack(item));
		}
	}
}
