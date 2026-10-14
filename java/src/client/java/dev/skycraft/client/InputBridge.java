package dev.skycraft.client;

import dev.skycraft.combat.SkyCombat;
import dev.skycraft.link.Proto;
import dev.skycraft.link.SkyLink;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.PauseScreen;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.client.input.KeyEvent;
import net.minecraft.client.input.MouseButtonInfo;
import org.lwjgl.sdl.SDLKeyboard;

/**
 * Replays Skyrim-captured input into Minecraft's own input handlers, as if the (hidden) MC
 * window had focus. Keeps a virtual keyboard so InputConstants.isKeyDown() still works.
 */
public final class InputBridge {
	private static final boolean[] KEYS = new boolean[512];
	private static final boolean[] BUTTONS = new boolean[8];
	private static double cursorX, cursorY;
	private static int modifiers;
	private static int clickLogs;
	private static int hitLogs;

	private InputBridge() {
	}

	public static boolean isKeyDown(int scancode) {
		return scancode >= 0 && scancode < KEYS.length && KEYS[scancode];
	}

	public static void drain(Minecraft minecraft) {
		SkyLink.drainInput((type, code, a, b, c) -> dispatch(minecraft, type, code, a, b, c));
	}

	private static void dispatch(Minecraft minecraft, int type, int code, int a, int b, int c) {
		long handle = minecraft.getWindow().handle();
		switch (type) {
			case Proto.IN_KEY -> key(minecraft, handle, code, a != 0);
			case Proto.IN_MOUSE_BUTTON -> {
				if (code > 0 && code < BUTTONS.length) {
					BUTTONS[code] = a != 0;
				}
				if (a != 0 && clickLogs++ < 20) {
					var hit = minecraft.hitResult;
					dev.skycraft.SkyCraft.LOG.info("SkyCraft: click {} -> {} {} (grabbed {}, screen {})", code, hit == null ? "null" : hit.getType(),
						hit instanceof net.minecraft.world.phys.EntityHitResult eh ? eh.getEntity().getName().getString() : hit == null ? "" : hit.getLocation(),
						minecraft.mouseHandler.isMouseGrabbed(), minecraft.gui.screen());
				}
				minecraft.mouseHandler.onButton(handle, new MouseButtonInfo(code, modifiers), a != 0 ? 1 : 0);
			}
			case Proto.IN_SCROLL -> minecraft.mouseHandler.onScroll(handle, 0.0, a / 120.0);
			case Proto.IN_CURSOR -> {
				double dx = a - cursorX;
				double dy = b - cursorY;
				cursorX = a;
				cursorY = b;
				minecraft.mouseHandler.onMove(handle, a, b, dx, dy);
			}
			case Proto.IN_TEXT -> {
				if (minecraft.gui.screen() != null) {
					minecraft.keyboardHandler.textInput(handle, new String(Character.toChars(a)));
				}
			}
			case Proto.IN_RELEASE_ALL -> releaseAll();
			case Proto.IN_HURT -> hurt(minecraft, code, a / 100.0F, b, c);
			case Proto.IN_HIT_ENTITY -> hitEntity(minecraft, a, b / 100.0F, c / 100.0F);
			case Proto.IN_PULL_ENTITY -> pullEntity(minecraft, a, b / 1000.0, (code - 32768) / 1000.0, c / 1000.0);
			case Proto.IN_BREAK_BLOCK -> onServer(minecraft, player -> dev.skycraft.world.WeaponMining.damage(player, new net.minecraft.core.BlockPos(a, b, c), (code & 0xFFF) / 100.0F, (code & 0x1000) != 0));
			case Proto.IN_BLAST -> onServer(minecraft, player -> dev.skycraft.world.WeaponMining.blast(player, a / 100.0, b / 100.0, c / 100.0, code / 100.0F));
			case Proto.IN_OPEN_MENU -> {
				if (minecraft.gui.screen() == null && minecraft.player != null) {
					releaseAll();
					minecraft.gui.setScreen(new PauseScreen(true));
				}
			}
			default -> {
			}
		}
	}

	/**
	 * A native ULTRAKILL weapon hit a Minecraft mob's hitbox: the player's attack, applied on the
	 * integrated server with vanilla knockback away from the shot. Invulnerability frames are
	 * cleared first so fast weapons (nailgun, shotgun pellets) all land.
	 */
	private static void hitEntity(Minecraft minecraft, int id, float damage, float yaw) {
		var server = minecraft.getSingleplayerServer();
		if (server == null || minecraft.player == null || damage <= 0.0F) {
			return;
		}
		var uuid = minecraft.player.getUUID();
		server.execute(() -> {
			ServerPlayer player = server.getPlayerList().getPlayer(uuid);
			if (player == null || !(player.level().getEntity(id) instanceof net.minecraft.world.entity.LivingEntity target) || !target.isAlive()) {
				return;
			}
			target.setInvulnerableTime(0);
			var source = player.level().damageSources().playerAttack(player);
			float before = target.getHealth();
			boolean hurt = target.hurtServer(player.level(), source, damage);
			if (hitLogs++ < 30) {
				dev.skycraft.SkyCraft.LOG.info("UltraCraft: native weapon hit {} for {}: health {} -> {}", target.getType().getDescriptionId(), damage, before, target.getHealth());
			}
			if (hurt) {
				float r = yaw * net.minecraft.util.Mth.DEG_TO_RAD;
				target.knockback(0.4, net.minecraft.util.Mth.sin(r), -net.minecraft.util.Mth.cos(r), source, damage);
			}
		});
	}

