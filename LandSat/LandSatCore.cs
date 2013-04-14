using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using System.Reflection;
using KSP.IO;

namespace LandSat
{
    public class LandSatCore : PartModule, IComparable<LandSatCore>
    {
        private List<ComputerModule> computerModules = new List<ComputerModule>();
        private List<ComputerModule> modulesToLoad = new List<ComputerModule>();
        private bool modulesUpdated = false;

        private static List<Type> moduleRegistry;

        //public LandSatModuleWarpController warp; For future warp-control expansion (e.g. warp until plane tis fully mapped)
		public LandSatModuleMapper mapper; //handles mapping
		public LandSatModuleViewer viewer; //handles display of maps

        public VesselState vesselState = new VesselState();

        private Vessel controlledVessel; //keep track of which vessel we've added our isLandSatTeamLeader callback to

        public string version = "";

        [KSPField(isPersistant = false)]
        public string blacklist = "";

        private bool weLockedEditor = false;

        private float lastSettingsSaveTime;

        private bool showGui = true;

        public static GUISkin skin = null;

        public static RenderingManager renderingManager = null;

		public LandSatDataStore datastore = new LandSatDataStore();

        

        //Returns whether the vessel we've registered OnFlyByWire with is the correct one. 
        //If it isn't the correct one, fixes it before returning false
        bool CheckControlledVessel()
        {
            if (controlledVessel == vessel) return true;

            //else we have an onFlyByWire callback registered with the wrong vessel:
            //handle vessel changes due to docking/undocking
            if (controlledVessel != null) controlledVessel.OnFlyByWire -= OnFlyByWire;
            if (vessel != null)
            {
                vessel.OnFlyByWire -= OnFlyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += OnFlyByWire;
            }
            controlledVessel = vessel;
            return false;
        }

        public int GetImportance()
        {
            if (part.State == PartStates.DEAD)
            {
                return 0;
            }
            else
            {
                return GetInstanceID();
            }
        }

        public int CompareTo(LandSatCore other)
        {
            if (other == null) return 1;
            return GetImportance().CompareTo(other.GetImportance());
        }

        public T GetComputerModule<T>() where T : ComputerModule
        {
            return (T)computerModules.FirstOrDefault(m => m is T); //returns null if no matches
        }

        public List<T> GetComputerModules<T>() where T : ComputerModule
        {
            return computerModules.FindAll(a => a is T).Cast<T>().ToList();
        }

        public ComputerModule GetComputerModule(string type)
        {
            return computerModules.FirstOrDefault(a => a.GetType().Name.ToLowerInvariant() == type.ToLowerInvariant()); //null if none
        }

        public void AddComputerModule(ComputerModule module)
        {
            computerModules.Add(module);
            modulesUpdated = true;
        }

        public void AddComputerModuleLater(ComputerModule module)
        {
            //The actual loading is delayed to FixedUpdate because AddComputerModule can get called inside a foreach loop
            //over the modules, and modifying the list during the loop will cause an exception. Maybe there is a better
            //way to deal with this?
            modulesToLoad.Add(module);
        }

        public void RemoveComputerModule(ComputerModule module)
        {
            computerModules.Remove(module);
            modulesUpdated = true;
        }


        public override void OnStart(PartModule.StartState state)
        {
            if (state == PartModule.StartState.None) return; //don't do anything when we start up in the loading screen

            //OnLoad doesn't get called for parts created in editor, so do that manually so 
            //that we can load global settings.
            //However, if you press ctrl-Z, a new PartModule object gets created, on which the
            //game DOES call OnLoad, and then OnStart. So before calling OnLoad from OnStart,
            //check whether we have loaded any computer modules.
            if (state == StartState.Editor && computerModules.Count == 0) OnLoad(null);

            lastSettingsSaveTime = Time.time;

            foreach (ComputerModule module in computerModules)
            {
                module.OnStart(state);
            }

            if (vessel != null)
            {
                vessel.OnFlyByWire -= OnFlyByWire; //just a safety precaution to avoid duplicates
                vessel.OnFlyByWire += OnFlyByWire;
                controlledVessel = vessel;
            }
        }

