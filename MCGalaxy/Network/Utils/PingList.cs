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

namespace MCGalaxy.Network {
    public sealed class PingList {
        
        public struct PingEntry {
            public DateTime TimeSent, TimeReceived;
            public ushort Data;
            public double Latency { get {
                    // Half, because received->reply time is actually twice time it takes to send data
                    return (TimeReceived - TimeSent).TotalMilliseconds * 0.5;
                } }
        }
        
        public PingEntry[] Entries = new PingEntry[10];
        
        
        public ushort NextTwoWayPingData() {
            // Find free ping slot
            for (int i = 0; i < Entries.Length; i++) {
                if (Entries[i].TimeSent.Ticks != 0) continue;
                
                ushort prev = i > 0 ? Entries[i - 1].Data : (ushort)0;
                return SetTwoWayPing(i, prev);
            }
            
            // Remove oldest ping slot
            for (int i = 0; i < Entries.Length - 1; i++) {
                Entries[i] = Entries[i + 1];
            }
            int j = Entries.Length - 1;
            return SetTwoWayPing(j, Entries[j].Data);
        }
        
        ushort SetTwoWayPing(int i, ushort prev) {
            Entries[i].Data = (ushort)(prev + 1);
            Entries[i].TimeSent = DateTime.UtcNow;
            Entries[i].TimeReceived = default(DateTime);
            return (ushort)(prev + 1);
        }
        
        public void Update(ushort data) {
            for (int i = 0; i < Entries.Length; i++ ) {
                if (Entries[i].Data != data) continue;
                Entries[i].TimeReceived = DateTime.UtcNow;
                return;
            }
        }
        
        
        /// <summary> Gets lowest (best) ping in milliseconds, or 0 if no ping measures. </summary>
        public int LowestPingMilliseconds() {
            double totalMs = 100000000;
            foreach (PingEntry ping in Entries) {
                if (ping.TimeSent.Ticks == 0 || ping.TimeReceived.Ticks == 0) continue;
                totalMs = Math.Min(totalMs, ping.Latency);
            }
            return (int)totalMs;
        }
        
        /// <summary> Gets average ping in milliseconds, or 0 if no ping measures. </summary>
        public int AveragePingMilliseconds() {
            double totalMs = 0;
            int measures = 0;
            foreach (PingEntry ping in Entries) {
                if (ping.TimeSent.Ticks == 0 || ping.TimeReceived.Ticks == 0) continue;
                totalMs += ping.Latency; measures++;
            }
            return measures == 0 ? 0 : (int)(totalMs / measures);
        }
        
        /// <summary> Gets worst ping in milliseconds, or 0 if no ping measures. </summary>
        public double HighestPingMilliseconds() {
            double totalMs = 0;
            foreach (PingEntry ping in Entries) {
                if (ping.TimeSent.Ticks == 0 || ping.TimeReceived.Ticks == 0) continue;
                totalMs = Math.Max(totalMs, ping.Latency);
            }
            return (int)totalMs;
        }
        
        public string Format() {
            return string.Format("Lowest ping {0}ms, average {1}ms, highest {2}ms",
                                 LowestPingMilliseconds(),
                                 AveragePingMilliseconds(),
                                 HighestPingMilliseconds());
        }
    }
}