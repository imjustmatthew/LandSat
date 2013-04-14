using System;

namespace LandSat
{
	public class LandSatDataStore
	{
		public LandSatDataStore ()
		{
		}

		private bool isReadOnly = false;
		private bool isFrozen = false;

		//some magic instance variable(s) to store the data should go here


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

		//Adds a data point to the store, this method should be very fast
		public void storeData (CelestialBody target, double latitude, double longitude, double surfaceAltitude)
		{
			if (isReadOnly) return; //really should throw an exception

			if (isFrozen) {
				//store the data in some kind of special frozen data container
			} else {
				//store the data
			}
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
			isFrozen = false;
			//add the data stored while frozen using storeData()
		}

		//prunes the data set
		public void prune ()
		{
			if (isReadOnly) return; //really should throw an exception

			//other stuff
		}

	}
}

