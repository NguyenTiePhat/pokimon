using System;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;

public static class LanDuelCheck
{
    static void Require(bool ok,string reason) { if(!ok) throw new Exception(reason); }
    [MenuItem("Pokemon/Check LAN Duel")]
    public static void Run()
    {
        var state=new LanDuelRules.State {step=2};
        var inputs=new[]{new LanDuelRules.Command {x=1,y=1},new LanDuelRules.Command()};
        LanDuelRules.Tick(state,inputs,.02f);
        Require(Mathf.Abs(Vector2.Distance(new Vector2(-5,0),new Vector2(state.players[0].x,state.players[0].y))-.072f)<.001f,"Diagonal normalization");
        inputs[0]=new LanDuelRules.Command {fire=true,aimX=1};
        state.players[0].x=-1; state.players[0].y=0; state.players[1].x=1;
        for(int i=0;i<20;i++) LanDuelRules.Tick(state,inputs,.02f);
        Require(state.players[1].hp==90,"Projectile damage and cooldown");
        inputs[0]=new LanDuelRules.Command {x=1,dash=true}; state.players[0].x=8.5f;
        LanDuelRules.Tick(state,inputs,.02f); Require(state.players[0].x==8.7f,"Dash bounds");
        Require(!LanDuelRules.Valid(new LanDuelRules.Command{x=float.NaN}),"Reject nonfinite input");
        Require(!LanDuelRules.Valid(new LanDuelRules.Command{pokemon=6}),"Reject invalid roster");
        state.players[1].hp=0; LanDuelRules.Tick(state,inputs,.02f);
        Require(state.step==3 && state.winner==0,"Winner state");
        float old=state.players[0].x; LanDuelRules.Tick(state,inputs,.02f); Require(state.players[0].x==old,"Finished match frozen");
        using(var host=new LanTransport()) using(var guest=new LanTransport())
        {
            host.Host(); Require(host.Error==null,"Host bind: "+host.Error);
            guest.Join("127.0.0.1"); string received=null;
            for(int i=0;i<200 && (!host.Connected || !guest.Connected);i++) { host.Pump(s=>received=s); guest.Pump(s=>{}); Thread.Sleep(5); }
            Require(host.Connected && guest.Connected,"Loopback connection");
            guest.Send(JsonUtility.ToJson(new LanDuelRules.Command {pokemon=4,ready=true}));
            for(int i=0;i<100 && received==null;i++) { guest.Pump(s=>{}); host.Pump(s=>received=s); Thread.Sleep(5); }
            Require(received!=null && JsonUtility.FromJson<LanDuelRules.Command>(received).pokemon==4,"Framed protocol delivery");
            guest.Dispose(); for(int i=0;i<100 && host.Connected;i++) {host.Pump(s=>{});Thread.Sleep(5);}
            Require(!host.Connected,"Disconnect detected");
        }
        Debug.Log("LAN_DUEL_CHECK_OK: gameplay rules, protocol, loopback and disconnect.");
    }
}
