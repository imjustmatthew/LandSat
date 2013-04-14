using System;

namespace LandSat
{
	public class LandSatModuleViewer : DisplayModule
    {
        public LandSatModuleViewer(LandSatCore core)
            : base(core)
        {
            priority = 100;
        }

		//TODO figure out how to draw the GUI :)
		//https://github.com/MuMech/MechJeb2/blob/master/MechJeb2/MechJebModuleSmartASS.cs is a good example
		//This module should handle dispatching map rendering using core.datastore.deepCopy(<params>) rendering 
		// should be done in whatever format will be fastest to display in the gui, which might be direct to a texture
		// this module does not need to try and control mapping itself, eventually LandSatModuleMapper should have it's 
		// own GUI to control mapping and pruning parameters.

		public override string GetName()
        {
            return "LandSat Map Viewer";
        }
	}
}

