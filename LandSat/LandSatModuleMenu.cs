﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LandSat
{
    public class LandSatModuleMenu : DisplayModule
    {
        public LandSatModuleMenu(LandSatCore core)
            : base(core)
        {
            priority = -1000;
            enabled = true;
            hidden = true;
            showInFlight = true;
            showInEditor = true;
        }

        public enum WindowStat
        {
            HIDDEN,
            MINIMIZED,
            NORMAL,
            OPENING,
            CLOSING
        }

        [Persistent(pass = (int)Pass.Global)]
        public WindowStat windowStat = WindowStat.HIDDEN;
        
        [Persistent(pass = (int)Pass.Global)]
        public float windowProgr = 0;

        public bool firstDraw = true;

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            foreach (DisplayModule module in core.GetComputerModules<DisplayModule>().OrderBy(m => m, DisplayOrder.instance))
            {
                if (!module.hidden && module.showInCurrentScene)
                {
                    module.enabled = GUILayout.Toggle(module.enabled, module.GetName());
                }
            }

            if (GUILayout.Button("Online Manual"))
            {
                Application.OpenURL("http://KerbalGA.com/manual");
            }
            GUILayout.EndVertical();
        }

        public override void DrawGUI(bool inEditor)
        {
            switch (windowStat)
            {
                case WindowStat.OPENING:
                    windowProgr += Time.deltaTime;
                    if (windowProgr >= 1)
                    {
                        windowProgr = 1;
                        windowStat = WindowStat.NORMAL;
                    }
                    break;
                case WindowStat.CLOSING:
                    windowProgr -= Time.deltaTime;
                    if (windowProgr <= 0)
                    {
                        windowProgr = 0;
                        windowStat = WindowStat.HIDDEN;
                    }
                    break;
            }

            GUI.depth = -100;
            GUI.SetNextControlName("LandSatOpen");
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(0, 0, -90)), Vector3.one);
            if (GUI.Button(new Rect((-Screen.height/4*3 - 100/2), Screen.width - 25 - (200 * windowProgr), 100, 25), (windowStat == WindowStat.HIDDEN) ? "/\\ LandSat /\\" : "\\/ LandSat \\/"))
            {
                if (windowStat == WindowStat.HIDDEN)
                {
                    windowStat = WindowStat.OPENING;
                    windowProgr = 0;
                    firstDraw = true;
                }
                else if (windowStat == WindowStat.NORMAL)
                {
                    windowStat = WindowStat.CLOSING;
                    windowProgr = 1;
                }
            }
            GUI.matrix = Matrix4x4.identity;

            GUI.depth = -99;

            if (windowStat != WindowStat.HIDDEN)
            {
                windowPos = GUILayout.Window(GetType().FullName.GetHashCode(), new Rect(Screen.width - windowProgr * 200, (Screen.height/4*3 - windowPos.height/2), 200, 20), WindowGUI, "LandSat " + core.version, GUILayout.Width(200), GUILayout.MinHeight(20));
            }

            GUI.depth = -98;

            if (firstDraw)
            {
                GUI.FocusControl("LandSatOpen");
                firstDraw = false;
            }
        }

        class DisplayOrder : IComparer<DisplayModule>
        {
            private DisplayOrder() { }
            public static DisplayOrder instance = new DisplayOrder();

            int IComparer<DisplayModule>.Compare(DisplayModule a, DisplayModule b)
            {
                // These are only used to ensure a module is last.
				//if (a is LandSatModuleCustomInfoWindow && b is LandSatModuleCustomInfoWindow) return a.GetName().CompareTo(b.GetName());
                //if (a is LandSatModuleCustomInfoWindow) return 1;
                //if (b is LandSatModuleCustomInfoWindow) return -1;
                return a.GetName().CompareTo(b.GetName());
            }
        }
    }
}
