# Artillery Royale: Todo list

This is the list of things I was working on when I decided to stop working fulltime on the game,
it's a direct translation of some french todo list I had in Workflowy.

I put this here for reference.  
It's ordered by priority and have hashtag by subjects.

## Work in progress

  - [ ] When score then turn locked
  - [ ] Ball must rest before changing the turn
  - [ ] #bug the bullet does not take the force explosion
  - [ ] Game mode: magic ball
    - [ ] Why not infinite life?
    - [ ] Shouldn't you necessarily shoot a person to take the bullet?
    - [ ] Why don't you shoot the ball over the round?
    
## Prio 1

  - [ ] Land with hole but in fact not #bug in some craters for example but not only
    "Maybe a way to line render the pieces of polygons that are tricky - either very small - or very thin - with tight angles - all in brown color to make it look like it's terrain?"
    - [ ] Falls on the edges big problem #bug
  - [ ] #bug #map the character stuck on an egg / land (check all new colliders)
  - [ ] #physics #map #bug Slopes not yet perfect
    - [ ] Sometimes when there is projection and resting, in fact the turn according to the character falls because not well posed
    - [ ] When we slide on an invalid slope it would be better if slower
    - [ ] some cases where the character is blocked
  - [ ] #bug often the character is in jump mode while on the ground
    - [ ] may also be linked to the fact that on certain slopes it retry remains while it seems ok
  - [ ] #network if shooting at the end of the turn / timer then it remains in this state in network at the other #bug
    "can also be in other cases
    "
    - [ ] #network the characters freezen in network at the end of the turn in last position but not always #bug
  - [ ] When we jump at the end of the retreat and we fall on a rounded edge (crater type) we slide #physics
  - [ ] Stop shield when sudden death
  - [ ] Correct the edges of beams, perhaps by adding roundings
  - [ ] Boring slide, when a jump towards a slope for example

## Prio 2

  - [ ] Show the rules capture the king when part network / see all the time (no flag?)
  - [ ] lack of rhythm
  - [ ] #network lag on the walk en network animation. Fair #bug
  - [ ] The maps are too big (wide and high) #gameplay
  - [ ] #camera auto zoom out with meteor and fireworks
  - [ ] #network force rest not sync #bug
    "while it should via character sync?"
  - [ ] Make an animation with the King who commits suicide when captured
  - [ ] #map Avoid too big angles after an explosion (apply some kind of erosion?)
    - [ ] #map Avoid too small map pixels after an explosion (filter for a minimum size)
  - [ ] #UI #bug the map is not centered under windows in the chooser map
  - [ ] #Weapon Hi jump (jump boots)
    "These are little boxes with flames, and when you jump they disappear
    but as long as not jumped they are there and prevent you from hurting yourself by falling "
    - [ ] add sfx
    - [ ] visual network sync
  - [ ] #UI #gameplay scheme / mode
    "there are modes that only work in local or multi or solo
    the modes are ini type text files with info on which param and which values ​​to see certain blocked params
    modes can also set weapons
    ditto sandbox mode which gives all the weapons it is a mode (in solo only) "
    - [ ] Sandbox mode is broken (not yet)
  - [ ] #gameplay Transfer tutorial and put texts with objective type double jump beam and load fire?
  - [ ] #ui #gameplay Add an auto re-game at the end of the game
  - [ ] #arme #visuel shield add preview when selecting
  - [ ] #sound his sticky pomegranate
  - [ ] #camera Camera on the network fireworks

