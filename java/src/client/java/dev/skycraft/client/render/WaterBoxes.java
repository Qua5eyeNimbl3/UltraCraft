package dev.skycraft.client.render;

import java.util.ArrayList;
import java.util.List;

/** Coalesces a section's fluid cells, keeping partial surface heights and dry gaps. */
public final class WaterBoxes {
    private WaterBoxes(){}
    public static List<float[]> merge(float[] input){
        if(input.length!=4096)throw new IllegalArgumentException("A section has 4096 cells");
        float[] cells=input.clone();
        List<float[]> boxes=new ArrayList<>();
        for(int y=0;y<16;y++)for(int z=0;z<16;z++)for(int x=0;x<16;x++){
            float height=cells[x+16*z+256*y];if(height<=0)continue;
            int xe=x+1;while(xe<16&&cells[xe+16*z+256*y]==height)xe++;
            int ze=z+1;while(ze<16&&row(cells,x,xe,ze,y,height))ze++;
            int ye=y+1;
            if(height==1)while(ye<16&&plane(cells,x,xe,z,ze,ye,height))ye++;
            for(int yy=y;yy<ye;yy++)for(int zz=z;zz<ze;zz++)for(int xx=x;xx<xe;xx++)cells[xx+16*zz+256*yy]=0;
            boxes.add(new float[]{x,y,z,xe,ye-1+height,ze});
        }
        return boxes;
    }
    private static boolean row(float[] cells,int x,int xe,int z,int y,float h){for(int xx=x;xx<xe;xx++)if(cells[xx+16*z+256*y]!=h)return false;return true;}
    private static boolean plane(float[] cells,int x,int xe,int z,int ze,int y,float h){for(int zz=z;zz<ze;zz++)if(!row(cells,x,xe,zz,y,h))return false;return true;}
}
