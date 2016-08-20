﻿/*
    Copyright 2015 MCGalaxy
    
    Dual-licensed under the Educational Community License, Version 2.0 and
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
using System.Collections.Generic;
using System.IO;

namespace MCGalaxy.Undo {

    /// <summary> Retrieves and saves undo data in a particular format. </summary>
    /// <remarks> Note most formats only support retrieving undo data. </remarks>
    public abstract class UndoFormat {
        
        protected const string undoDir = "extra/undo", prevUndoDir = "extra/undoPrevious";
        public static UndoFormat TxtFormat = new UndoFormatText();
        public static UndoFormat BinFormat = new UndoFormatBin();
        public static UndoFormat NewFormat = new UndoFormatCBin();
        
        protected abstract void Save(List<Player.UndoPos> buffer, string path);
        
        protected abstract void Save(UndoCache buffer, string path);
        
        protected abstract IEnumerable<Player.UndoPos> GetEntries(Stream s, UndoEntriesArgs args);
        
        protected abstract string Ext { get; }
        
        public static void SaveUndo(Player p) {
            if (p == null || p.UndoBuffer.Count < 1) return;
            
            CreateDefaultDirectories();
            if (Directory.GetDirectories(undoDir).Length >= Server.totalUndo) {
                Directory.Delete(prevUndoDir, true);
                Directory.Move(undoDir, prevUndoDir);
                Directory.CreateDirectory(undoDir);
            }

            string playerDir = Path.Combine(undoDir, p.name.ToLower());
            if (!Directory.Exists(playerDir))
                Directory.CreateDirectory(playerDir);
            
            int numFiles = Directory.GetFiles(playerDir).Length;
            string path = Path.Combine(playerDir, numFiles + NewFormat.Ext);
            
            UndoCache cache = p.UndoBuffer;
            using (IDisposable locker = cache.ClearLock.AccquireReadLock()) {
                NewFormat.Save(cache, path);
            }

            using (IDisposable locker = cache.ClearLock.AccquireWriteLock()) {
                lock (cache.AddLock)
                    cache.Clear();
            }
        }
        
        public static void UndoPlayer(Player p, string target, Vec3S32[] marks, DateTime start, ref bool FoundUser) {
            FilterEntries(p, undoDir, target, marks, start, false, ref FoundUser);
            FilterEntries(p, prevUndoDir, target, marks, start, false, ref FoundUser);
        }
        
        public static void HighlightPlayer(Player p, string target, DateTime start, ref bool FoundUser) {
            FilterEntries(p, undoDir, target, null, start, true, ref FoundUser);
            FilterEntries(p, prevUndoDir, target, null, start, true, ref FoundUser);
        }
        
        static void FilterEntries(Player p, string dir, string name, Vec3S32[] marks,
                                  DateTime start, bool highlight, ref bool FoundUser) {
            string[] files = GetFiles(dir, name);
            if (files == null || files.Length == 0) return;
            UndoEntriesArgs args = new UndoEntriesArgs(p, start);
            
            for (int i = files.Length - 1; i >= 0; i--) {
                string path = files[i];
                if (path == null) continue;
                UndoFormat format = GetFormat(path);
                if (format == null) continue;
                
                using (Stream s = File.OpenRead(path)) {
                    if (highlight) {
                        DoHighlight(s, format, args);
                    } else {
                        DoUndo(s, format, args);
                    }
                    if (args.Stop) break;
                }
            }
            FoundUser = true;
        }
        
        static string[] GetFiles(string dir, string name) {
            string path = Path.Combine(dir, name);
            if (!Directory.Exists(path)) return null;
            
            string[] files = Directory.GetFiles(path);
            Array.Sort<string>(files, CompareFiles);
            
            for (int i = 0; i < files.Length; i++) {
                name = Path.GetFileName(files[i]);
                if (name.Length == 0 || name[0] < '0' || name[0] > '9')
                    files[i] = null;
            }
            return files;
        }
        
        static UndoFormat GetFormat(string file) {
            if (file.EndsWith(TxtFormat.Ext)) return TxtFormat;
            if (file.EndsWith(BinFormat.Ext)) return BinFormat;
            if (file.EndsWith(NewFormat.Ext)) return NewFormat;
            return null;
        }
        
        static int CompareFiles(string a, string b) {
            a = Path.GetFileNameWithoutExtension(a);
            b = Path.GetFileNameWithoutExtension(b);
            
            int aNum, bNum;
            if (!int.TryParse(a, out aNum) || !int.TryParse(b, out bNum))
                return a.CompareTo(b);
            return aNum.CompareTo(bNum);
        }
        
        public static void DoHighlight(Stream s, UndoFormat format, UndoEntriesArgs args) {
            BufferedBlockSender buffer = new BufferedBlockSender(args.Player);
            Level lvl = args.Player.level;
            
            foreach (Player.UndoPos P in format.GetEntries(s, args)) {
                byte type = P.type, newType = P.newtype;
                byte block = (newType == Block.air
                              || Block.Convert(type) == Block.water || type == Block.waterstill
                              || Block.Convert(type) == Block.lava || type == Block.lavastill)
                    ? Block.red : Block.green;
                
                buffer.Add(lvl.PosToInt(P.x, P.y, P.z), block, 0);
                buffer.CheckIfSend(false);
            }
            buffer.CheckIfSend(true);
        }
        
        public static void DoUndo(Stream s, UndoFormat format, UndoEntriesArgs args) {
            Level lvl = args.Player == null ? null : args.Player.level;
            BufferedBlockSender buffer = new BufferedBlockSender(lvl);
            string lastMap = null;
            
            foreach (Player.UndoPos P in format.GetEntries(s, args)) {
                if (P.mapName != lastMap) {
                    lvl = LevelInfo.FindExact(P.mapName);
                    buffer.CheckIfSend(true);
                    buffer.level = lvl;
                }
                
                if (lvl == null) continue;
                UndoBlock(args.Player, lvl, P, buffer);
            }
            buffer.CheckIfSend(true);
        }
        
        protected internal static void UndoBlock(Player pl, Level lvl, Player.UndoPos P,
                                                 BufferedBlockSender buffer) {
            byte lvlTile = lvl.GetTile(P.x, P.y, P.z);
            if (lvlTile == P.newtype || Block.Convert(lvlTile) == Block.water
                || Block.Convert(lvlTile) == Block.lava || lvlTile == Block.grass) {
                
                byte newExtType = P.newExtType;
                P.newtype = P.type; P.newExtType = P.extType;
                P.extType = newExtType; P.type = lvlTile;
                
                if (pl != null) {
                    if (lvl.DoBlockchange(pl, P.x, P.y, P.z, P.newtype, P.newExtType, true)) {
                        buffer.Add(lvl.PosToInt(P.x, P.y, P.z), P.newtype, P.newExtType);
                        buffer.CheckIfSend(false);
                    }
                } else {
                    bool diffBlock = Block.Convert(lvlTile) != Block.Convert(P.newtype);
                    if (!diffBlock && lvlTile == Block.custom_block)
                        diffBlock = lvl.GetExtTile(P.x, P.y, P.z) != P.newExtType;
                    
                    if (diffBlock) {
                        buffer.Add(lvl.PosToInt(P.x, P.y, P.z), P.newtype, P.newExtType);
                        buffer.CheckIfSend(false);
                    }
                    lvl.SetTile(P.x, P.y, P.z, P.newtype);
                    if (P.newtype == Block.custom_block)
                        lvl.SetExtTile(P.x, P.y, P.z, P.newExtType);
                }
            }
        }
        
        
        public static void CreateDefaultDirectories() {
            if (!Directory.Exists(undoDir))
                Directory.CreateDirectory(undoDir);
            if (!Directory.Exists(prevUndoDir))
                Directory.CreateDirectory(prevUndoDir);
        }
        
        public static void UpgradePlayerUndoFiles(string name) {
            UpgradeFiles(undoDir, name);
            UpgradeFiles(prevUndoDir, name);
        }
        
        static void UpgradeFiles(string dir, string name) {
            string path = Path.Combine(dir, name);
            if (!Directory.Exists(path)) return;
            string[] files = Directory.GetFiles(path);
            List<Player.UndoPos> buffer = new List<Player.UndoPos>();
            UndoEntriesArgs args = new UndoEntriesArgs(null, DateTime.MinValue);
            
            for (int i = 0; i < files.Length; i++) {
                path = files[i];
                if (!path.EndsWith(BinFormat.Ext) && !path.EndsWith(TxtFormat.Ext)) continue;
                
                IEnumerable<Player.UndoPos> data = null;
                using (FileStream s = File.OpenRead(path)) {
                    data = path.EndsWith(BinFormat.Ext)
                        ? BinFormat.GetEntries(s, args) : TxtFormat.GetEntries(s, args);
                    
                    foreach (Player.UndoPos pos in data)
                        buffer.Add(pos);
                    buffer.Reverse();
                    string newPath = Path.ChangeExtension(path, NewFormat.Ext);
                    NewFormat.Save(buffer, newPath);
                }
                File.Delete(path);
            }
        }
    }
    
    public class UndoEntriesArgs {
        public Player Player;
        public byte[] Temp;
        public bool Stop;
        public DateTime StartRange;
        
        public UndoEntriesArgs(Player p, DateTime start) {
            Player = p;
            StartRange = start;
        }
    }
}