## Prio 3

  - [ ] #sound voice the characters say something back when you put a board
  - [ ] IA
    - [ ] Beam add in #IA simulation (existance)
    - [ ] #IA Add shotgun to ai
    - [ ] #IA not put the angle detectio for the grenade drop because it shoots with low angles sometimes
    - [ ] #IA must move away from the edges
    - [ ] #IA when the character advances, jump from time to time for a more natural feel
    - [ ] #IA must separate its characters
    - [ ] #IA must shoot at the feet and not in the face (changing the power -0.1 should be enough?)
    - [ ] #IA puts down a grenade and has to go (not stand next to it)
    - [ ] #IA if no shot possible as time remaining then go back to the start and do a simulation there
    - [ ] #IA if really nothing possible then do a simulation without terrain for a bazooka with wind
    - [ ] #IA ever uses beam?
    - [ ] #IA signal no need #bug
    - [ ] #IA #bug the AI ​​makes the mac lag in AITestWeapon ()
    - [ ] #IA do not show #bug pickup messages
    - [ ] #IA check if you touch from the basic position then move to the desired position check then come back if not better
      - [ ] Force to move more even if already at the right point
    - [ ] #IA shoots herself when she doesn't know what to do (too close to the wall or if another character has her in the way)
    - [ ] #IA blocked in some cases => if not end of move in X sec freeze and shoot
    - [ ] #IA Falls from somewhat steep edges
    - [ ] #IA sometimes blocked and loses turn
      "Do a fall back mechanism if not pulled before x time?"
    - [ ] #IA don't stop at goal sometimes
      "Cheater and position the agent on the goal at the end of each segment?
      Probably only if not too far to avoid glitches "
      - [ ] Overreach and falls when exceeding the goal especially on descents
        "The slope may be too tolerant on the edges.
        Maybe according to the establishment of the links between the graphs we can implement a trim method which reduces the edges by a segment? "
    - [ ] #IA The 45 degree jumps are missed because the 50 degree rule contradicted?
      "Create one more rule, or two, counting the 90 degrees in skip> or <"
    - [ ] #IA shoots through walls
    - [ ] #IA Blocked in some deep holes (but not multiple) and with a protruding rim)
      "in ascii art that would give
      ——---,
               /
             (
               \
                ------
      \ o / "
    - [ ] #IA Stays too often down / in the holes
      "Put a big bonus in retreat and even best place to fire to move upwards. And x 5 when sudden death
      "
    - [ ] #IA Sudden death if my life is> 45, that I am low (compared to the lava thus) that there are 3 or less nobody in my team then I beam and I go up 1/2
      - [ ] ia if the lava is too ready to do everything to escape (including beam) even if possible shooting
    - [ ] #IA Jump over the mines
    - [ ] #IA Later, simulation of firing down from the edges of the path in grenade or rocket!
      - [ ] do a shot simulation from the edges of the path down (if the human is hiding below)
    - [ ] #IA Add mine explosions and mines in the simulation
  - [ ] #bug #ui back in the multi / game menu etc doesn't always return where we think
  - [ ] No back button on the map generation screen #ui
  - [ ] #network Put a loader between the tour network as in worms at the top right with the computers
    - [ ] add a load when it is the change of turn
  - [ ] #network Roll of grenades doesn't look sync, is it lagging? same mortar #bug
  - [ ] when the grenade falls in the lava make lava sfx, same with all the dropables #visual
  - [ ] # sniper weapon possibly head shot, add a voice say now dead direct on the victim
  - [ ] Recall of the current weapon at the top right #UI
  - [ ] #network shield stamp not sync
  - [ ] #network Move from dash not sync
  - [ ] #network manage Character :: Signal
  - [ ] #network sfx box not sync
  - [ ] #network SFX not wash sync
  - [ ] #sound Shield not at the right time #bug
  - [ ] #sound Meteor invocation
  - [ ] #sound Map select A REVIEW
  - [ ] #sound Dash
  - [ ] #sound Big bomb bounce
  - [ ] #voices Dash launches a "miss" sound on himself #bug
  - [ ] #voices If all we do is work then the character Signal again #bug
  - [ ] #voices When FF the affected character launches HIT (objective complete) #bug
  - [ ] #voices When we hurt ourselves we can't even launch a #bug hit
  - [ ] #voices Traitre etc for the moment you touch one of your friends
  - [ ] #map Objects are not placed, they should always be above the map and not inside as often
  - [ ] # Dash weapon
    - [ ] Dash may not be the best word
    - [ ] not perfect but it's ok for now
  - [ ] #Weapon Shield
    - [ ] work on map borders during explosions
    - [ ] add in IA simulation (use and existence)
    - [ ] preview
  - [ ] # weapon Sniper rifle
    - [ ] sprite
    - [ ] icon
  - [ ] #solo Mission with gold silver bronze according to number of turns then if gold note the time
  - [ ] #solo a kind of progress with unlockable powers (to keep people coming back, a bit of RPG too)
    "but see how not to block the multi which would quickly be unbalanced"
  - [ ] #camera Do not Follow the retreat in network
  - [ ] #bug #UI Esc in the previous menu, esc on the popup closes the popup (popup exit only)
  - [ ] #bug Performances (CG Alloc everywhere)
    - [ ] #bug Lag on the position of the characters
    - [ ] Performances
      - [ ] AI wait a bit before ask for path findind after a shot so shot don't freeze
      - [ ] AI wait a bit before ask for path finding so UI don't freeze
      - [ ] CG Alloc de ouf in the refresh on StreamPlay (reflection) and call many time
      - [ ] CG Alloc of ouf in the PathFinding (used by the AI ​​but also by the map position)
      - [ ] https://www.reddit.com/r/Unity3D/comments/6dh9mt/things_i_learnt_today_gc/
      - [ ] Dash lag a bit (smells like CG alloc)
    - [ ] Work on performance with cg alloc reduce
  - [ ] # Meteor weapon
    - [ ] meteor which spans on the personal network (start too early / init) + SFX
  - [ ] #network Sync first map style works badly / not (in-depth review)
  - [ ] #map #bug stamp on throne shields not ok (but generally not ok)
  - [ ] #sound #network end of turn in network the voice is too late it's the other
    - [ ] die text / voice not sync network
  - [ ] #UI #bug Esc on the choice of map fail
  - [ ] # rainbow weapon
    "Knowing that if it is a power, it is not possible for the moment to have several powers per character"
  - [ ] #gameplay Virer tuto => put the info in the training vs dummies
  - [ ] #Magic rabbit weapon 🐇 comes out of his top hat
  - [ ] #gameplay Be careful the lava is too close to the ground #bug
  - [ ] #UI Help menu and config in the game too + display ingame help once if never displayed before
  - [ ] #UI Make the wind more visual in the UI
  - [ ] #ui #camera The mouse is too sensitive to move the view

## Details

  - [ ] Meteor must come from much higher #Weapon
  - [ ] #network Create a game gives a code to join it and / or a link that starts the game?
    - [ ] Open Windows app link js fragment.
  - [ ] #visual Move SFX to parent object
    - [ ] ex: pickup box
    - [ ] and remove from SFX manager?
  - [ ] #UI Menu to bring / leave the left character
  - [ ] #UI New screen only for people who already have the game
  - [ ] #UI Pk not make a custom cursor because used to aim
  - [ ] #UI #bug we can open the weapon drawer while move in progress
  - [ ] #UI In the weapon selector the character no longer holds the weapon
    "he releases his arms and goes back to idle"
  - [ ] #sound the mine bounce (the sound of the mine should be 3 sec max now)
  - [ ] #sound #bug The atmosphere doesn't stop at the end of the scene
  - [ ] #sound No sound fall (hhhhuuu) when in projection
  - [ ] #sound #bug its UI click does not work
  - [ ] #sound Jump with boots
  - [ ] #sound Skip
    - [ ] valid / canceled
  - [ ] #voices When dead then it sounds hurt (bug) but it looks good (perpetuate)
  - [ ] #voices Hmm? ho? etc for the moment when someone puts a droppable on you
  - [ ] #voices When dead then stop the other falling and bounce sounds
  - [ ] #voices Bounce sound missing #bug
  - [ ] #voices When we fall there is no sound of the bounce at the end
  - [ ] #voices Its hoho when in a mine but not active
  - [ ] #sound #bug the music starts while we are in map choose
  - [ ] #map Less blocky terrain, maybe several levels of smoothing on the angles?
  - [ ] # weapon Hud of weapons must scroll
  - [ ] #Weapon Toxic Barel
    "A barrel of radioactive material that rolls until it meets someone where it explodes in radioactive material and toxic smoke and makes you sick (in addition to losing points)"
  - [ ] #Weapon Langoliers
    "https://fr.wikipedia.org/wiki/Les_Langoliers"
    - [ ] Erase a horizontal line from the place where it is invoked
  - [ ] # Rope weapon
    - [ ] bonce a lot
    - [ ] rope in hig join
    - [ ] right to double jump at the end of the rope
    - [ ] the character is in ragdoll tied by his hands
  - [ ] #map The mines laid straight even if the ground is not straight
  - [ ] #Weapon Jet pack
  - [ ] # weapon Stupid rabbit 🐇
    "Go straight ahead and make a U-turn if the wall is too high and if the slope is too large (up or down), it jumps to try to pass it. Explodes after 10 sec or if action is taken."
  - [ ] # weapon Rifle that launches a creeping worm
    "The line where snake runs along the ground perfectly except when the slope is 90 where you lose control and it falls. It explodes when it crosses a crate or a character. If you press action (fire) before it digs (therefore towards) in the ground at 90 degrees. It doesn't make a hole behind it. "
  - [ ] #Weapon Teleport
    "Basically the guy activates teleport, the ship grabs the guy and goes up then when he re-validates it re-drops it and he can left / left to choose (rayast for the deposit, he has the retreat time to pose) / OR / The chosen guy teleport in arms and his aim pointer is controlled by H / B / L / D and he chooses where he teleports
    OR it zooms out to the max and the controls to move the view become a control to move a pointer "
  - [ ] #Weapon Mine becomes a weapon
    - [ ] change their colliders to be bigger and rounder (but not circle)
  - [ ] # Sticky grenade weapon: add sfx which tastes
  - [ ] #Weapon box: positioning on the map when falling from the sky
  - [ ] #Weapon ladder that goes across the field, see who really makes a hole in the collider like that good
  - [ ] #arme Make spells and weapons in UI in the game, each of the characters has his animal mascot?
  - [ ] #Mine turtle weapon
  - [ ] # Storm alarm
  - [ ] #Dragon weapon?
  - [ ] #Weapon Mud Boulder.
  - [ ] #Weapon Living flameches.
  - [ ] # weapon when the characters use a spell, then make a kind of cane / wand
  - [ ] # weapon Some weapon such as mortar should not disappear immediately after firing
  - [ ] #arm a kind of drone
  - [ ] #gameplay uses the lexical field of spells rather than weapons? Spell vocabulary
  - [ ] #gameplay weapon selector right click problem not remove? or in any case we expect it to disappear when moving?
  - [ ] #gameplay does signal character at least voice have to be sync?
  - [ ] #gameplay End of round a bit long
  - [ ] #gameplay #feedback games can be long and boring if the characters are up vs down (especially with AI)
  - [ ] #gameplay Aiming aid
  - [ ] #gameplay The goodies appear or we can't take them
  - [ ] #visual Lack of relief / light
  - [ ] #visual Do not use layer groups in the UI or be very careful that the elements are not constantly updated #code
  - [ ] #visuel The sprite shapes corners
    "https://forum.unity.com/threads/2d-sprite-shape-is-out-of-preview-for-2019-3.818799/page-3#post-6870950"
  - [ ] #visual Sprite mask smoother (not possible as it is)
  - [ ] #visuel Background make mini paralaxis and smoke and wash bg
  - [ ] #visual Border / corners of shape sprites not ok
  - [ ] #visual Jump particle not at first jump than fall and second ...
    - [ ] Remove jump particles when thrown
  - [ ] #VFX visual on the appearance of boxes
  - [ ] #bug In full hd zoomed out the characters are a bit pixelated (mipmap fail)
  - [ ] #solo Online you play against your level and if less the weapons are deactivated
  - [ ] #server Allow restart at any time and not lose clients
  - [ ] #server Work on the server with game registration
  - [ ] #server Think about network fail and for that identify the master slave in the rounds via a uuid for potential reconnections
  - [ ] #server switch to int, switch to binary
  - [ ] #server Attention we can create false games via the entry of a false gameId
  - [ ] #server Configure the web socket and document it
  - [ ] #camera Add requests follow here and there
  - [ ] #camera du fireworks it's not good
  - [ ] #sound #bug its drop firework not good, do not put any
  - [ ] #sound #bug sniper's sound can be reversed? and then remove its explosion
  - [ ] #ui Icon in the bubbles are upside down if pointing to the left
  - [ ] #ui Review menu with grid missions already
  - [ ] #Personal map
  - [ ] #ui Make a button to save the scheme
  - [ ] #ui Change nickname
  - [ ] #ui #gameplay See your weapons when not playing
  - [ ] #map + Option map generation
    - [ ] algo
    - [ ] symmetry
    - [ ] islands
  - [ ] #ui People don't want to show the code while streaming, plan this
  - [ ] #ui #arme review the weapon icons + small readability on the crates (or / and enlarge the crates)
  - [ ] #UI rename beams to planks
  - [ ] #visual #bug on the blue throne but hey okay it does not go into captured mode (removal of the armchair etc)
  - [ ] #physics Damage <10 no ragdoll
  - [ ] #UI change the font of the bubbles to something bolder and more readable
  - [ ] #camera #bug when we fall in the lava in its direct turn the change of turn (camera) is too fast
    - [ ] Too fast after a weapon no retreat or a fall in lava
  - [ ] #sound Sound at the start of sudden death + its ambiance
  - [ ] #UI ingame in front of popup (eg quit)
  - [ ] #gameplay if we press move while in load fire it blocks the load fire #bug
  - [ ] #physics The mines must not collide with the characters (maybe already ok) nor generate the grounded and co raycast
  - [ ] #arm #visual jump boots need better ui
  - [ ] #gameplay #bug If shot engaged while moving and released, it fires with force 0 = not cool, avoid
  - [ ] #network cash register in network loss ui green #bug
  - [ ] In #network the fore background on personalities does not work #bug
    - [ ] personal pass not in front in network
  - [ ] #visual Make an SFX when the lava rises
    "she can flash to white for example"
    - [ ] sudden death make the lava rise on the first turn + sfx
  - [ ] #physics No more bouncing on mines / making them round
  - [ ] #sound When sudden death the character speaks and says n'imp the next turn #bug
  - [ ] #visual Use sorting groups
    "https://docs.unity3d.com/Manual/class-SortingGroup.html"
  - [ ] #sound #network Voice dead not present on the guest
  - [ ] #visual SFX splashing the wrong color
  - [ ] #gameplay No fall option
  - [ ] #Map platform according to the styles
  - [ ] #map Characters are not often positioned on platforms even when there is
  - [ ] #feedback #gameplay Aiming is hard
  - [ ] #ui Direct shortcut for weapon groups

## Ideas for later

  - [ ] #bug #physics the ragdoll #check combo is a problem because the collider slide completely, you have to re-ragdoll
    - [ ] #map #physics #bug In ragdoll often ca slide after
    - [ ] #physics double ragdoll which infinite slide not good #bug
    - [ ] Ragdoll allow re-ragdoll from zero
      "It seems that it is the main collider who has already started to re-grow, which poses a problem "
  - [ ] Mods
    - [ ] Allow community work with custom map etc (a forum type site where you can submit your maps, or mod.io)
    - [ ] mod map
      - [ ] image = image, or image + collider mask, optional background
      - [ ] generative = fill + border + objects (object with center bottom anchor + 20px), optional background
    - [ ] mod scheme = set of pre-defined options
    - [ ] mod package = map + scheme
    - [ ] voice mod = audio files
    - [ ] mod weapon = take an existing weapon and change its parameters and sprites
    - [ ] personal mod = skin + rig
    - [ ] Skins and other brawlhalla and fortnite stuff (free2play like)
      - [ ] Several races / peoples à la Starcraft
  - [ ] Make a direct chat while waiting for your turn?
  - [ ] Steam
    - [ ] steam integration, drm, pause, player name
    - [ ] When opening the Steam Overlay, by pressing the home button on the controller, the game does not pause and continues to function while navigating the Steam Overlay menu
      "pause if application in background should be enough?"
  - [ ] elite mode with that colliders and colors?
  - [ ] Twitch integration like sending airstrikes and co
  - [ ] Choose your color
  - [ ] Discord chat api for internal chat?
  - [ ] Live or twitch spectator mode?
  - [ ] Allow to Pause game and vote system for sudden death
  - [ ] Match making + ranking
    "match making in discord first"
    - [ ] Steam cloud save (for the ranking)
      "make an interface for all the clouds to come"
  - [ ] Export last 30 second in gif
  - [ ] Replay / or for full async games
    - [ ] you have to snap all the events that create objects (for example the stamps)
    - [ ] you need a replay mode which does not play the anim or the sounds
    - [ ] you have to know how to calculate unnecessary snaps
    - [ ] it is necessary that when the turn is played and replay it is recorded as is in the base
    - [ ] if a person loses the connection during his turn, he loses his turn
  - [ ] Anti cheat
    - [ ] mainly consists of locking the replay data
    - [ ] see how people cheat on other games and check
  - [ ] Add an anim when the character is very close to the edge (before falling)
  - [ ] Add different idle animations and also that of speaking and taking turns
    - [ ] Small animation like "cuckoo" when the character becomes active
  - [ ] Team color shader in overlay.
    "So for the shader: yes and no. The team color should be applied in" overlay "and not in pure and hard superposition. It would allow a real very precise control of the team colors"
