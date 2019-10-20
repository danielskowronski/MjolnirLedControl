using System;
using System.Collections.Generic;
using System.Drawing;
using RGBKit.Core;
using Microsoft.Win32;

namespace MjolnirLedControl
{
    class Program
    {
        static RGBKitService rgbKit;
        static RegistryKey lastColor,customColor;
        struct ColorOption
        {
            public string id;
            public string desc;
            public int r, g, b;
        }

        static List<ColorOption> ColorOptionList = new List<ColorOption>
        {
            new ColorOption{ id="R", desc="Red",     r=255, g=0,   b=0   },
            new ColorOption{ id="G", desc="Green",   r=0,   g=255, b=0   },
            new ColorOption{ id="B", desc="Blue",    r=0,   g=0,   b=255 },

            new ColorOption{ id="C", desc="Cyan",    r=0,   g=255, b=255 },
            new ColorOption{ id="M", desc="Magenta", r=255, g=0,   b=255 },
            new ColorOption{ id="Y", desc="Yellow",  r=255, g=255, b=0   },

            new ColorOption{ id="W", desc="White",   r=255, g=255, b=255 }
        };

        static int WindowWidth  = 40;
        static int WindowHeight = 15;

        static void SetColor(ColorOption co)
        {
            SetColor(co.r, co.g, co.b);
        }
        static void SetColor(int r, int g, int b)
        {
            lastColor.SetValue("r", r);
            lastColor.SetValue("g", g);
            lastColor.SetValue("b", b);
            foreach (var provider in rgbKit.DeviceProviders)
            {
                foreach (var device in provider.Devices)
                {
                    foreach (var light in device.Lights)
                    {
                        light.Color = Color.FromArgb(r, g, b);
                    }
                    device.ApplyLights();
                }
            }
        }

        static ColorOption GetColorOptionFromRegKey(RegistryKey rk)
        {
            ColorOption co = new ColorOption();

            if (lastColor.GetValue("r") != null && lastColor.GetValue("g") != null && lastColor.GetValue("b") != null)
            {
                co.r = (int)(rk.GetValue("r"));
                co.g = (int)(rk.GetValue("g"));
                co.b = (int)(rk.GetValue("b"));
            }

            return co;
        }

        static void PrintBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;   Console.Write("MJOLNIR");
            Console.ForegroundColor = ConsoleColor.Green; Console.Write("LED");
            Console.ForegroundColor = ConsoleColor.Blue;  Console.Write("CONTROL");
            Console.ForegroundColor = ConsoleColor.White; Console.WriteLine("");
        }

        static void Main(string[] args)
        {
            Console.SetWindowSize(WindowWidth, WindowHeight);
            Console.BufferWidth = WindowWidth;
            Console.BufferHeight = WindowHeight;
            PrintBanner();

            lastColor   = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\lastColor");
            customColor = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\customColor");

            rgbKit = new RGBKitService();
            rgbKit.UseAura();
            rgbKit.UseCue();

            string[] procList = { "atkexComSvc", "LightingService", "iCUE" };
            foreach (string proc in procList)
            {
                if (System.Diagnostics.Process.GetProcessesByName(proc).Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red; 
                    Console.WriteLine(proc+".exe is not running - you need to start (or even install) it first!");
                }
            }
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("initializing rgbKit, this may take some time...");
            rgbKit.Initialize();

            ColorOption lastColorOption = GetColorOptionFromRegKey(lastColor);
            SetColor(lastColorOption);

            while (true)
            {
                PrintBanner();

                foreach (ColorOption co in ColorOptionList)
                {
                    Console.WriteLine("[" + co.id + "] " + co.desc);
                }
                Console.WriteLine();
                Console.WriteLine("[0] Turn off");
                Console.WriteLine();
                Console.WriteLine("[S] Set custom colors (RGB)");
                Console.WriteLine("[1] Use custom color");

                char command = Console.ReadKey().KeyChar;

                if (command == '0')
                {
                    SetColor(0, 0, 0);
                }
                else if (command == '1')
                {
                    ColorOption customColorOption = GetColorOptionFromRegKey(customColor);
                    SetColor(customColorOption);
                }
                else if (command.ToString().ToUpper()[0] == 'S')
                {
                    PrintBanner();

                    string[] colors = { "R", "G", "B" };
                    Console.WriteLine("Set below to numbers in range [0-255]");

                    foreach (string c in colors)
                    {
                        while (true)
                        {
                            Console.Write("Custom color "+c+": ");
                            if (Int32.TryParse(Console.ReadLine(), out int v) && v>=0 && v<=255)
                            {
                                customColor.SetValue(c, v);
                                break;
                            }
                        }
                    }

                    ColorOption customColorOption = GetColorOptionFromRegKey(customColor);
                    SetColor(customColorOption);

                    continue;
                }
                else
                {
                    foreach (ColorOption co in ColorOptionList)
                    {
                        if (command.ToString().ToUpper()[0] == co.id.ToUpper()[0])
                        {
                            SetColor(co);
                            break;
                        }
                    }
                }
            } // end main input
        }
    }
}
