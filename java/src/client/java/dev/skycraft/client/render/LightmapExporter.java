package dev.skycraft.client.render;

import com.mojang.blaze3d.systems.RenderSystem;
import com.mojang.renderpearl.api.buffers.GpuBuffer;
import com.mojang.renderpearl.api.buffers.GpuBufferSlice;
import dev.skycraft.Design;
import dev.skycraft.SkyCraft;
import dev.skycraft.link.Proto;
import dev.skycraft.link.SkyLink;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import net.minecraft.client.Minecraft;

/** Ships Java's actual 16x16 lightmap, including gamma, skylight and torch colours. */
final class LightmapExporter {
    private static GpuBuffer buffer;
    private static volatile boolean ready;
    private static boolean pending,logged;
    private static long nextCapture;
    static void frame(Minecraft minecraft){
        if(ready){
            try(GpuBufferSlice.MappedView view=buffer.map(true,false)){
                // RenTextureHdr is four uint32 fields; the final padding word is part of the wire format.
                var header=ByteBuffer.allocate(16).order(ByteOrder.LITTLE_ENDIAN)
                    .putInt(Design.Render_Native.LightmapTextureId).putInt(16).putInt(16).putInt(0).flip();
                SkyLink.writeRender(Proto.REN_TEXTURE,header,view.data().duplicate());
                if(!logged){logged=true;SkyCraft.LOG.info("UltraCraft: actual Java lightmap exported (16x16 RGBA)");}
            }
            ready=false;pending=false;
        }
        long now=System.nanoTime();
        if(pending||now<nextCapture)return;
        var light=minecraft.gameRenderer.levelLightmap().texture();
        if(light.isClosed()||light.getWidth(0)!=16||light.getHeight(0)!=16)return;
        if(buffer==null)buffer=RenderSystem.getDevice().createBuffer(()->"UltraCraft lightmap readback",9,16*16*4);
        pending=true;nextCapture=now+200_000_000L;
        RenderSystem.getDevice().createCommandEncoder().copyTextureToBuffer(light,buffer,0L,()->ready=true,0);
    }
}
