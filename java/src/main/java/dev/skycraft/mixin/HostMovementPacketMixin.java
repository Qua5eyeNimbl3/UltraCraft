package dev.skycraft.mixin;

import dev.skycraft.link.SkyLink;
import net.minecraft.network.protocol.game.ServerboundMovePlayerPacket;
import net.minecraft.server.network.ServerGamePacketListenerImpl;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** The solo native bridge supplies position directly; vanilla packets must not re-check it. */
@Mixin(ServerGamePacketListenerImpl.class)
public abstract class HostMovementPacketMixin {
    @Inject(method="handleMovePlayer",at=@At("HEAD"),cancellable=true)
    private void ultracraft$nativePosition(ServerboundMovePlayerPacket packet,CallbackInfo ci){
        if(Boolean.getBoolean("ultracraft.hostMovement") && SkyLink.active()) ci.cancel();
    }
}
