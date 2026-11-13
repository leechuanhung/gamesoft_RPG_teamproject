using UnityEngine;

// The things in the inventory and the peddler's pack, coins from ghosts, walking from map to map, the ferry, the pass.
public static partial class JinaPlayChecks
{
    static void Items()
    {
        combat.Restart();
        Check(JinaItems.All.Length==6 && System.Array.TrueForAll(JinaItems.All,i=>i.Icon!=null && i.price>0),"Every item has its icon and price");
        var bag=JinaItems.Inventory; var ui=bag.GetComponent<InventoryUI>(); var wallet=combat.Tavern.wallet;
        Check(bag!=null && ui!=null && bag.Slots.Count==0,"The inventory starts empty");

        // Each effect on its own.
        string said;
        var misut=JinaItems.Get("misutgaru");
        Check(!misut.Use(combat,out said) && combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth,"Food is kept at full health");
        Set(combat,"<PlayerHealth>k__BackingField",10);
        Check(JinaItems.Get("cheongsimhwan").Use(combat,out said) && combat.PlayerHealth==70,"Cheongsimhwan heals sixty");
        Set(combat,"<PlayerHealth>k__BackingField",PrototypeCombat.MaxPlayerHealth);
        combat.AddArrows(-12);
        Check(JinaItems.Get("arrows").Use(combat,out said) && combat.Arrows==ShopRules.MaxArrows-7,"An arrow bundle refills the quiver");
        Check(JinaItems.Get("ward").Use(combat,out said) && combat.Warded && !JinaItems.Get("ward").Use(combat,out said),"One ward charm at a time");
        Set(combat,"invulnerableUntil",0f); combat.HurtPlayer();
        Check(combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth && !combat.Warded && combat.WardBrokeAt>=0,"The ward takes the blow");
        Check(JinaItems.Get("jipsin").Use(combat,out said) && combat.player.Hasted,"Straw sandals quicken him");
        combat.player.HasteUntil=0;

        // Bought from the peddler into the inventory, then used from it.
        int coins=wallet.Coins;
        combat.Peddler.Purchase(JinaItems.Get("jumeokbap"));
        Check(wallet.Coins==coins-7 && bag.GetQuantity(JinaItems.Get("jumeokbap"))==1,"The peddler sells into the inventory");
        Set(ui,"selected",0); ui.UseSelected();
        Check(bag.GetQuantity(JinaItems.Get("jumeokbap"))==1,"Using food at full health keeps it");
        Set(combat,"<PlayerHealth>k__BackingField",30); ui.UseSelected();
        Check(combat.PlayerHealth==70 && bag.Slots.Count==0,"Using it heals and empties the slot");
        Set(wallet,"<Coins>k__BackingField",2); combat.Peddler.Purchase(JinaItems.Get("cheongsimhwan"));
        Check(wallet.Coins==2 && bag.Slots.Count==0,"No sale without the coins");
        combat.FindCoins(PrototypeCamp.CoinsPerGhost);
        Check(wallet.Coins==2+PrototypeCamp.CoinsPerGhost,"Ghosts leave coins");
        combat.Peddler.Purchase(JinaItems.Get("misutgaru"));
        combat.Restart();
        Check(bag.Slots.Count==0 && !combat.Warded && wallet.Coins==30,"Restart empties the bag and the charms");
    }