	/**
	 * The Whiplash is yanking a small Minecraft mob towards the player: ULTRAKILL sends the velocity
	 * (blocks per tick) every physics step while it pulls, the last one zero when it arrives.
	 */
	private static void pullEntity(Minecraft minecraft, int id, double vx, double vy, double vz) {
		var server = minecraft.getSingleplayerServer();
		if (server == null || minecraft.player == null) {
			return;
		}
		var uuid = minecraft.player.getUUID();
		server.execute(() -> {
			ServerPlayer player = server.getPlayerList().getPlayer(uuid);
			if (player == null || !(player.level().getEntity(id) instanceof net.minecraft.world.entity.LivingEntity target) || !target.isAlive()) {
				return;
			}
			target.setDeltaMovement(vx, vy, vz);
			target.syncVelocity = true; // send the new velocity to the client this tick
			target.resetFallDistance();
		});
	}

	/** Runs {@code action} with the integrated server's player, on the server thread. */
	private static void onServer(Minecraft minecraft, java.util.function.Consumer<ServerPlayer> action) {
		var server = minecraft.getSingleplayerServer();
		if (server == null || minecraft.player == null) {
			return;
		}
		var uuid = minecraft.player.getUUID();
		server.execute(() -> {
			ServerPlayer player = server.getPlayerList().getPlayer(uuid);
			if (player != null && player.isAlive()) {
				action.accept(player);
			}
		});
	}

	/** Skyrim hit the player: apply it as Minecraft damage on the integrated server (or the host's). */
	private static void hurt(Minecraft minecraft, int kind, float skyrimDamage, int attacker, int flags) {
		var server = minecraft.getSingleplayerServer();
		if (minecraft.player == null) {
			return;
		}
		if (server == null) {
			// A guest in a friend's world: the host's server applies it.
			if (net.fabricmc.fabric.api.client.networking.v1.ClientPlayNetworking.canSend(dev.skycraft.net.SkyNet.Hurt.TYPE)) {
				net.fabricmc.fabric.api.client.networking.v1.ClientPlayNetworking.send(new dev.skycraft.net.SkyNet.Hurt(kind, skyrimDamage, attacker, flags));
			}
			return;
		}
		var uuid = minecraft.player.getUUID();
		server.execute(() -> {
			ServerPlayer player = server.getPlayerList().getPlayer(uuid);
			if (player != null) {
				SkyCombat.hurtPlayer(player, kind, skyrimDamage, attacker, flags);
			}
		});
	}

	private static void key(Minecraft minecraft, long handle, int scancode, boolean down) {
		if (scancode <= 0 || scancode >= KEYS.length) {
			return;
		}
		boolean wasDown = KEYS[scancode];
		KEYS[scancode] = down;
		updateModifiers();
		int action = down ? (wasDown ? -1 : 1) : 0; // -1 = repeat
		int keycode = SDLKeyboard.SDL_GetKeyFromScancode(scancode, (short) modifiers, true);
		minecraft.keyboardHandler.keyPress(handle, action, new KeyEvent(scancode, keycode, modifiers));
	}

	private static void updateModifiers() {
		int m = 0;
		if (KEYS[225]) m |= 0x0001; // SDL_KMOD_LSHIFT
		if (KEYS[229]) m |= 0x0002; // SDL_KMOD_RSHIFT
		if (KEYS[224]) m |= 0x0040; // SDL_KMOD_LCTRL
		if (KEYS[228]) m |= 0x0080; // SDL_KMOD_RCTRL
		if (KEYS[226]) m |= 0x0100; // SDL_KMOD_LALT
		if (KEYS[230]) m |= 0x0200; // SDL_KMOD_RALT
		modifiers = m;
	}

	/** Lift every key and button we think is held (focus moved to Skyrim, link dropped, ...). */
	public static void releaseAll() {
		Minecraft minecraft = Minecraft.getInstance();
		long handle = minecraft.getWindow().handle();
		for (int sc = 0; sc < KEYS.length; sc++) {
			if (KEYS[sc]) {
				KEYS[sc] = false;
				updateModifiers();
				minecraft.keyboardHandler.keyPress(handle, 0, new KeyEvent(sc, SDLKeyboard.SDL_GetKeyFromScancode(sc, (short) 0, true), modifiers));
			}
		}
		for (int button = 1; button < BUTTONS.length; button++) {
			if (BUTTONS[button]) {
				BUTTONS[button] = false;
				minecraft.mouseHandler.onButton(handle, new MouseButtonInfo(button, 0), 0);
			}
		}
	}
}
