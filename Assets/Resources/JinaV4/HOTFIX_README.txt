Jina Seonbi — Unity sprite hotfix (based on v4)

Fixes:
- Correct sword sheet -> 02_Player_Attack_Sword/sword_attack_00..03.png
- Correct bow sheet -> 03_Player_Attack_Bow/bow_attack_00..03.png
- Correct boss club sheet -> 05_Boss_Brainwashed/boss_attack_00..03.png
- New jump -> 09_Player_Jump/jump_00..03.png

All 16 motion images are independent transparent RGBA PNGs at 1024x768.
Sprite extraction uses connected components, NOT 4 equal vertical cuts.
This prevents mixing neighboring characters and preserves blades/club tips.
Each animation is feet-aligned in its own frame and the canvas has extra room.
The flying arrow in the final bow frame is included at its actual release height.

Unchanged v4 resource bytes (8 sprites and README) are preserved exactly.
Unity settings: Sprite Mode=Single, Filter Mode=Point, Compression=None.
Prefer consistent Custom Pivot based on world ground point x=420/1024, y=48/768
(bottom-left coordinate; normalized x=0.41015625, y=0.0625) for grounded attacks.
For jump the same pivot is retained and vertical sprite displacement is depicted in frames.
Actual animation speed and hitboxes should be tuned during Unity integration.