    static void Maps()
    {
        combat.Restart();
        var maps=combat.Maps; var view=Camera.main.GetComponent<PrototypeView>(); var player=combat.player.transform;
        var cam=Camera.main; float half=cam.orthographicSize*cam.aspect;
        PrototypeView.Shake(0,.01f);   // an earlier check's shake would push the camera off
        Check(PrototypeMaps.All.Length==8 && PrototypeMaps.Index(PrototypeWorld.Spawn.x)==2 && PrototypeMaps.Index(-50)==0 && PrototypeMaps.Index(40)==3 && PrototypeMaps.Index(90)==4 && PrototypeMaps.Index(130)==5 && PrototypeMaps.Index(170)==6 && PrototypeMaps.Index(220)==7,"Eight maps, the village where he starts");
        Check(maps.Current==2 && !maps.Travelling,"He starts in the village");
        Call(view,"LateUpdate");
        Check(cam.transform.position.x+half<=PrototypeWorld.ForestStart+.01f,"The camera stays inside the village");

        // Walking off the right edge: black closes, the forest is shown, black opens, his name card.
        player.position=new Vector3(PrototypeWorld.ForestStart+PrototypeMaps.Margin+.1f,player.position.y,0);
        Call(maps);
        Check(maps.Travelling && maps.Current==2,"Leaving the village begins the change");
        Check(!maps.Shows(PrototypeWorld.ForestStart),"The dokkaebi cannot notice him before the forest is on screen");
        Set(maps,"<LeftAt>k__BackingField",Time.time-PrototypeMaps.FadeOut-.01f); Call(maps);
        Check(maps.Current==3 && maps.Cover>.99f,"Behind the black the forest takes over");
        Call(view,"LateUpdate");
        Check(cam.transform.position.x-half>=PrototypeWorld.ForestStart-.01f,"The camera shows only the forest");
        Set(maps,"<LeftAt>k__BackingField",Time.time-PrototypeMaps.FadeOut-PrototypeMaps.FadeIn-.02f);
        Set(maps,"<EnteredAt>k__BackingField",Time.time-PrototypeMaps.FadeIn-.01f); Call(maps);
        Check(!maps.Travelling && maps.Cover==0 && maps.Shows(PrototypeWorld.ForestStart),"The black lifts on the forest");
        Capture("map-forest-edge",cam.transform.position,5.5f);

        // Walking back the other way.
        player.position=new Vector3(PrototypeWorld.ForestStart-PrototypeMaps.Margin-.1f,player.position.y,0);
        Call(maps); Set(maps,"<LeftAt>k__BackingField",Time.time-PrototypeMaps.FadeOut-.01f); Call(maps);
        Check(maps.Current==2,"And back into the village");
        combat.Restart();
        Check(maps.Current==2 && !maps.Travelling,"Restart returns to the village map");
    }

