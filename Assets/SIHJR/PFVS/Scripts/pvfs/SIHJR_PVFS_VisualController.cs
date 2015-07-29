using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SIHJR_PVFS_VisualController : Object {
	
	private GameObject _visualObject;
	//private IList _visuals;
	
	private IDictionary<SIHJR_PVFS_Particle, GameObject> _visualParticleMap;

	public SIHJR_PVFS_VisualController(GameObject visual){
		_visualObject = visual;

		//_visuals = new ArrayList ();
		
		_visualParticleMap = new Dictionary<SIHJR_PVFS_Particle, GameObject>();
	}

	public void Step() {
		//refresh visual positions

		/*
		foreach (GameObject go in _visuals) {
			go.transform.position.Set(go.transform.position.x + 1, 0f, 0f);
		}
		*/
		foreach (KeyValuePair<SIHJR_PVFS_Particle, GameObject> kvPair in _visualParticleMap) {
			//kvPair.Value.transform.Translate(kvPair.Key.velocity.x, kvPair.Key.velocity.y, 0f);
			//kvPair.Value.transform.Translate(kvPair.Key.x, kvPair.Key.y, 0f);
			//kvPair.Value.transform.position.x = kvPair.Key.x;
			//kvPair.Value.transform.position.y = kvPair.Key.y;
			kvPair.Value.transform.position = new Vector2(kvPair.Key.x, kvPair.Key.y);
		}
	}

	/**
	 * returns the Visual that spawned
	 * */
	public GameObject SpawnVisual(float x, float y) {
		GameObject v = Instantiate(_visualObject, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
		v.layer = 10;
		return v;
	}

	/**
	 * adds the visual and particle to one map
	 */
	public void Connect (SIHJR_PVFS_Particle par, GameObject vis)
	{
		_visualParticleMap.Add(par, vis);
	}

	public void removeVisualFor(SIHJR_PVFS_Particle par) {
		GameObject vis = null;
		_visualParticleMap.TryGetValue (par, out vis);
		if (vis != null) {
			Destroy(vis);
			_visualParticleMap.Remove (par);
		}
	}
}
