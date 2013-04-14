using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LandSat
{
    public class LandSatModuleMapper : ComputerModule
    {
        public LandSatModuleMapper(LandSatCore core)
            : base(core)
        {
            priority = 100;
        }

		private float lastMeasurementTime;
		private float lastPruneTime;
		private bool pruneInProgress = false;
		private int measurementsSincePrune = 0;
		//TODO instance variable to hold the pruning thread?

		public override void OnStart(PartModule.StartState state)
        {
            lastMeasurementTime = Time.time;
			lastPruneTime = Time.time;
			users.Add(this);
			base.OnStart(state);
        }

        public override void OnFixedUpdate ()
		{
			//data collection
			if (Time.time > lastMeasurementTime + 0.1) 
			{
				lastMeasurementTime = Time.time;
				measurementsSincePrune++;
				takeMeasurement();
			}
			//pruning
			if (!pruneInProgress && (Time.time > lastPruneTime + 600 || measurementsSincePrune > 10000))
            {
                Debug.Log("LandSat doing periodic datastore prune");
				lastPruneTime = Time.time;
				measurementsSincePrune = 0;
                //TODO spawn worker to prune the data store
				Debug.Log("LandSat is starting a prune job.");
            }
		}

		private void takeMeasurement ()
		{
			//take a measurement and add each datum with:
			//core.datastore.storeData(<params>)
			Debug.Log("LandSat is taking a measurement.");
		}
    }
}
