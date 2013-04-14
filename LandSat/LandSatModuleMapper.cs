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
			if (Time.time > lastMeasurementTime + 1) //TODO: build with 0.1 for real?
			{
				lastMeasurementTime = Time.time;
				measurementsSincePrune++;
				takeMeasurement();
			}
			//pruning
			if (!pruneInProgress && (Time.time > lastPruneTime + 6 || measurementsSincePrune > 100)) //TODO: build with 600 and 10000 for real?
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

			//Debug.Log("LandSat is taking a measurement.");
			//ISA_Mapsat didn't get the altitude of the sun, I wonder why?

			double altitude = vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass());

			//
			// This Code from ISA_LandSat by Innsewerants
			// NO LICENSE SPECIFIED -- USE AT YOUR OWN RISK
			// This code will be replaced in the future
			//
			double line = -20000d;
			Vector3 pos = vessel.findLocalCenterOfMass();
			double alt = vessel.mainBody.GetAltitude(pos);
			double radius = vessel.mainBody.Radius;
			double latSpacing = (2d*Math.PI*radius)/360d;
			
			double lat = vessel.mainBody.GetLatitude(pos);
			double lon = vessel.mainBody.GetLongitude(pos);
			double artScanAreaLon1;
			double artScanAreaLon2;
			double artScanAreaLat1;
			double artScanAreaLat2;

			while (line <= 20000d)
			{
				double row = -20000d;
				while (row <= 20000d)
				{
					double lat2 = 0d;
					double lon2 = 0d;
					if(Math.Ceiling(line/10000d) == (line/10000d) && Math.Floor(line/10000d) == (line/10000d))
					{
						lat2 = lat+((line*alt/120000d)/latSpacing);
						lon2 = lon+((row*alt/120000d)/((2d*Math.PI*radius/360d)*Math.Cos(lat2*Math.PI/180d)));
					}
					else 
					{
						if(row < 0)
						{
							lat2 = lat+((line*alt/120000d)/latSpacing);
							lon2 = lon+(((row+2500)*alt/120000d)/((2d*Math.PI*radius/360d)*Math.Cos(lat2*Math.PI/180d)));
						}
						else 
						{ 
							lat2 = lat+((line*alt/120000d)/latSpacing);
							lon2 = lon+(((row-2500)*alt/120000d)/((2d*Math.PI*radius/360d)*Math.Cos(lat2*Math.PI/180d)));
						} 
					}
					double[] coordFixed = coordFix(lon2,lat2);
					//The next 4 lines were added by imjustmatthew
					double[] coordNormalized = normalizeLatLong(lat2,lon2);
					if ((coordFixed[1]-coordNormalized[0] > 0.0001d) || (coordFixed[0]-coordNormalized[1] > 0.0001d))
					{
						Debug.Log("LandSat DISAGREES!!! original:"+Math.Round(lat2,6)+","+Math.Round(lon2,6)+" coordFixed:"+Math.Round(coordFixed[1],6)+","+Math.Round(coordFixed[0],6)+" coordNormalized:"+Math.Round(coordNormalized[0],6)+","+Math.Round(coordNormalized[1],6));
					}

					Vector3d rad = QuaternionD.AngleAxis(coordFixed[0], Vector3d.down)*QuaternionD.AngleAxis(coordFixed[1], Vector3d.forward)*Vector3d.right;
					double elev = vessel.mainBody.pqsController.GetSurfaceHeight(rad) - radius;
					//The 2 lines were added by imjustmatthew:
					//Debug.Log("LandSat measured:"+elev);
					core.datastore.storeData(vessel.mainBody,coordNormalized[0],coordNormalized[1],elev);

					//imjustmatthew: These record the maximu extents of the scan rectangle so that the ISA code can later look for artifacts.
					if(line == 20000d && row == -20000)
					{
						artScanAreaLat1 = coordFixed[1];
						artScanAreaLon1 = coordFixed[0];
					}
					if(line == -20000d && row == 20000)
					{
						artScanAreaLat2 = coordFixed[1];
						artScanAreaLon2 = coordFixed[0];
					}
					row = row+5000d;
				}
				line = line+5000d;
			}


			//
			// End code from ISA_LandSat by Innsewerants 
			//

		}

		//
		// This Code from ISA_LandSat by Innsewerants
		// NO LICENSE SPECIFIED -- USE AT YOUR OWN RISK
		// This code will be replaced in the future
		//
		private static double[] coordFix(double lon, double lat)
		{	
			double lonFixed = lon;
			double latFixed = lat;
			int latSwitched = 0;
			while (lonFixed > 180d) { lonFixed = lonFixed - 360d; }
			while (lonFixed < -180d) { lonFixed = lonFixed + 360d; }
			while(latFixed > 90d) { latFixed = Math.Sqrt(Math.Pow ((latFixed-180d),2)); latSwitched = 1;}
			while(latFixed < -90d) { latFixed = Math.Sqrt (Math.Pow (latFixed,2))-180d; latSwitched = 1;}
			if(latSwitched == 1)
			{
				if (lonFixed>0d)
				{
					lonFixed = lonFixed-180d;
				}
				if (lonFixed<0d)
				{
					lonFixed = lonFixed+180d;
				}
			}
			System.Collections.ArrayList list = new System.Collections.ArrayList();
			list.Add(lonFixed);
			list.Add(latFixed);
			double[] lonlat = (double[]) list.ToArray(typeof(double));
			return lonlat;
		}
		//
		// End code from ISA_LandSat by Innsewerants 
		//
		
		// In KSP latitude and longitude are not always in the normal [-90,+90], [-180,+180] interval we expect. 
		//This code puts the longitude into the more familiar interval... and assumes latitude is correct.
		private static double[] normalizeLatLong (double latitude, double longitude)
		{
			double[] ret = new double[2];
			ret[0] = latitude;
			ret[1] = LandSat.MuUtils.ClampDegrees180(longitude);
			return ret;
		}
    }
}
