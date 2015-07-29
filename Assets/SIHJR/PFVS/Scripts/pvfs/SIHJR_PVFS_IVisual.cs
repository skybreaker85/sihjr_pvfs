using UnityEngine;
using System.Collections;

public interface SIHJR_PVFS_IVisual {

	GameObject connectedVisual();
	SIHJR_PVFS_Particle connectedParticle();

	void setConnectedVisual(SIHJR_PVFS_IVisual vis);
	void setConnectedParticle(SIHJR_PVFS_Particle par);
}
