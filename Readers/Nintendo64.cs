﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetroSpy.Readers
{
    static public class Nintendo64
    {
        const int PACKET_SIZE = 32;

        static readonly string[] BUTTONS = {
            "a", "b", "z", "start", "up", "down", "left", "right", null, null, "l", "r", "cup", "cdown", "cleft", "cright"
        };

        static float ReadStick (byte input) {
            return (float)((sbyte)input) / 128;
        }

        static public ControllerState ReadFromPacket (byte[] packet)
        {
            if (packet.Length != PACKET_SIZE) return null;

            var state = new ControllerStateBuilder ();

            for (int i = 0 ; i < BUTTONS.Length ; ++i) {
                if (string.IsNullOrEmpty (BUTTONS [i])) continue;
                state.SetButton (BUTTONS[i], packet[i] != 0x00);
            }

            float x = ReadStick(SignalTool.ReadByte(packet, BUTTONS.Length    ));
            float y = ReadStick(SignalTool.ReadByte(packet, BUTTONS.Length + 8));
            state.SetAnalog ("stick_x", x);
            state.SetAnalog ("stick_y", y);

            SignalTool.SetMouseProperties(x, y, state);

            return state.Build ();
        }
    }
}
