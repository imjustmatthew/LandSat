using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; //TODO remove before using threads!!!

namespace LandSat
{
	public class LandSatDataStore
	{
		public LandSatDataStore ()
		{
		}

		private bool isReadOnly = false;
		private bool isFrozen = false;

		public class Datum
		{
			public Datum (double latitude, double longitude)
			{
				this.latitude = latitude;
				this.longitude = longitude;
			}

			public Datum (double latitude, double longitude, double elevation)
			{
				this.latitude = latitude;
				this.longitude = longitude;
				this.elevation = elevation;
			}

			private double latitude;
			private double longitude;
			private double elevation;

			//returns true elevation, may be negative if below sea level.
			public double getElevation ()
			{
				return elevation;
			}

			//returns sea-level as zero, removing negative data points.
			public double getElevationNoSea ()
			{
				if (elevation < 0) return 0;
				return elevation;
			}

			public double[] getLatLong ()
			{
				//double[] ret = new double[2];
				//ret[0] = latitude;
				//ret[1] = longitude;
				//This is faster if it actually works...
				double[] ret = {latitude, longitude};
				return ret;
			}

			public double getLatitude ()
			{
				return latitude;
			}

			public double getLongitude ()
			{
				return longitude;
			}

			//returns the radius-independant surface distance between two points, useful to find nearest neighbors.
			// this uses the ‘Haversine’ formula from http://goo.gl/IeQUv
			// multiply this by the radius to get eh actual surface distance between the point.
			public double getDistance (Datum otherPoint)
			{
				double dLat = degToRad(otherPoint.getLatitude() - this.getLatitude());
				double dLong = degToRad(otherPoint.getLongitude() - this.getLongitude());
				double inner = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
					Math.Cos(degToRad(this.getLatitude())) * Math.Cos(degToRad(otherPoint.getLatitude())) *
					Math.Sin(dLong/2) * Math.Sin(dLong/2);
				double outer = 2 * Math.Atan2(Math.Sqrt(inner), Math.Sqrt(1-inner));
				return outer;
			}

			private static double degToRad(double angle) {
				return (angle * (Math.PI/180d));
			}

		}

		//A wrapper for datums that also stores their celestialbody's name
		public class TargetedDatum 
		{
			public TargetedDatum (String target, Datum datum)
			{
				this.target = target;
				this.datum = datum;
			}
			private String target;
			private Datum datum;
			public String getTarget ()
			{
				return target;
			}
			public Datum getDatum ()
			{
				return datum;
			}
		}

		//these are sorted dictionaires of the datums collected. 
		// The outer dictionary is by celestialbody name, 
		// the inner dictionaries aren by latitude and longitude respectively.
		// this optimizes for retrieval by body --> lat --> long
		// retrieval by body --> long --> lat should be avoided
		private SortedDictionary<String, SortedDictionary<double, SortedDictionary<double, Datum>>> data 
			= new SortedDictionary<String, SortedDictionary<double, SortedDictionary<double, Datum>>>();

		//This is used to store collected datums while we are frozen.
		private LinkedList<TargetedDatum> frozenStorage = new LinkedList<TargetedDatum>();

		//Load data from persistant storage, could take argument(s) to do so
		public void loadFromStorage ()
		{
			//check that we're not read-only
			if (isReadOnly) return; //really should throw an exception

			//other stuff
		}

		//Save data to persistant storage, could take argument(s) to do so
		public void saveToStorage ()
		{
		}

		public void storeData (CelestialBody target, double latitude, double longitude, double surfaceAltitude)
		{
			storeData(target.GetName(),new Datum(latitude, longitude, surfaceAltitude));
		}

		private SortedDictionary<double, SortedDictionary<double, Datum>> lastBodyDataCache;
		private String lastBodyDataName = "";