        public override void OnActive()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnActive();
            }
        }

        public override void OnInactive()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnInactive();
            }
        }

        public override void OnAwake()
        {
            foreach (ComputerModule module in computerModules)
            {
                module.OnAwake();
            }
        }

		//This is called every physcis frame
        public void FixedUpdate()
        {
            if (modulesToLoad.Count > 0)
            {
                computerModules.AddRange(modulesToLoad);
                modulesUpdated = true;
                modulesToLoad.Clear();
            }

            CheckControlledVessel(); //make sure our onFlyByWire callback is registered with the right vessel

            if (this != vessel.GetMasterLandSat())
            {
                return;
            }

            if (modulesUpdated)
            {
                computerModules.Sort();
                modulesUpdated = false;
            }

            //periodically save settings in case we quit unexpectedly
            if (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel)
            {
                if (Time.time > lastSettingsSaveTime + 30)
                {
                    Debug.Log("LandSat doing periodic settings save");
                    OnSave(null);
                    lastSettingsSaveTime = Time.time;
                }
            }

            if (vessel == null) return; //don't run ComputerModules' OnFixedUpdate in editor

            vesselState.Update(vessel);

            foreach (ComputerModule module in computerModules)
            {
                if (module.enabled) module.OnFixedUpdate();
            }

        }

		//This is called every Unity frame see http://goo.gl/gQjtL
        public void Update()
        {
            //a hack to detect when the user hides the GUI
            if (renderingManager == null)
            {
                renderingManager = (RenderingManager)GameObject.FindObjectOfType(typeof(RenderingManager));
            }
            if (HighLogic.LoadedSceneIsFlight && renderingManager != null)
            {
                showGui = renderingManager.uiElementsToDisable[0].activeSelf;
            }

            if (this != vessel.GetMasterLandSat())
            {
                return;
            }

            if (modulesUpdated)
            {
                computerModules.Sort();
                modulesUpdated = false;
            }

            if (vessel == null) return; //don't run ComputerModules' OnUpdate in editor

            foreach (ComputerModule module in computerModules)
            {
                if (module.enabled) module.OnUpdate();
            }
        }

        void LoadComputerModules()
        {
            if (moduleRegistry == null)
            {
                moduleRegistry = (from ass in AppDomain.CurrentDomain.GetAssemblies() from t in ass.GetTypes() where t.IsSubclassOf(typeof(ComputerModule)) select t).ToList();
            }

            Version v = Assembly.GetAssembly(typeof(LandSatCore)).GetName().Version;
            version = v.Major.ToString() + "." + v.Minor.ToString() + "." + v.Build.ToString();

            foreach (Type t in moduleRegistry)
            {
                if ((t != typeof(ComputerModule)) && (t != typeof(DisplayModule)) && !blacklist.Contains(t.Name))
                {
                    AddComputerModule((ComputerModule)(t.GetConstructor(new Type[] { typeof(LandSatCore) }).Invoke(new object[] { this })));
                }
            }

            //warp = GetComputerModule<LandSatModuleWarpController>(); For future warp-control expansion (e.g. warp until plane tis fully mapped)
			mapper = GetComputerModule<LandSatModuleMapper>();
			viewer = GetComputerModule<LandSatModuleViewer>();
        }

        public override void OnLoad(ConfigNode sfsNode)
        {
            try
            {
                bool generateDefaultWindows = false;

                base.OnLoad(sfsNode); //is this necessary?

				//TODO add any parameters
				datastore.loadFromStorage();

                LoadComputerModules();

                ConfigNode global = new ConfigNode("LandSatGlobalSettings");
                if (File.Exists<LandSatCore>("landsat_settings_global.cfg"))
                {
                    try
                    {
                        global = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "landsat_settings_global.cfg"));
                    }
                    catch (Exception e)
                    {
                        Debug.Log("LandSatCore.OnLoad caught an exception trying to load landsat_settings_global.cfg: " + e);
                    }
                }
                else
                {
                    generateDefaultWindows = true;
                }

                //Todo: load a different file for each vessel type
                ConfigNode type = new ConfigNode("LandSatTypeSettings");
                if (File.Exists<LandSatCore>("landsat_settings_type.cfg", vessel))
                {
                    try
                    {
                        type = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "landsat_settings_type.cfg", vessel));
                    }
                    catch (Exception e)
                    {
                        Debug.Log("LandSatCore.OnLoad caught an exception trying to load landsat_settings_type.cfg: " + e);
                    }
                }

                ConfigNode local = new ConfigNode("LandSatLocalSettings");
                if (sfsNode != null && sfsNode.HasNode("LandSatLocalSettings"))
                {
                    local = sfsNode.GetNode("LandSatLocalSettings");
                }

                /*Debug.Log("OnLoad: loading from");
                Debug.Log("Local:");
                Debug.Log(local.ToString());
                Debug.Log("Type:");
                Debug.Log(type.ToString());
                Debug.Log("Global:");
                Debug.Log(global.ToString());*/

                foreach (ComputerModule module in computerModules)
                {
                    string name = module.GetType().Name;
                    ConfigNode moduleLocal = local.HasNode(name) ? local.GetNode(name) : null;
                    ConfigNode moduleType = type.HasNode(name) ? type.GetNode(name) : null;
                    ConfigNode moduleGlobal = global.HasNode(name) ? global.GetNode(name) : null;
                    module.OnLoad(moduleLocal, moduleType, moduleGlobal);
                }