    static void Ferry()
    {
        combat.Restart();
        var ferry=combat.Ferry; var boat=ferry.Boat; var player=combat.player.transform;
        var elderGone=new QuestProgress(); elderGone.SacrificeElder();
        elderGone.Resolve(false);
        Check(PrototypeFerry.Lines(elderGone)[0].Contains("노인장"),"The ferryman speaks of the elder if he fell");
        var slain=new QuestProgress(); slain.Resolve(false);
        var purified=WithCharm(new QuestProgress()); purified.Resolve(true);
        Check(string.Join("",PrototypeFerry.Lines(slain)).Contains("쓰러뜨렸") && string.Join("",PrototypeFerry.Lines(purified)).Contains("달래"),"He knows how the forest was quieted");
        foreach(var q in new[]{slain,purified,elderGone}) Check(!string.Join("",PrototypeFerry.Lines(q)).Contains("약속") && !string.Join("",PrototypeFerry.Lines(q)).Contains("못 띄우오"),"He never speaks as if the forest were still troubled");

        // Thorns bar the forest's end until the dokkaebi is dealt with, then wither; a restart grows them back.
        var thorns=GameObject.Find("숲 끝 가시덤불");
        Physics2D.SyncTransforms();
        Check(ferry.ThornsShut && !combat.EnemyDefeated && Physics2D.Raycast(new Vector2(PrototypeWorld.FerryStart-2,.2f),Vector2.right,3,1<<8).collider==thorns.GetComponent<BoxCollider2D>(),"Thorns block the way to the ferry");
        Capture("thorns",new Vector3(PrototypeWorld.FerryStart-6,2,-10),5.5f);
        Set(combat,"<EnemyHealth>k__BackingField",0); Call(ferry);
        Check(!ferry.ThornsShut && ferry.ClearedAt>=0,"The dokkaebi slain, the thorns no longer block");
        Set(ferry,"<ClearedAt>k__BackingField",Time.time-PrototypeFerry.WitherLength-.01f); Call(ferry);
        Check(!thorns.GetComponent<SpriteRenderer>().enabled,"And they wither away");
        combat.Restart();
        Check(ferry.ThornsShut && thorns.GetComponent<SpriteRenderer>().enabled && ferry.ClearedAt<0,"Restart grows the thorns back");
        Set(combat,"<EnemyHealth>k__BackingField",0); Call(ferry);
        var ferryman=GameObject.Find("뱃사공").GetComponent<JinaActorVisual>();
        Check(Picture(ferryman).sprite.name==JinaArt.Ferryman && Picture(ferryman).color==Color.white,"The ferryman is drawn, untinted");

        // Talking holds the game, page by page.
        player.position=new Vector3(PrototypeWorld.Ferryman.x+1,player.position.y,0);
        Check(ferry.Near,"Near the ferryman");
        ferry.Open();
        Check(ferry.IsOpen && combat.Paused && !combat.player.CanMove,"Talking to the ferryman holds the game");
        Set(ferryman,"last",ferryman.transform.position); ferryman.Refresh();
        Check(Picture(ferryman).flipX,"He turns to the seonbi");
        for(int i=0,n=Field<string[]>(ferry,"lines").Length;i<n;i++) ferry.Next();
        Check(!ferry.IsOpen && combat.player.CanMove,"The talk ends after the last line");
        Capture("ferry-bank",new Vector3(PrototypeWorld.Ferryman.x+3,1,-10),5.5f);

        // The boat waits at the near bank until he is on this map, then crosses.
        player.position=new Vector3(PrototypeWorld.Spawn.x,player.position.y,0);
        Call(boat,"FixedUpdate");
        Check(!ferry.PlayerHere && boat.Velocity==Vector2.zero,"The boat waits while he is away");
        player.position=new Vector3(PrototypeWorld.Ferryman.x+1,player.position.y,0);
        Call(boat,"FixedUpdate");
        Check(ferry.PlayerHere && boat.Velocity.x>0,"With him here it sets out for the far bank");
        Capture("ferry-river",new Vector3(90,1,-10),5.5f);

        // Falling into the river hurts and puts him back on the nearer bank.
        player.position=new Vector3(PrototypeWorld.RiverLeft+4,-2,0);
        Set(combat,"invulnerableUntil",0f); ferry.Splash();
        Check(combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth-PrototypeCombat.MonsterAttack && player.position.x<PrototypeWorld.RiverLeft && ferry.SplashAt>=0,"The river hurts and returns him to the bank");
        // Water ghosts: tinted blue, rise again each time he comes back, and the tenth felled frees the keepsake.
        combat.Restart();
        var river=combat.River; var bag=JinaItems.Inventory;
        Check(river.mobs.Count==3 && river.mobs[0].visual.tint!=Color.white && combat.Camp.mobs[0].visual.tint==Color.white,"Three water ghosts, drawn blue");
        for(int round=0;round<4;round++)
        {
            player.position=new Vector3(PrototypeWorld.Spawn.x,player.position.y,0); Call(river);
            player.position=new Vector3(PrototypeWorld.Ferryman.x,player.position.y,0); Call(river);
            Check(river.Remaining==3,"The water ghosts rise again on coming back, round "+round);
            foreach(var m in river.mobs) if(combat.RiverFelled<PrototypeCombat.KeepsakeAt) river.Hit(m,PrototypeCamp.GhostHealth);
        }
        Check(combat.RiverFelled==PrototypeCombat.KeepsakeAt && bag.GetQuantity(JinaItems.Keepsake)==1 && combat.KeepsakeFreedAt>=0,"The tenth frees the drowned mother's keepsake");
        string kept; Check(!JinaItems.Keepsake.Use(combat,out kept) && bag.GetQuantity(JinaItems.Keepsake)==1,"The keepsake is kept, not spent");
        river.Hit(river.mobs[2],PrototypeCamp.GhostHealth);
        Check(bag.GetQuantity(JinaItems.Keepsake)==1,"Only one keepsake a run");
        Capture("river-ghosts",new Vector3(90,1,-10),5.5f);
        combat.Restart();
        Check(combat.RiverFelled==0 && bag.GetQuantity(JinaItems.Keepsake)==0,"Restart starts the count again");
        Check(!ferry.IsOpen && ferry.SplashAt<0 && Mathf.Abs(boat.transform.position.x-PrototypeFerry.BoatLeft)<.01f,"Restart brings the boat back to the near bank");
    }

