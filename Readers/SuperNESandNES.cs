﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetroSpy.Readers
{
    static public class SuperNESandNES
    {
        static ControllerState ReadPacketButtons(byte[] packet, string[] buttons)
        {
            if (packet.Length < buttons.Length) return null;

            var state = new ControllerStateBuilder();

            for (int i = 0; i < buttons.Length; ++i)
            {
                if (string.IsNullOrEmpty(buttons[i])) continue;
                state.SetButton(buttons[i], packet[i] != 0x00);
            }

            return state.Build();
        }


        static ControllerState ReadPacketButtons_ascii(byte[] packet, string[] buttons)
        {
            if (packet.Length < buttons.Length) return null;

            var state = new ControllerStateBuilder();

            for (int i = 0; i < buttons.Length; ++i)
            {
                if (string.IsNullOrEmpty(buttons[i])) continue;
                state.SetButton(buttons[i], packet[i] != '0');
            }

            return state.Build();
        }

        static readonly string[] BUTTONS_NES = {
            "a", "b", "select", "start", "up", "down", "left", "right", "2", "1", "5", "9", "6", "10", "11", "7", "4", "3", "12", "8", null, null, null, null
        };

        static readonly string[] BUTTONS_NES_BACKCOMPAT = {
            "a", "b", "select", "start", "up", "down", "left", "right"
        };

        static readonly string[] BUTTONS_SNES = {
            "b", "y", "select", "start", "up", "down", "left", "right", "a", "x", "l", "r", null, null, null, null
        };

        static readonly string[] BUTTONS_INTELLIVISION = {
            "n", "nne", "ne", "ene", "e", "ese", "se", "sse", "s", "ssw", "sw", "wsw", "w", "wnw", "nw", "nnw", "1", "2", "3", "4", "5", "6", "7", "8", "9", "clear", "0", "enter", "topleft", "topright", "bottomleft", "bottomright"
        };

        static readonly string[] BUTTONS_PSCLASSIC =
        {
            "r1", "l1", "r2", "l2", "square", "x", "circle", "triangle", null, null, "down", "up", "right", "left", "start", "select"
        };

        static readonly string[] BUTTONS_ATARI5200 =
        {
            "start", "pause", "reset", "1", "2", "3", "4", "5", "6", "7", "8", "9", "star", "0", "pound", "trigger", "fire", null, null
        };

        static readonly string[] BUTTONS_FMTOWNS =
        {
            "up", "down", "left", "right", null, "a", "b", null, null, "select", "run"
        };

        static readonly string[] BUTTONS_PCFX =
        {
            null, "1", "2", "3", "4", "5", "6", "select", "run", "up", "right", "down", "left", "mode1", null, "mode2"
        };

        static public ControllerState ReadFromPacket_Intellivision(byte[] packet)
        {
            return ReadPacketButtons(packet, BUTTONS_INTELLIVISION);
        }

        static public ControllerState ReadFromPacket_NES (byte[] packet) {
            return ReadPacketButtons(packet, packet.Length == 8 ? BUTTONS_NES_BACKCOMPAT : BUTTONS_NES);
        }

        static public ControllerState ReadFromPacket_PCFX(byte[] packet)
        {
            if (packet.Length != BUTTONS_PCFX.Length) return null;
            return ReadPacketButtons(packet, BUTTONS_PCFX);
        }

        static public ControllerState ReadFromPacket_PSClassic(byte[] packet)
        {
            return ReadPacketButtons_ascii(packet, BUTTONS_PSCLASSIC);
        }

        static public ControllerState ReadFromPacket_SNES (byte[] packet) {
            if (packet.Length < BUTTONS_SNES.Length) return null;

            var state = new ControllerStateBuilder();

            for (int i = 0; i < BUTTONS_SNES.Length; ++i)
            {
                if (string.IsNullOrEmpty(BUTTONS_SNES[i])) continue;
                state.SetButton(BUTTONS_SNES[i], packet[i] != 0x00);
            }

            if (state != null && packet.Length == 32 && packet[15] != 0x00)
            {
                float y = (float)(SignalTool.ReadByte(packet, 17, 7, 0x1) * ((packet[16] & 0x1) != 0 ? 1 : -1)) / 127;
                float x = (float)(SignalTool.ReadByte(packet, 25, 7, 0x1) * ((packet[24] & 0x1) != 0 ? -1 : 1)) / 127;
                SignalTool.SetMouseProperties(x, y, state);

            }

            return state.Build();
        }

        static public ControllerState ReadFromPacket_Jaguar(byte[] packet)
        {
            if (packet.Length < 4) return null;

            var state = new ControllerStateBuilder();

            state.SetButton("pause", (packet[0] & 0b00000100) == 0x00);
            state.SetButton("a", (packet[0] & 0b00001000) == 0x00);
            state.SetButton("right", (packet[0] & 0b00010000) == 0x00);
            state.SetButton("left", (packet[0] & 0b00100000) == 0x00);
            state.SetButton("down", (packet[0] & 0b01000000) == 0x00);
            state.SetButton("up", (packet[0] & 0b10000000) == 0x00);

            state.SetButton("b", (packet[1] & 0b00001000) == 0x00);
            state.SetButton("1", (packet[1] & 0b00010000) == 0x00);
            state.SetButton("4", (packet[1] & 0b00100000) == 0x00);
            state.SetButton("l", (packet[1] & 0b00100000) == 0x00);
            state.SetButton("7", (packet[1] & 0b01000000) == 0x00);
            state.SetButton("x", (packet[1] & 0b01000000) == 0x00);
            state.SetButton("star", (packet[1] & 0b10000000) == 0x00);

            state.SetButton("c", (packet[2] & 0b00001000) == 0x00);
            state.SetButton("2", (packet[2] & 0b00010000) == 0x00);
            state.SetButton("5", (packet[2] & 0b00100000) == 0x00);
            state.SetButton("8", (packet[2] & 0b01000000) == 0x00);
            state.SetButton("y", (packet[2] & 0b01000000) == 0x00);
            state.SetButton("0", (packet[2] & 0b10000000) == 0x00);

            state.SetButton("option", (packet[3] & 0b00001000) == 0x00);
            state.SetButton("3", (packet[3] & 0b00010000) == 0x00);
            state.SetButton("6", (packet[3] & 0b00100000) == 0x00);
            state.SetButton("r", (packet[3] & 0b00100000) == 0x00);
            state.SetButton("9", (packet[3] & 0b01000000) == 0x00);
            state.SetButton("z", (packet[3] & 0b01000000) == 0x00);
            state.SetButton("pound", (packet[3] & 0b10000000) == 0x00);

            return state.Build();

        }

        static readonly string[] SCANCODES_FMTOWNS =
        {
            null, "ESC", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "^", "yen", "Backspace",
            "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "@", "[", "Enter", "A", "S",
            "D", "F", "G", "H", "J", "K", "L", ";", ":", "]", "Z", "X", "C", "V", "B", "N",
            "M", ",", ".", "/", "quote", "Spacebar", "*", "divide", "+", "subtract", "Num7", "Num8", "Num9", "=", "Num4", "Num5",
            "Num6", null, "Num1", "Num2", "Num3", "NumEnter", "Num0", "Num.", "DUP", null, "000", "EL", null, "Up", "Home", "Left",
            "Down", "Right", "CTRL", "SHIFT", null, "CAP", "BottomLeft", "BelowSpace1", "BelowSpace2", "RightOfSpacebar1",
                             "RightOfSpacebar2", "PF12", "ALT", "PF1", "PF2", "PF3",
            "PF4", "PF5", "PF6", "PF7", "PF8", "PF9", "PF10", null, null, "PF11", null, "UpperLeftOfHome", "AboveHome", "UpperRightOfHome", "LeftOfHome", null,
            "RightOfHome", "LowerLeftOfHome", "RightOfSpacebar3", "BelowArrows", "PF13", "PF14", "PF15", "PF16", "PF17", "PF18", "PF19", "PF20", "Pause",
                             "Copy", null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, "SYSREQ", null, null,
            "ScrollLock", "SysHome", "End", null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, "EXT1", null, null, null, "EXT2", null, null, null, null, null, null, null
        };

        static public ControllerState ReadFromPacket_FMTowns(byte[] packet)
        {
            if (packet.Length != 9 && packet.Length != 70) return null;

            if (packet.Length == 9)
            {
                byte[] polishedPacket = new byte[BUTTONS_FMTOWNS.Length];

                if (packet[0] != 0 && packet[1] != 0)
                {
                    packet[0] = packet[1] = 0;
                    polishedPacket[9] = 1;
                }

                if (packet[2] != 0 && packet[3] != 0)
                {
                    packet[2] = packet[3] = 0;
                    polishedPacket[10] = 1;
                }

                for (int i = 0; i < packet.Length; ++i)
                {
                    polishedPacket[i] = packet[i];
                }

                return ReadPacketButtons(polishedPacket, BUTTONS_FMTOWNS);
            }
            else
            {
                int j = 0;
                var reconstructedPacket = new byte[34];
                for (int i = 0; i < 34; ++i)
                {
                    reconstructedPacket[i] = (byte)((packet[j] >> 4) | packet[j + 1]);
                    j += 2;
                }

                byte[] polishedPacket = new byte[256];

                for (int i = 0; i < 32; ++i)
                {
                    for (int k = 0; k < 8; ++k)
                    {
                        polishedPacket[(i * 8) + k] = (byte)((reconstructedPacket[i] & (1 << k)) != 0 ? 1 : 0);
                    }
                }

                var state = new ControllerStateBuilder();

                for (int i = 0; i < SCANCODES_FMTOWNS.Length; ++i)
                {
                    if (string.IsNullOrEmpty(SCANCODES_FMTOWNS[i])) continue;
                    state.SetButton(SCANCODES_FMTOWNS[i], polishedPacket[i] != 0x00);
                }

                SignalTool.SetMouseProperties(((sbyte)reconstructedPacket[33]) / -128.0f, ((sbyte)reconstructedPacket[32]) / 128.0f, state);

                state.SetButton("left", packet[68] == 1);
                state.SetButton("right", packet[69] == 1);


                return state.Build();

            }
        }

        static float atari5200_y;

        static public ControllerState ReadFromPacket_Atari5200_2(byte[] packet)
        {
            if (packet.Length != BUTTONS_ATARI5200.Length) return null;
            atari5200_y = (((packet[17] >> 4) | (packet[18])) - 128.0f) / 128.0f;

            return null;
        }

        static public ControllerState ReadFromPacket_Atari5200_1(byte[] packet)
        {
            if (packet.Length != BUTTONS_ATARI5200.Length) return null;

            var state = new ControllerStateBuilder();
            for (int i = 0; i < BUTTONS_ATARI5200.Length; ++i)
            {
                if (string.IsNullOrEmpty(BUTTONS_ATARI5200[i])) continue;
                state.SetButton(BUTTONS_ATARI5200[i], packet[i] == 0x00);
            }

            state.SetAnalog("x", (((packet[17] >> 4) | (packet[18])) - 128.0f) / -128.0f);
            state.SetAnalog("y", atari5200_y);

            return state.Build();
        }
    }
}