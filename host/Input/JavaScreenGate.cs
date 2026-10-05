using UnityEngine;
namespace UltraCraft.Input;
/// <summary>Owns only the temporary suspension caused by a Java screen.</summary>
internal sealed class JavaScreenGate {
    NewMovement? player;
    bool suspended,wasActive,wasEnabled,wasKinematic,wasCameraActive,wasCameraEnabled;
    public bool Suspended=>suspended;
    public void Adopt(NewMovement next){
        Release();player=next;
        // Sandbox spawn scripts can leave movement inactive even after the camera is restored.
        // Restore this once, without overriding real pause menus, terminals or death.
        if(!next.dead&&!GameStateManager.Instance.PlayerInputLocked&&MonoSingleton<OptionsManager>.Instance?.paused!=true){
            next.enabled=true;next.activated=true;next.rb.isKinematic=false;next.UnlockMovementAxes();
        }
    }
    public void Sync(bool screen){
        if(player==null||screen==suspended)return;
        if(screen){
            wasActive=player.activated;wasEnabled=player.enabled;wasKinematic=player.rb.isKinematic;
            wasCameraActive=player.cc.activated;wasCameraEnabled=player.cc.enabled;
            suspended=true;player.activated=false;player.rb.isKinematic=true;
            player.cc.activated=false;player.cc.enabled=false;
        }else Release();
    }
    public void Release(){
        if(!suspended||player==null){suspended=false;return;}
        player.rb.isKinematic=wasKinematic;
        player.activated=wasActive&&!player.dead;
        player.enabled=wasEnabled;
        player.cc.activated=wasCameraActive&&!player.dead;
        player.cc.enabled=wasCameraEnabled;
        suspended=false;
    }
    public static bool RoundTrip(NewMovement player){
        // Atomic state test; no frame/physics step, keyboard event or position change.
        var position=player.rb.position;bool active=player.activated,enabled=player.enabled,kinematic=player.rb.isKinematic,camera=player.cc.activated,cameraEnabled=player.cc.enabled;
        var gate=new JavaScreenGate();gate.player=player;gate.Sync(true);
        bool stopped=!player.activated&&player.rb.isKinematic&&!player.cc.enabled;
        gate.Sync(false);
        return stopped&&player.activated==active&&player.enabled==enabled&&player.rb.isKinematic==kinematic&&player.cc.activated==camera&&player.cc.enabled==cameraEnabled&&player.rb.position==position;
    }
}