    static void Pass()
    {
        combat.Restart();
        var pass=combat.Pass; var player=combat.player.transform;
        float s0=PrototypeWorld.PassStart;
        Check(PrototypeWorld.Top(s0+5)==-.5f && PrototypeWorld.Top(s0+15)==1f && PrototypeWorld.Top(PrototypeWorld.ShrineX)==2.5f && PrototypeWorld.Top(PrototypeWorld.GoalX)==-.5f && PrototypeWorld.Top(130)==-.5f,"The pass climbs to the shrine and comes down to the road");

        // A boulder rolls down toward him, drops off a step, and hits him unless he backsteps.
        var rock=pass.Spawn(PrototypeWorld.RockStart);
        float x0=rock.art.transform.position.x;
        pass.Step(.05f);
        Check(rock.art.transform.position.x<x0,"Boulders roll downhill");
        rock.art.transform.position=new Vector3(s0+18.2f,PrototypeWorld.Top(s0+18.2f)+PrototypePass.Radius,0);
        pass.Step(.1f);
        Check(rock.art.transform.position.y<2.5f+PrototypePass.Radius && rock.art.transform.position.y>1f+PrototypePass.Radius,"Off a step it falls");
        player.position=new Vector3(s0+8,.3f,0); Physics2D.SyncTransforms();
        rock.art.transform.position=new Vector3(s0+8.3f,PrototypeWorld.Top(s0+8)+PrototypePass.Radius,0);
        Set(combat.player,"dodgeUntil",Time.time+1); Set(combat,"invulnerableUntil",0f);
        pass.Step(0);
        Check(combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth,"Backstepping slips through a boulder");
        Set(combat.player,"dodgeUntil",0f);
        pass.Step(0);
        Check(combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth-PrototypeCombat.MonsterAttack,"A boulder hits him");
        Capture("pass-boulder",new Vector3(s0+14,2,-10),5.5f);

        // The shrine: a stone on the cairn restores him, once.
        player.position=new Vector3(PrototypeWorld.ShrineX,PrototypeWorld.Top(PrototypeWorld.ShrineX)+.8f,0);
        Set(combat,"<PlayerHealth>k__BackingField",2);
        Check(pass.NearShrine && pass.UseShrine() && combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth,"The shrine restores him");
        Set(combat,"<PlayerHealth>k__BackingField",2);
        Check(!pass.UseShrine() && combat.PlayerHealth==2,"Only once a run");
        Capture("pass-shrine",new Vector3(PrototypeWorld.ShrineX-2,2,-10),5.5f);
        Capture("pass-road",new Vector3(s0+40,2,-10),5.5f);
        combat.Restart();
        Check(!pass.ShrineUsed && pass.rocks.Count==0,"Restart clears the boulders and the shrine");
    }

