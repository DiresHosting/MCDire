﻿/*
	Copyright 2011 MCGalaxy
		
	Dual-licensed under the	Educational Community License, Version 2.0 and
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
using System.IO;
namespace MCGalaxy.Commands {
	public sealed class CmdView : Command {
		
		public override string name { get { return "view"; } }
		public override string shortcut { get { return ""; } }
		public override string type { get { return CommandTypes.Other; } }
		public override bool museumUsable { get { return true; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
		public CmdView() { }

		public override void Use(Player p, string message) {
			if (!Directory.Exists("extra/text/")) 
				Directory.CreateDirectory("extra/text");
			if (message == "") {
				DirectoryInfo di = new DirectoryInfo("extra/text/");
				string allFiles = "";
				foreach (FileInfo fi in di.GetFiles("*.txt"))
					allFiles += ", " + fi.Name;

				if (allFiles == "") {
					Player.SendMessage(p, "No files are viewable by you");
				} else {
					Player.SendMessage(p, "Available files:");
					Player.SendMessage(p, allFiles.Remove(0, 2));
				}
			} else {
				Player who = p;
				string[] args = message.Split(' ');
				if (args.Length > 1) {
					who = PlayerInfo.FindOrShowMatches(p, args[1]);
					if (who == null) return;
				}
				args[0] = Path.GetFileName(args[0]);
				
				if (File.Exists("extra/text/" + args[0] + ".txt")) {
					string[] lines = File.ReadAllLines("extra/text/" + args[0] + ".txt");
					for (int i = 0; i < lines.Length; i++)
						Player.SendMessage(who, lines[i]);
				} else {
					Player.SendMessage(p, "File specified doesn't exist");
				}
			}
		}
		
		public override void Help(Player p) {
			Player.SendMessage(p, "/view [file] [player] - Views [file]'s contents");
			Player.SendMessage(p, "/view by itself will list all files you can view");
			Player.SendMessage(p, "If [player] is given, that player is shown the file");
		}
	}
}