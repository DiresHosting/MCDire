/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
*/
using System;
using MCGalaxy.Blocks;

namespace MCGalaxy
{
    public sealed partial class Block
    {
        public static bool Walkthrough(byte block) {
			return block == air || block == shrub || (block >= water && block <= lavastill) 
			    || (block >= yellowflower && block <= redmushroom) || block == rope;
        }

        public static bool AllowBreak(byte block) {	
            switch (block) {
                case blue_portal:
                case orange_portal:

                case MsgWhite:
                case MsgBlack:

                case door_tree:
                case door_obsidian:
                case door_glass:
                case door_stone:
                case door_leaves:
                case door_sand:
                case door_wood:
                case door_green:
                case door_tnt:
                case door_stair:
                case door_iron:
                case door_gold:
                case door_dirt:
                case door_grass:
                case door_blue:
                case door_book:
                case door_cobblestone:
                case door_red:

                case door_orange:
                case door_yellow:
                case door_lightgreen:
                case door_aquagreen:
                case door_cyan:
                case door_lightblue:
                case door_purple:
                case door_lightpurple:
                case door_pink:
                case door_darkpink:
                case door_darkgrey:
                case door_lightgrey:
                case door_white:

                case c4:
                case smalltnt:
                case bigtnt:
                case nuketnt:
                case rocketstart:
                case firework:

                case zombiebody:
                case creeper:
                case zombiehead:
                    return true;
            }
            return false;
        }

        public static bool Placable(byte block) {
        	return !(block == blackrock || (block >= water && block <= lavastill))
			    && block < CpeCount;
        }

        public static bool RightClick(byte block, bool countAir = false) {
            if (countAir && block == air) return true;
            return block >= water && block <= lavastill;			
        }

        public static bool OPBlocks(byte block) { return Props[block].OPBlock; }

        public static bool Death(byte block) { return Props[block].CollisionDeath; }

        public static bool BuildIn(byte block) {
            if (block == op_water || block == op_lava || portal(block) || mb(block)) return false;
			block = Convert(block);
			return block >= water && block <= lavastill;
        }

        public static bool Mover(byte block) { return walkthroughHandlers[block] != null; }

        public static bool FireKill(byte block) { return block != air && Props[block].LavaKills; }
        
        public static bool LavaKill(byte block) { return Props[block].LavaKills; }
		
        public static bool WaterKill(byte block) { return Props[block].WaterKills; }

        public static bool LightPass(byte block, byte extBlock, BlockDefinition[] defs) {
            switch (Convert(block)) {
                case air:
                case glass:
                case leaf:
                case redflower:
                case yellowflower:
                case mushroom:
                case redmushroom:
                case shrub:
                case rope:
                    return true;
                case custom_block:
                    BlockDefinition def = defs[extBlock];
                    return def == null ? false : !def.BlocksLight;
                default:
                    return false;
            }
        }

        public static bool NeedRestart(byte block)
        {
            switch (block)
            {
                case train:

                case snake:
                case snaketail:

                case lava_fire:
                case rockethead:
                case firework:

                case creeper:
                case zombiebody:
                case zombiehead:

                case birdblack:
                case birdblue:
                case birdkill:
                case birdlava:
                case birdred:
                case birdwater:
                case birdwhite:

                case fishbetta:
                case fishgold:
                case fishsalmon:
                case fishshark:
                case fishlavashark:
                case fishsponge:

                case tntexplosion:
                    return true;
            }
            return false;
        }

        public static bool portal(byte block) { return Props[block].IsPortal; }
        
        public static bool mb(byte block) { return Props[block].IsMessageBlock; }

        public static bool Physics(byte block)   //returns false if placing block cant actualy cause any physics to happen
        {
            switch (block)
            {
                case rock:
                case stone:
                case blackrock:
                case waterstill:
                case lavastill:
                case goldrock:
                case ironrock:
                case coal:

                case goldsolid:
                case iron:
                case staircasefull:
                case brick:
                case tnt:
                case stonevine:
                case obsidian:

                case op_glass:
                case opsidian:
                case op_brick:
                case op_stone:
                case op_cobblestone:
                case op_air:
                case op_water:

                case door_tree:
                case door_obsidian:
                case door_glass:
                case door_stone:
                case door_leaves:
                case door_sand:
                case door_wood:
                case door_green:
                case door_tnt:
                case door_stair:
                case door_iron:
                case door_gold:
                case door_dirt:
                case door_grass:
                case door_blue:
                case door_book:
                case door_cobblestone:
                case door_red:

                case door_orange:
                case door_yellow:
                case door_lightgreen:
                case door_aquagreen:
                case door_cyan:
                case door_lightblue:
                case door_purple:
                case door_lightpurple:
                case door_pink:
                case door_darkpink:
                case door_darkgrey:
                case door_lightgrey:
                case door_white:

                case tdoor:
                case tdoor2:
                case tdoor3:
                case tdoor4:
                case tdoor5:
                case tdoor6:
                case tdoor7:
                case tdoor8:
                case tdoor9:
                case tdoor10:
                case tdoor11:
                case tdoor12:
                case tdoor13:

                case air_door:
                case air_switch:
                case water_door:
                case lava_door:

                case MsgAir:
                case MsgWater:
                case MsgLava:
                case MsgBlack:
                case MsgWhite:

                case blue_portal:
                case orange_portal:
                case air_portal:
                case water_portal:
                case lava_portal:

                case deathair:
                case deathlava:
                case deathwater:

                case flagbase:
                    return false;

                default:
                    return true;
            }
        }
        
        public static byte DoorAirs(byte block) { return Props[block].DoorAirId; }

        public static bool tDoor(byte block) { return Props[block].IsTDoor; }

        public static byte odoor(byte block) { return Props[block].ODoorId; }
    }
}
