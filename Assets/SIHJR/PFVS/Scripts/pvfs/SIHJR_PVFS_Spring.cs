using UnityEngine;
using System.Collections;

public class SIHJR_PVFS_Spring
{
	private SIHJR_PVFS_Particle _pi;
	private SIHJR_PVFS_Particle _pj;
	private float _restLength;

	public SIHJR_PVFS_Spring (SIHJR_PVFS_Particle pi, SIHJR_PVFS_Particle pj, float restLength)
	{
		_pi = pi;
		_pj = pj;
		_restLength = restLength;
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

	public float L {
		get {
			return _restLength;
		}
		set {
			_restLength = value;
		}
	}
}