    static void KongVillage()
    {
        combat.Restart();
        var kong=combat.Kong; var player=combat.player.transform; var W=PrototypeKongVillage.Who.Kongjwi;
        foreach(var name in new[]{"콩쥐","팥쥐","아낙","나무꾼"})
        {
            var v=GameObject.Find(name).GetComponent<JinaActorVisual>(); v.Refresh();
            Check(Picture(v).sprite!=null && Picture(v).sprite.name.StartsWith("16_Kong/") && Picture(v).color==Color.white,name+" is drawn");
        }
        Check(PrototypeKongVillage.Lines(W,false,false,false,false,false,false)[2].Contains("잔치")
            && System.Array.Exists(PrototypeKongVillage.Lines(PrototypeKongVillage.Who.Anak,false,false,false,false,false,false),l=>l.Contains("천벌"))
            && PrototypeKongVillage.Lines(PrototypeKongVillage.Who.Patjwi,false,false,false,false,true,false,false,true)[0].Contains("비녀"),"Kongjwi invites, the wife curses Patjwi, Patjwi knows her mother's hairpin");
        // The hairpin: never at their first meeting, and once said, not again the morning after.
        var P=PrototypeKongVillage.Who.Patjwi;
        Check(!string.Join("",PrototypeKongVillage.Lines(P,false,false,false,false,true,false)).Contains("비녀"),"A stranger first: no word of the hairpin at their first meeting");
        Check(!string.Join("",PrototypeKongVillage.Lines(P,false,false,false,false,true,true,false,true,true)).Contains("비녀")
            && string.Join("",PrototypeKongVillage.Lines(P,false,false,false,false,true,true,false,true,false)).Contains("비녀"),"In the morning she only speaks of it if she has not before");

        // The scene: Patjwi shoves Kongjwi while he is near.
        player.position=new Vector3(PrototypeWorld.Kongjwi.x-4,player.position.y,0);
        Set(kong,"nextScene",0f); Call(kong);
        Check(kong.SceneAt>=0 && kong.SceneLine==0,"Patjwi starts on Kongjwi when he comes near");
        Set(kong,"<SceneAt>k__BackingField",Time.time-1f); Call(kong,"LateUpdate");
        Check(GameObject.Find("콩쥐").transform.position.x<PrototypeWorld.Kongjwi.x-.3f,"Kongjwi is shoved back");
        Capture("kong-scene",new Vector3(PrototypeWorld.Kongjwi.x-1,1,-10),3);
        Capture("kong-village",new Vector3(130,2,-10),5.5f);

        // Before the night there is nothing to find: the crocks stay shut and the paper is not there yet.
        var paper=GameObject.Find("잔치 명단")??null; var stay=combat.Stay;
        Check(paper==null,"No paper by the crocks before the night");
        kong.ExamineCrocks();
        Check(!kong.SawCrocks,"He will not open another house's crocks yet");
        // Patjwi and Kongjwi never repeat themselves between the evening and the morning, and Patjwi never says it outright.
        foreach(var who in new[]{W,PrototypeKongVillage.Who.Patjwi})
        {
            var before=PrototypeKongVillage.Lines(who,false,false,false,false,false,false); var after=PrototypeKongVillage.Lines(who,false,false,false,false,false,true);
            Check(System.Array.TrueForAll(before,l=>System.Array.IndexOf(after,l)<0),"Nothing said twice before and after the night");
        }
        Check(!string.Join("",PrototypeKongVillage.Lines(PrototypeKongVillage.Who.Patjwi,false,false,false,false,false,true)).Contains("요괴"),"Patjwi only hints");
        kong.Open(PrototypeKongVillage.Who.Patjwi); for(int i=0,n=Field<string[]>(kong,"lines").Length;i<n;i++) kong.Next();
        Check(!kong.HeardPatjwi,"Before the night she lets nothing slip");

        // Meeting Kongjwi is only meeting her; once he has met everyone and goes back to her, she offers a bed, Patjwi
        // objects, one look ends it.
        kong.Open(W); Check(kong.IsOpen && combat.Paused && kong.Portrait=="kongjwi" && JinaArt.Portrait("kongjwi")!=null,"Talking to Kongjwi holds the game, with her portrait");
        for(int i=0;i<3;i++) kong.Next();
        Check(!kong.Invited && !stay.Playing && !kong.ReadyForNight,"Their first talk is only a greeting");
        kong.Open(W); for(int i=0,n=Field<string[]>(kong,"lines").Length;i<n;i++) kong.Next();
        Check(!kong.Invited && !stay.Playing,"Until he has met the village she lets him look round");
        foreach(var who in new[]{PrototypeKongVillage.Who.Anak,PrototypeKongVillage.Who.Namu}) { kong.Open(who); for(int i=0,n=Field<string[]>(kong,"lines").Length;i<n;i++) kong.Next(); }
        Check(kong.ReadyForNight && !stay.Playing,"Everyone met; nothing starts until he goes back to her");
        kong.Open(W); Check(Field<string[]>(kong,"lines")[1].Contains("하늘"),"She notices the sky going red");
        for(int i=0,n=Field<string[]>(kong,"lines").Length;i<n;i++) kong.Next();
        Check(kong.Invited && stay.Now==PrototypeKongStay.Phase.Evening && combat.Paused && !combat.player.CanMove,"Her invitation leads into the evening scene");
        Check(combat.Hud.Hidden,"The HUD steps aside for it");
        foreach(var part in PrototypeKongStay.Evening) Check(JinaArt.Portrait(part.portrait)!=null,"Evening portrait "+part.portrait);
        var evening=PrototypeKongStay.Evening;
        Check(evening[0].who==W && evening[0].line.Contains("묵고") && evening[1].who==PrototypeKongVillage.Who.Patjwi && evening[1].line.Contains("외간")
            && evening[3].portrait=="kongjwi_stare" && evening[3].line=="……" && evening[5].line.Contains("자고 가든지"),"She asks, Patjwi shouts, Kongjwi stares, Patjwi gives in");
        Check(evening[1].portrait=="patjwi_angry" && evening[1].line.Contains("무슨 소리야") && evening[5].portrait=="patjwi_down","Patjwi has the face for each");
        stay.NextLine(); stay.NextLine(); stay.NextLine(); stay.NextLine(); Call(kong,"LateUpdate");
        Check(GameObject.Find("팥쥐").transform.position.x>PrototypeWorld.Patjwi.x+.2f,"Patjwi shrinks back from the stare");
        Capture("stay-evening-stare",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);

        // The night runs by itself, picture after picture; then the morning.
        var view=Camera.main.GetComponent<PrototypeView>();
        stay.Show(PrototypeKongStay.Phase.Night,1); Call(view,"LateUpdate");
        float fit=Mathf.Max(PrototypeKongStay.RoomSize,PrototypeKongStay.PictureWidth/2/Camera.main.aspect);
        Check(stay.InRoom && Mathf.Abs(Camera.main.transform.position.x-PrototypeKongStay.RoomX)<.01f && Mathf.Abs(Camera.main.orthographicSize-fit)<.01f,"The camera moves onto the pictures, one filling the screen: "+Camera.main.transform.position.x+" "+Camera.main.orthographicSize+" "+fit+" "+stay.InRoom);
        Capture("stay-night-house",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Night,PrototypeKongStay.RoomAt+1);
        Check(stay.BedSprite=="lie" && !stay.ShadowShown,"He sleeps; nothing at the door yet");
        Capture("stay-night-sleep",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Night,PrototypeKongStay.ShadowStop+.6f);
        Check(stay.ShadowShown && Mathf.Abs(stay.ShadowX)<.01f && stay.BedSprite=="lie","A shape stands square on the door");
        Capture("stay-night-shadow",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Night,PrototypeKongStay.SitAt+1);
        Check(stay.BedSprite=="sit","He sits up");
        Capture("stay-night-sit",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Night,PrototypeKongStay.EyeAt+.5f);
        Check(stay.HoleShown && stay.EyeShown,"The close shot: a hole in the paper, an eye at it");
        Capture("stay-night-eye",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Night,PrototypeKongStay.LieAt+1);
        Check(stay.BedSprite=="lie" && !stay.ShadowShown && !stay.EyeShown,"It has gone; he lies down again");
        stay.Show(PrototypeKongStay.Phase.Morning,1);
        Check(combat.Paused,"Morning comes");
        Capture("stay-morning-road",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Morning,PrototypeKongStay.MorningRoomAt+1);
        Check(stay.BedSprite=="sit" && combat.Paused,"He is up, in the sun, the hole still in the door");
        Capture("stay-morning",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        // Her plea, his answer, the silence, the hill: he cannot leave without choosing.
        stay.Show(PrototypeKongStay.Phase.Parting,0);
        Check(combat.Paused && !stay.Choosing && stay.PartingLines==PrototypeKongStay.PartingAsk,"She asks him to stay on");
        stay.NextPart();
        Check(stay.Choosing,"He has to answer");
        stay.NextPart();
        Check(stay.Choosing && stay.PartingPage==PrototypeKongStay.PartingAsk.Length-1,"There is no going on without an answer");
        foreach(var way in new[]{PrototypeKongStay.PartingPolite,PrototypeKongStay.PartingBlunt})
        {
            Check(System.Array.Exists(way,l=>l.line=="……"),"She falls silent");
            Check(System.Array.Exists(way,l=>l.line.Contains("뒷산")),"She asks him up to the hill");
            Check(way[0].portrait==null && way[way.Length-1].portrait==null,"He answers first and has the last thought");
            foreach(var l in way) if(l.portrait!=null && l.portrait!="stare") Check(JinaArt.Portrait(l.portrait)!=null,"Parting portrait "+l.portrait);
        }
        stay.Choose(true);
        Check(stay.Blunt && !stay.Choosing && stay.PartingLines==PrototypeKongStay.PartingBlunt,"He says it to her face");
        stay.Wake(); Call(view,"LateUpdate");
        Check(kong.Blunt,"She remembers how he answered");
        Check(!stay.Playing && !stay.InRoom && kong.Stayed && combat.player.CanMove && Mathf.Abs(player.position.x-PrototypeWorld.KongHouseX)<.01f && !combat.Paused,"He comes out of the house in control");
        Check(Mathf.Abs(Camera.main.transform.position.x-view.ViewX(player.position.x))<.01f && Mathf.Abs(Camera.main.orthographicSize-5.5f)<.01f,"The camera is back on the village");
        Check(GameObject.Find("잔치 명단")!=null,"Now a paper lies by the crocks");
        Capture("kong-morning",new Vector3(PrototypeWorld.JangdokX-3,2,-10),5.5f);

        // Morning talk does not repeat the night; the three clues, then her mask drops because she is caught.
        kong.Open(W); Check(Field<string[]>(kong,"lines")[0].Contains("뒷산"),"Kongjwi speaks of the hill");
        for(int i=0,n=Field<string[]>(kong,"lines").Length;i<n;i++) kong.Next();
        Check(!stay.Playing && !kong.Revealed,"No second night");
        kong.Open(PrototypeKongVillage.Who.Patjwi); Check(kong.Portrait=="patjwi" && JinaArt.Portrait("patjwi")!=null,"Patjwi speaks with her portrait");
        for(int i=0,n=Field<string[]>(kong,"lines").Length;i<n;i++) kong.Next();
        kong.ExamineCrocks(); kong.ReadFeastList();
        Check(kong.HeardPatjwi && kong.SawCrocks && kong.ReadList && kong.Clues==3,"Three clues");
        kong.Open(W);
        Check(System.Array.Exists(Field<string[]>(kong,"lines"),l=>l.Contains("들켰으니")) && System.Array.Exists(Field<string[]>(kong,"lines"),l=>l.Contains("젓갈로 담가")),"Caught, Kongjwi drops her mask");
        Check(kong.Portrait=="kongjwi" && Next(kong)=="kongjwi_true" && JinaArt.Portrait("kongjwi_true")!=null,"Her portrait turns after her first line");
        for(int i=0;i<2;i++) kong.Next();
        Check(kong.Revealed,"She is unmasked");
        Set(kong,"nextScene",0f); Call(kong);
        Check(kong.SceneAt<0,"No more scenes once she is unmasked");
        combat.Restart();
        Check(!kong.Revealed && !kong.Stayed && kong.Clues==0 && !kong.Met(W) && !kong.KeepsakeTold && !kong.IsOpen && !stay.Playing && GameObject.Find("잔치 명단")==null,"Restart forgets the clues and the night");
    }
    static string Next(PrototypeKongVillage kong) { kong.Next(); return kong.Portrait; }

    // Words and small things added around the story; the sword's three steps; what the world stands on.
    static void Polish()
    {
        combat.Restart();
        var fresh=new QuestProgress();
        Check(PrototypeQuest.ElderPages(fresh).Length>=3 && string.Join("",PrototypeQuest.ElderPages(fresh)).Contains("달, 강, 산"),"The elder explains the way a page at a time, and gives the order of the signs");
        Check(PrototypeEnding.Coda(true,true,true)[0].Contains("은비녀") && PrototypeEnding.Coda(false,false,false)[0].Contains("잔치"),"The ending's last words follow what he saw in Kongbat");
        var W=PrototypeKongVillage.Who.Patjwi;
        Check(string.Join("",PrototypeKongVillage.Lines(W,true,true,true,true,false,true)).Contains("비녀"),"Once her sister is unmasked Patjwi points him to the hairpin");
        var kongjwiAfter=PrototypeKongVillage.Lines(PrototypeKongVillage.Who.Kongjwi,true,true,true,true,false,true);
        Check(kongjwiAfter.Length==2 && kongjwiAfter[1].Contains("뒷산"),"Unmasked, she only reminds him of the hill");
        Check(string.Join("",PrototypeKongVillage.Lines(PrototypeKongVillage.Who.Kongjwi,false,false,false,false,false,true,true))!=string.Join("",PrototypeKongVillage.Lines(PrototypeKongVillage.Who.Kongjwi,false,false,false,false,false,true,false)),"She speaks differently once he has said it to her face");
        Check(string.Join("",PrototypeKongVillage.Lines(W,false,false,false,true,false,true)).Contains("배를 못 타"),"Patjwi tells him she cannot follow by boat");
        foreach(var who in new[]{PrototypeKongVillage.Who.Anak,PrototypeKongVillage.Who.Namu})
        {
            string before=string.Join("",PrototypeKongVillage.Lines(who,false,false,false,false,false,false)), after=string.Join("",PrototypeKongVillage.Lines(who,false,false,false,false,false,true)), last=string.Join("",PrototypeKongVillage.Lines(who,true,true,true,true,false,true));
            Check(before!=after && after!=last,"The villagers' talk moves with the story");
        }
        foreach(var spot in PrototypeLore.Spots) Check(spot.x>-70 && spot.x<PrototypeWorld.HillEnd && !string.IsNullOrEmpty(spot.said) && !string.IsNullOrEmpty(spot.prompt),"Lore spot "+spot.prompt);
        var lore=combat.Lore; var p=combat.player.transform;
        p.position=new Vector3(PrototypeLore.Spots[0].x,p.position.y,0);
        Check(lore.Near()==0,"He can read what he stands by");
        p.position=new Vector3(PrototypeWorld.Spawn.x,p.position.y,0);
        Check(SeonbiVisual.SwordLength(0)==SeonbiVisual.SwordDuration && SeonbiVisual.SwordHit(0)==SeonbiVisual.HitDelay,"The first cut is as it always was");
        Check(SeonbiVisual.SwordLength(1)<SeonbiVisual.SwordLength(0) && SeonbiVisual.SwordLength(2)>SeonbiVisual.SwordLength(0),"The second is quicker, the third heavier");
        for(int i=0;i<3;i++) Check(SeonbiVisual.SwordHit(i)<SeonbiVisual.SwordLength(i),"Each blow lands within its swing");
        foreach(var art in new[]{"stele","camp","village","forest","ferry","kong","pass"})
        {
            Check(GameObject.Find("지면 "+art)!=null && GameObject.Find("하늘 "+art)!=null,"Ground and sky for "+art);
            Check(JinaArt.Strip("ground_"+art)!=null,"Ground art for "+art);
        }
        foreach(var part in new[]{"arc_0","arc_2","spark_3","dust_3","ring_0","glint","seal"}) Check(JinaArt.Fx(part)!=null,"Effect "+part);
        foreach(var part in new[]{"grain_paper","grain_wood","grain_night","corner","vignette","icon_sword","icon_coin"}) Check(JinaUI.Art(part)!=null,"UI art "+part);
        foreach(var part in new[]{"ext_evening","ext_evening_flinch","ext_night","ext_night_dark","ext_morning","ext_rooster","room_night","room_night_front","room_night_floor","room_night_sit","room_morning","stare","close_paper","close_front","close_shadow","close_hole_2","close_eye_2","shadow_2"}) Check(JinaArt.Night(part)!=null,"Picture "+part);
        // Sparks and numbers leave no trace after a restart, and the clock is never left held.
        JinaFx.Hit(Vector3.zero,1,Color.white); JinaFx.Number(Vector3.zero,10,Color.white); JinaFx.Shock(Vector3.zero);
        combat.Restart();
        Check(Time.timeScale==1,"The clock runs at full speed after a restart");
    }
}
