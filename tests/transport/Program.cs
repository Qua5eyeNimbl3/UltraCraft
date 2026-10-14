using System;
using System.Runtime.InteropServices;
using UltraCraft.Link;

unsafe class Program {
    static void Require(bool ok, string name) { if(!ok)throw new Exception(name); Console.WriteLine("PASS "+name); }
    static void Main() {
        var log=new BepInEx.Logging.ManualLogSource();
        Require(Proto.SelfCheck(log)&&Rings.SelfTest(log),"protocol layout and ring wraparound");
        const int size=256;
        byte* ring=(byte*)Marshal.AllocHGlobal(size+(int)Proto.RingDataOff);
        try {
            var span=new Span<byte>(ring,size+(int)Proto.RingDataOff);
            int calls=0;
            Rings.MessageHandler count=(t,p,n)=>calls++;
            foreach(uint malformed in new[]{129u,248u,uint.MaxValue}) {
                span.Clear();*(ulong*)ring=16;
                *(Proto.MsgHeader*)(ring+Proto.RingDataOff)=new Proto.MsgHeader{type=1,payloadBytes=malformed};
                Rings.ByteDrain(ring,size,long.MaxValue,count);
                Require(calls==0&&*(ulong*)(ring+Proto.RingTailOff)==16,"reject oversized/truncated packet "+malformed);
            }
            span.Clear();*(ulong*)ring=272;
            Rings.ByteDrain(ring,size,long.MaxValue,count);
            Require(calls==0&&*(ulong*)(ring+Proto.RingTailOff)==272,"reject lapped byte ring");
            span.Clear();*(ulong*)ring=256;*(ulong*)(ring+Proto.RingTailOff)=248;
            *(Proto.MsgHeader*)(ring+Proto.RingDataOff+248)=new Proto.MsgHeader{type=1,payloadBytes=8};
            Rings.ByteDrain(ring,size,long.MaxValue,count);
            Require(calls==0,"reject packet crossing mapping boundary");
            span.Clear();Rings.ByteWrite(ring,size,1,null,0);Rings.ByteWrite(ring,size,2,null,0);
            try { Rings.ByteDrain(ring,size,long.MaxValue,(t,p,n)=>throw new Exception("rejected")); }catch(Exception){}
            Rings.ByteDrain(ring,size,long.MaxValue,count);
            Require(calls==1&&*(ulong*)(ring+Proto.RingTailOff)==16,"exception advances tail and next packet recovers");
            span.Clear();for(int i=0;i<12;i++)Rings.ByteWrite(ring,size,1,null,0);
            calls=0;Rings.ByteDrain(ring,size,long.MaxValue,(t,p,n)=>{calls++;System.Threading.Thread.Sleep(10);},1);
            Require(calls==1,"frame budget leaves pending work for next frame");
            Require(!PayloadChecks.Texture(uint.MaxValue,uint.MaxValue,64,16)&&!PayloadChecks.Texture(16,16,64,16)&&PayloadChecks.Texture(16,16,1040,16),"texture size overflow and truncation");
            Proto.RenBatch* batches=stackalloc Proto.RenBatch[1];
            batches[0]=new Proto.RenBatch{first=uint.MaxValue,count=3};
            Require(!PayloadChecks.Model(1,3,batches),"reject overflowing mesh batch");
            batches[0]=new Proto.RenBatch{first=0,count=3};
            Require(PayloadChecks.Model(1,3,batches),"accept valid triangle batch");
        }finally{Marshal.FreeHGlobal((IntPtr)ring);}
    }
}
namespace BepInEx.Logging {
    class ManualLogSource {
        public void LogInfo(object text) { }
        public void LogError(object text) { Console.WriteLine(text); }
    }
}
