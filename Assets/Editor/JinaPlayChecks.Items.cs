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
        Check(PrototypeMaps.All.Length==7 && PrototypeMaps.Index(PrototypeWorld.Spawn.x)==2 && PrototypeMaps.Index(-50)==0 && PrototypeMaps.Index(40)==3 && PrototypeMaps.Index(90)==4 && PrototypeMaps.Index(130)==5 && PrototypeMaps.Index(170)==6,"Seven maps, the village where he starts");
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
            && PrototypeKongVillage.Lines(PrototypeKongVillage.Who.Patjwi,false,false,false,false,true,false)[0].Contains("비녀"),"Kongjwi invites, the wife curses Patjwi, Patjwi knows her mother's hairpin");

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

        // Finishing Kongjwi's talk starts the evening: she offers a bed, Patjwi objects, one look ends it.
        kong.Open(W); Check(kong.IsOpen && combat.Paused && kong.Portrait=="kongjwi" && JinaArt.Portrait("kongjwi")!=null,"Talking to Kongjwi holds the game, with her portrait");
        for(int i=0;i<3;i++) kong.Next();
        Check(kong.Invited && stay.Now==PrototypeKongStay.Phase.Evening && combat.Paused && !combat.player.CanMove,"Her invitation leads into the evening scene");
        Check(combat.Hud.Hidden,"The HUD steps aside for it");
        foreach(var part in PrototypeKongStay.Evening) Check(JinaArt.Portrait(part.portrait)!=null,"Evening portrait "+part.portrait);
        var evening=PrototypeKongStay.Evening;
        Check(evening[0].who==W && evening[0].line.Contains("묵고") && evening[1].who==PrototypeKongVillage.Who.Patjwi && evening[1].line.Contains("외간")
            && evening[3].portrait=="kongjwi_stare" && evening[3].line=="……" && evening[5].line.Contains("자고 가시지요"),"She asks, Patjwi makes excuses, Kongjwi stares, Patjwi gives in");
        foreach(var part in evening) Check(!part.line.Contains("!"),"Nobody raises her voice in the evening");
        stay.NextLine(); stay.NextLine(); stay.NextLine(); stay.NextLine(); Call(kong,"LateUpdate");
        Check(GameObject.Find("팥쥐").transform.position.x>PrototypeWorld.Patjwi.x+.2f,"Patjwi shrinks back from the stare");
        Capture("stay-evening",new Vector3(PrototypeWorld.Kongjwi.x,1,-10),3);

        // The night, in the small room of its own, runs by itself; then the morning.
        var view=Camera.main.GetComponent<PrototypeView>();
        stay.Show(PrototypeKongStay.Phase.Night,1); Call(view,"LateUpdate");
        Check(stay.InRoom && Mathf.Abs(Camera.main.transform.position.x-PrototypeKongStay.RoomX)<.01f && Mathf.Abs(Camera.main.orthographicSize-PrototypeKongStay.RoomSize)<.01f,"The camera moves into the guest room");
        Check(stay.BedSprite.EndsWith("player_sleep"),"He sleeps");
        Check(Mathf.Abs(stay.BedBody.sprite.pixelsPerUnit-JinaArt.Load(JinaArt.Player).pixelsPerUnit)<.01f,"In bed he keeps his in-game size");
        Check(stay.BedQuilt.sortingOrder>stay.BedBody.sortingOrder && stay.BedQuilt.bounds.min.x>stay.BedBody.bounds.min.x+.4f && stay.BedQuilt.bounds.min.x<stay.BedBody.bounds.max.x,"The quilt covers him from the chest, his collar showing above it");
        Capture("stay-night-sleep",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Night,PrototypeKongStay.ShadowStop+.6f);
        Check(stay.BedSprite.EndsWith("player_sit") && Mathf.Abs(stay.ShadowPosition.x-PrototypeKongStay.DoorX)<.01f,"A shape stands at the door and he sits up");
        Check(stay.BedQuilt.bounds.min.x<stay.BedBody.transform.position.x-.36f && stay.BedQuilt.bounds.max.y>stay.BedBody.transform.position.y+.75f,"Sitting, the quilt hides where he is cut at the lap");
        Capture("stay-night-shadow",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Show(PrototypeKongStay.Phase.Morning,2);
        Check(stay.BedSprite.EndsWith("player_sit") && combat.Paused,"Morning comes");
        Capture("stay-morning",new Vector3(PrototypeKongStay.RoomX,2,-10),PrototypeKongStay.RoomSize);
        stay.Wake(); Call(view,"LateUpdate");
        Check(!stay.Playing && !stay.InRoom && kong.Stayed && combat.player.CanMove && Mathf.Abs(player.position.x-PrototypeWorld.KongHouseX)<.01f && !combat.Paused,"He comes out of the house in control");
        Check(Mathf.Abs(Camera.main.transform.position.x-view.ViewX(player.position.x))<.01f && Mathf.Abs(Camera.main.orthographicSize-5.5f)<.01f,"The camera is back on the village");
        Check(GameObject.Find("잔치 명단")!=null,"Now a paper lies by the crocks");
        Capture("kong-morning",new Vector3(PrototypeWorld.JangdokX-3,2,-10),5.5f);

        // Morning talk does not repeat the night; the three clues, then her mask drops because she is caught.
        kong.Open(W); Check(Field<string[]>(kong,"lines")[0].Contains("주무셨"),"Kongjwi asks how he slept");
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
        Check(!kong.Revealed && !kong.Stayed && kong.Clues==0 && !kong.IsOpen && !stay.Playing && GameObject.Find("잔치 명단")==null,"Restart forgets the clues and the night");
    }
    static string Next(PrototypeKongVillage kong) { kong.Next(); return kong.Portrait; }
}