		//Adds a data point to the store, this method should be very fast
		// we could try to optimize it by providing body and latitude caches with early-out (Added body early out for testing)
		public void storeData (String target, Datum datum)
		{
			if (isReadOnly) return; //really should throw an exception

			if (isFrozen) {
				//Debug.Log("LandSat feeezing the data for later..."); //TODO remove before using threads!!!
				//store the data in frozenStorage
				frozenStorage.AddLast(new TargetedDatum(target,datum));
			} else {
				//store the data directly:
				//get the body's dataset
				SortedDictionary<double, SortedDictionary<double, Datum>> bodyData;
				if (lastBodyDataName.Equals(target)) 
			    {
					bodyData = lastBodyDataCache;
					//Debug.Log("LandSat using cached body."); //TODO remove before using threads!!!
				} else {
					if (!data.TryGetValue(target, out bodyData))
					{
						bodyData = new SortedDictionary<double, SortedDictionary<double, Datum>>();
						data.Add(target,bodyData);
						//Debug.Log("LandSat added new body data set"); //TODO remove before using threads!!!
			        }
					lastBodyDataCache = bodyData;
					lastBodyDataName = target;
					//Debug.Log("LandSat switched to different body"); //TODO remove before using threads!!!
				}
				//get the lattitude dataset
				SortedDictionary<double, Datum> bodyLatData;
				if (!bodyData.TryGetValue(datum.getLatitude(), out bodyLatData)) //should latitude be rounded at all? (also at Add below)
				{
					bodyLatData = new SortedDictionary<double, Datum>();
					bodyData.Add(datum.getLatitude(),bodyLatData);
					//Debug.Log("LandSat added new latitude data set"); //TODO remove before using threads!!!
		        }
				//Debug.Log("LandSat loaded latitude data set"); //TODO remove before using threads!!!
				//store the datum at the longitude
				try 
				{
					bodyLatData.Add(datum.getLongitude(),datum);
				} catch (ArgumentException) 
				{
					//TODO: check why this is hit once per cycle, does the collection code double-up somewhere?
					//Debug.Log("LandSat WARNING duplicate longitude key! DROPPING DATA."); //TODO remove before using threads!!!
				}
			}
		}


		//returns the average of all data in a range of latitudes and longitudes
		// should probably use Where see: http://msdn.microsoft.com/en-us/library/bb534803.aspx
		public double getAverageElevation (String target, double latitude1, double latitude2, double longitude1, double longitude2)
		{
			return 0d;
		}


		//returns a thread-safe deep copy marked as read-only
		public LandSatDataStore deepCopy()
		{
			return null;
		}

		//returns a thread-safe deep copy of the data for a single CelestialBody marked as read-only
		public LandSatDataStore deepCopy(CelestialBody target)
		{
			return null;
		}

		//returns a thread-safe deep copy of the data for a single CelestialBody, identified by name, marked as read-only
		public LandSatDataStore deepCopy(String targetname)
		{
			return null;
		}

		//returns a writable deep copy and freezes this copy for writing
		public LandSatDataStore deepCopyAndFreeze ()
		{
			LandSatDataStore copy = deepCopy();
			isFrozen = true;
			return copy;
		}

		//merges our frozen data points with the passed copy and unfreezes us
		public void mergeAndThaw (LandSatDataStore mergeTarget)
		{
			//replace our data with the data from the mergeTarget
			data = mergeTarget.data;
			//release freeze
			isFrozen = false;
			//add the data stored while frozen using storeData()
			foreach (TargetedDatum td in frozenStorage) 
			{
				this.storeData(td.getTarget(), td.getDatum());
				frozenStorage.Remove(td);
			}
		}

		//prunes the data set
		public void prune ()
		{
			if (isReadOnly) return; //really should throw an exception

			//other stuff
		}

		//returns the count of all data elements
		public long getCount ()
		{
			long ret = 0;
			foreach (KeyValuePair<String,SortedDictionary<double, SortedDictionary<double, Datum>>> bodyDataKP in data) {
				foreach (KeyValuePair<double,SortedDictionary<double, Datum>> bodyLatDataKP in bodyDataKP.Value) {
					ret += bodyLatDataKP.Value.LongCount();
				}
			}
			return ret;
		}

	}
}

