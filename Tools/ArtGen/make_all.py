# Regenerates every drawn or derived picture from its script, in order, and reports what changed.
# Every script reproduces the committed art pixel for pixel, so on a clean tree nothing should change.
#   python Tools/ArtGen/make_all.py           regenerate everything
#   python Tools/ArtGen/make_all.py --check   regenerate, then fail (exit 1) if any picture differs from before
# Run from the project root.
import hashlib, os, shutil, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ART = "Assets/Resources/JinaV4"
SOUND = "Assets/Resources/JinaSound"   # teammates' wavs are left alone; make_sound.py writes only its own
# (script, arguments). Order matters only where one picture feeds another: make_young before the frames cut from him.
JOBS = [
    ("make_props.py", [f"{ART}/13_Props"]),                       # ledges, tavern, keeper (+ run), charm ...
    ("make_backdrops.py", [f"{ART}/13_Props"]),                   # bg_<area>_far / _near
    ("make_portraits.py", [f"{ART}/17_Portraits"]),            # talk-window portraits
    ("make_night.py", [f"{ART}/18_Night"]),                      # the night at Kongjwi's house, full-screen pictures
    ("make_ground.py", [f"{ART}/13_Props"]),                     # ground strips, skies, deep river, clouds
    ("make_fx.py", [f"{ART}/20_Fx"]),                            # crescents, sparks, dust, shock rings
    ("make_ui.py", [f"{ART}/19_UI"]),                            # grains, brass corners, HUD icons, vignette
    ("make_kong.py", [ART]),                                     # Kongbat village people and props
    ("make_kong_boss.py", [ART]),                                # the Kongjwi demon, what she throws, hill crocks and roofs
    ("make_pass.py", [f"{ART}/13_Props"]),                       # jangseung, shrine tree, cairn, boulder
    ("make_ferry.py", [ART]),                                    # ferryman, boat, hut, reeds, water
    ("make_soldier.py", [ART]),                                  # government soldiers: blade and bow, three ranks; their arrow
    ("make_road_mobs.py", [ART]),                                # dokkaebi-bul (wisp) and dueoksini (brute)
    ("make_hanyang.py", [ART]),                                  # Hanyang people; fortress, station, grave, street, hall, manor props
    ("make_final_boss.py", [ART]),                               # the councillor and the eodugsini, what he throws
    ("make_hwando_sheath.py", [f"{ART}/04_Weapons"]),
    ("make_clubs.py", [f"{ART}/04_Weapons"]),                    # dokkaebi clubs: plain (purified) and bloody (slain)
    ("make_items.py", [f"{ART}/14_Items"]),                       # inventory item icons
    ("make_peddler.py", [f"{ART}/12_Peddler/peddler_idle.png"]),  # peddler idle + run
    ("make_young.py", [ART]),                                    # clean-shaven seonbi; everything below is derived from it
    ("make_run.py", [f"{ART}/10_Player_Walk", f"{ART}/11_Player_Run"]),
    ("make_player_down.py", [ART]),
    ("make_player_ko.py", [ART]),                                # game-over collapse
    ("make_aim.py", [ART]),                                      # aimed bow and sword frames (mouse aiming)
    ("make_boss_death.py", [ART]),
    ("make_boss_remorse.py", [ART]),
    ("make_ghost_death.py", [ART]),
    ("make_ghost_attack.py", [ART]),
    ("make_elder.py", [ART]),
    ("make_sound.py", [SOUND]),                                  # synthesized music and sounds where the team has none
]

def digests():
    out = {}
    for root, _, files in [w for d in (ART, SOUND) for w in os.walk(d)]:
        for f in files:
            if f.endswith(".png") or f.endswith(".wav"):
                path = os.path.join(root, f).replace("\\", "/")
                with open(path, "rb") as fh: out[path] = hashlib.md5(fh.read()).hexdigest()
    return out

before = digests()
for script, args in JOBS:
    subprocess.run([sys.executable, os.path.join(HERE, script)] + args, check=True)
shutil.rmtree(os.path.join(HERE, "__pycache__"), ignore_errors=True)
after = digests()
changed = sorted(p for p in after if before.get(p) != after[p])
removed = sorted(set(before) - set(after))
for p in changed: print(("new " if p not in before else "changed ") + p)
for p in removed: print("missing " + p)
print(f"{len(after)} pictures and sounds, {len(changed)} changed")
if "--check" in sys.argv and (changed or removed): sys.exit(1)