//                if (generateDefaultWindows)
//                {
//                    GetComputerModule<LandSatModuleCustomWindowEditor>().AddDefaultWindows();
//                }
            }
            catch (Exception e)
            {
                Debug.Log("LandSat caught exception in core OnLoad: " + e);
            }
        }

        public override void OnSave(ConfigNode sfsNode)
        {
            //we have nothing worth saving if we're outside the editor or flight scenes:
            if (!(HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)) return;

            try
            {
                base.OnSave(sfsNode); //is this necessary?

				//TODO add any parameters
				datastore.saveToStorage();

                ConfigNode local = new ConfigNode("LandSatLocalSettings");
                ConfigNode type = new ConfigNode("LandSatTypeSettings");
                ConfigNode global = new ConfigNode("LandSatGlobalSettings");

                foreach (ComputerModule module in computerModules)
                {
                    string name = module.GetType().Name;
                    module.OnSave(local.AddNode(name), type.AddNode(name), global.AddNode(name));
                }

                /*Debug.Log("OnSave:");
                Debug.Log("Local:");
                Debug.Log(local.ToString());
                Debug.Log("Type:");
                Debug.Log(type.ToString());
                Debug.Log("Global:");
                Debug.Log(global.ToString());*/

                if (sfsNode != null) sfsNode.nodes.Add(local);

                type.Save(IOUtils.GetFilePathFor(this.GetType(), "landsat_settings_type.cfg")); //Todo: save a different file for each vessel type.
                global.Save(IOUtils.GetFilePathFor(this.GetType(), "landsat_settings_global.cfg"));
            }
            catch (Exception e)
            {
                Debug.Log("LandSat caught exception in core OnSave: " + e);
            }
        }

        public void OnDestroy()
        {
            if (this == vessel.GetMasterLandSat() && (HighLogic.LoadedSceneIsEditor || vessel.isActiveVessel))
            {
                OnSave(null);
            }

            foreach (ComputerModule module in computerModules)
            {
                module.OnDestroy();
            }
            if (vessel != null)
            {
                vessel.OnFlyByWire -= OnFlyByWire;
            }
            controlledVessel = null;
        }

        private void OnFlyByWire(FlightCtrlState s)
        {
            if (!CheckControlledVessel() || this != vessel.GetMasterLandSat())
            {
                return;
            }

            //Previously called: Drive(s); but we don't need to drive anything
			return;
        }

        private void OnGUI()
        {
            if (!showGui) return;

            GUI.skin = skin;

            if (this == vessel.GetMasterLandSat() &&
                ((HighLogic.LoadedSceneIsEditor) || ((FlightGlobals.ready) && (vessel == FlightGlobals.ActiveVessel) && (part.State != PartStates.DEAD))))
            {
                foreach (DisplayModule module in GetComputerModules<DisplayModule>())
                {
                    if (module.enabled) module.DrawGUI(HighLogic.LoadedSceneIsEditor);
                }

                if (HighLogic.LoadedSceneIsEditor) PreventEditorClickthrough();
            }
        }

        // VAB/SPH description
        public override string GetInfo()
        {
            return "LandSat: Know the Earth, Show the Way, Understand the World.";
        }

        //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
        void PreventEditorClickthrough()
        {
            bool mouseOverWindow = GuiUtils.MouseIsOverWindow(this);
            if (mouseOverWindow && !EditorLogic.editorLocked)
            {
                EditorLogic.fetch.Lock(true, true, true);
                weLockedEditor = true;
            }
            if (weLockedEditor && !mouseOverWindow && EditorLogic.editorLocked)
            {
                EditorLogic.fetch.Unlock();
            }
            if (!EditorLogic.editorLocked) weLockedEditor = false;
        }
    }
}
