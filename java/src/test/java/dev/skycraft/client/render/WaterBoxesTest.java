package dev.skycraft.client.render;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

public class WaterBoxesTest {
    @Test void combinesFullPoolWithoutMutatingInput(){
        float[] h=new float[4096];
        for(int y=0;y<4;y++)for(int z=2;z<6;z++)for(int x=1;x<5;x++)h[x+16*z+256*y]=1;
        var boxes=WaterBoxes.merge(h);
        assertEquals(1,boxes.size());assertArrayEquals(new float[]{1,0,2,5,4,6},boxes.get(0));assertEquals(1,h[1+32]);
    }
    @Test void keepsPartialSurfaceHeight(){
        float[] h=new float[4096];h[0]=.75f;h[1]=.75f;
        assertArrayEquals(new float[]{0,0,0,2,.75f,1},WaterBoxes.merge(h).get(0));
    }
    @Test void doesNotFillDryGapOrJoinUnequalFlows(){
        float[] h=new float[4096];h[0]=1;h[2]=1;h[3]=.5f;
        var boxes=WaterBoxes.merge(h);assertEquals(3,boxes.size());
        for(var b:boxes)assertFalse(b[0]<2&&b[3]>1);
        float volume=0;for(var b:boxes)volume+=(b[3]-b[0])*(b[4]-b[1])*(b[5]-b[2]);assertEquals(2.5f,volume);
    }
    @Test void coversWholeWaterSectionWithOneVolume(){
        float[] h=new float[4096];java.util.Arrays.fill(h,1);
        var boxes=WaterBoxes.merge(h);assertEquals(1,boxes.size());assertArrayEquals(new float[]{0,0,0,16,16,16},boxes.get(0));
    }
}
