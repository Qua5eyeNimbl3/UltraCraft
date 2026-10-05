using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using UltraCraft.Generated;
using UltraCraft.Link;
namespace UltraCraft.Input;
internal sealed class InputRouter {
    readonly UltraLink link;
    readonly ConfigEntry<Key> buildKey,inventoryKey,menuKey;
    readonly HashSet<Key> held=new();
    readonly bool[] buttons=new bool[4];
    Keyboard? textKeyboard;
    readonly Queue<char> text=new();
    public bool Building {get;set;}=true;
    public bool ScreenOpen {get;private set;}
    public Vector2 Cursor {get;private set;}
    public InputRouter(UltraLink link,ConfigFile config){this.link=link;buildKey=config.Bind("Controls","BuildMode",Enum.Parse<Key>(Design.Controls_BuildMode.Key),"Switch between Java mining/building and ULTRAKILL guns.");inventoryKey=config.Bind("Controls","Inventory",Enum.Parse<Key>(Design.Controls_Inventory.Key),"Open Minecraft Java inventory.");menuKey=config.Bind("Controls","JavaMenu",Enum.Parse<Key>(Design.Controls_JavaMenu.Key),"Open Minecraft Java options.");}
    public void Update(in Proto.GuestState guest){
      var k=Keyboard.current;var m=Mouse.current;
      bool wasOpen=ScreenOpen;ScreenOpen=(guest.flags&(uint)Proto.GuestFlags.ScreenOpen)!=0;
      if(ScreenOpen&&!wasOpen){Cursor=new Vector2(Screen.width*.5f,Screen.height*.5f);SendCursor();}
      if(k==null||m==null||!Application.isFocused){Release();return;}
      if(textKeyboard!=k){if(textKeyboard!=null)textKeyboard.onTextInput-=OnText;textKeyboard=k;k.onTextInput+=OnText;}
      if(!ScreenOpen&&k[buildKey.Value].wasPressedThisFrame){Building=!Building;Release();Plugin.Log.LogInfo("mode: "+(Building?"Minecraft building":"ULTRAKILL combat"));}
      if(!ScreenOpen&&k[menuKey.Value].wasPressedThisFrame)link.PushInput(Proto.InputType.OpenMenu);
      foreach(var control in k.allKeys){Key key=control.keyCode;if(key==buildKey.Value||key==menuKey.Value)continue;
        bool allowed=ScreenOpen||key==inventoryKey.Value||key==Key.T||key==Key.Slash||(Building&&(key==Key.Q||(key>=Key.Digit1&&key<=Key.Digit0)));
        if(!Scancodes.FromKey.TryGetValue(key,out ushort sc))continue;
        bool down=allowed&&control.isPressed;
        if(down&&held.Add(key))link.PushInput(Proto.InputType.Key,sc,1);
        if(!down&&held.Remove(key))link.PushInput(Proto.InputType.Key,sc,0);
      }
      bool route=Building||ScreenOpen;
      Pump(1,route&&m.leftButton.isPressed);Pump(2,route&&m.middleButton.isPressed);Pump(3,route&&m.rightButton.isPressed);
      if(route&&m.scroll.ReadValue().y!=0)link.PushInput(Proto.InputType.Scroll,0,m.scroll.ReadValue().y>0?120:-120);
      if(ScreenOpen){Vector2 delta=m.delta.ReadValue();Cursor=new Vector2(Mathf.Clamp(Cursor.x+delta.x,0,Screen.width-1),Mathf.Clamp(Cursor.y-delta.y,0,Screen.height-1));SendCursor();while(text.Count>0)link.PushInput(Proto.InputType.Text,0,text.Dequeue());}else text.Clear();
    }
    // Java's frame is rendered at a fraction of the screen (Render_Native.OverlayScale); the cursor is kept in screen pixels.
    void SendCursor(){float k=(float)Design.Render_Native.OverlayScale;link.PushInput(Proto.InputType.Cursor,0,(int)(Cursor.x*k),(int)(Cursor.y*k));}
    void OnText(char c){if(ScreenOpen)text.Enqueue(c);}
    void Pump(int b,bool down){if(down==buttons[b])return;buttons[b]=down;link.PushInput(Proto.InputType.MouseButton,(ushort)b,down?1:0);}
    public void Release(){link.PushInput(Proto.InputType.ReleaseAll);held.Clear();Array.Clear(buttons,0,buttons.Length);}
}
