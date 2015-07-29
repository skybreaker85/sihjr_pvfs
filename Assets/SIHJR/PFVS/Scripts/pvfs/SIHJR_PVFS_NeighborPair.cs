using UnityEngine;
using System.Collections;

public class SIHJR_PVFS_NeighborPair
{
	private SIHJR_PVFS_Particle _pi;
	private SIHJR_PVFS_Particle _pj;
	
	public SIHJR_PVFS_NeighborPair (SIHJR_PVFS_Particle pi, SIHJR_PVFS_Particle pj)
	{
		_pi = pi;
		_pj = pj;
	}
	
	//checks if the two particles has this spring attached
	public bool consistsOf (SIHJR_PVFS_Particle pi, SIHJR_PVFS_Particle pj) {
		if (pi == _pi && pj == _pj) {
			return true;
		} else {
			if (pj == _pi && pi == _pj) {
				return true;
			} else {
				return false;
			}
		}
	}

	public bool contains(SIHJR_PVFS_Particle particle) {
		if (particle == _pi || particle == _pj) {
			return true;
		} else {
			return false;
		}
	}
	
	public SIHJR_PVFS_Particle particle1 {
		get {
			return _pi;
		}
	}
	
	public SIHJR_PVFS_Particle particle2 {
		get {
			return _pj;
		}
	}
}