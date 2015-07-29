using UnityEngine;
using System.Collections;

public class SIHJR_PVFS_Grid
{
	private float _left;
	private float _top;
	private float _right;
	private float _bottom;
	private Bounds _bounds;
	private float _influenceWidth;
	private int _gridWidthCellCount;
	private int _gridHeightCellCount;

	//private List<List<List<SIHJR_PVFS_Particle>>> _particles;
	private IList[,] _gridArray;

	public SIHJR_PVFS_Grid (GameObject _boundary, float influenceWidth)
	{
		Collider2D collider = _boundary.GetComponents<Collider2D> () [0];
		_bounds = collider.bounds;
		_left = collider.bounds.min.x;
		_bottom = collider.bounds.min.y;
		_right = collider.bounds.max.x;
		_top = collider.bounds.max.y;
		_influenceWidth = influenceWidth;

		//create new grid
		_gridWidthCellCount = (int)Mathf.Ceil (collider.bounds.size.x / _influenceWidth);
		_gridHeightCellCount = (int)Mathf.Ceil (collider.bounds.size.y / _influenceWidth);
		//Debug.Log ("gridW: " + _gridWidthCellCount + " -- gridH: " + _gridHeightCellCount);
		//Debug.Log ("grid L: " + _left + " -- grid R: " + _right);
		//Debug.Log ("grid Cell Width: " + _influenceWidth);

		_gridArray = new IList[_gridWidthCellCount + 1, _gridHeightCellCount + 1];

		/*
		_particles = new LinkedList<List<List<SIHJR_PVFS_Particle>>>(xCount);
		for (int i = 0; i < xCount; i++) {
			//create list
			_particles[i] = new LinkedList<List<SIHJR_PVFS_Particle>>(yCount);

			for (int j = 0; j < yCount; j++) {
				_particles[j] = new LinkedList<SIHJR_PVFS_Particle>();
			}

			//create nested lists
		}
		*/




	}

	public void clearGrid () {
		for (int i = 0; i < _gridWidthCellCount; i++) {
			//Debug.Log("first loop");
			for (int j = 0; j < _gridHeightCellCount; j++) {
				if (_gridArray[i, j] != null && _gridArray[i, j].Count > 0) {//if (_gridArray[i, j].Count > 0) {
					_gridArray[i, j].Clear();
				}
			}
		}
	}

	public void insertParticleToGrid (SIHJR_PVFS_Particle particle)
	{
		//calculate where particle is sistuated on grid
		SIHJR_PVFS_GridReference gR = gridPositionOf(particle);
		insertParticleToGrid (particle, gR);

		//Debug.Log ("inserted AT grX: " + gR.x + ", grY: " + gR.y + " -- thats Count: " + _gridArray[gR.x, gR.y].Count);// + " - gridArray: " + _gridArray[gR.x, gR.y].ToString());

		/*
		Debug.Log ("gr: " + gR.x);// + " - gridArray: " + _gridArray[gR.x, gR.y].ToString());

		
		//insert particle to grid
		if (_gridArray[gR.x, gR.y] == null) {
			//Debug.Log ("null");
			_gridArray[gR.x, gR.y] = new ArrayList ();
		} else {
			//Debug.Log ("not null but: " + _gridArray [0, 0]);
			//_gridArray[gR.x, gR.y].Add (particle);
			_gridArray[gR.x, gR.y].Add(particle);
		}
		//add information of gridreference to particle
		particle.gridReference = gR;
		*/

	}
	public void insertParticleToGrid (SIHJR_PVFS_Particle particle, SIHJR_PVFS_GridReference gR)
	{
		//Debug.Log ("gr: " + gR.x);// + " - gridArray: " + _gridArray[gR.x, gR.y].ToString());

		//insert particle to grid
		if (_gridArray[gR.x, gR.y] == null) {
			//Debug.Log ("null");
			_gridArray[gR.x, gR.y] = new ArrayList ();
		} else {
			//Debug.Log ("not null but: " + _gridArray [0, 0]);
			//_gridArray[gR.x, gR.y].Add (particle);
		}
		_gridArray[gR.x, gR.y].Add(particle);
		//add information of gridreference to particle
		particle.gridReference = gR;
		
	}

	public void moveParticleTo (SIHJR_PVFS_Particle p, SIHJR_PVFS_GridReference gR)
	{
		//p.gridReference
		//Debug.Log ("moveParticleTo-Method -- gridcount OLD before remove: " + _gridArray [p.gridReference.x, p.gridReference.y].Count + " gr["+p.gridReference.x+","+p.gridReference.y+"]");
		_gridArray [p.gridReference.x, p.gridReference.y].Remove (p);
		//Debug.Log ("moveParticleTo-Method -- gridcount OLD after  remove: " + _gridArray [p.gridReference.x, p.gridReference.y].Count + " gr["+p.gridReference.x+","+p.gridReference.y+"]");
		//_gridArray [gR.x, gR.y].Add (p);
		if (_gridArray [gR.x, gR.y] == null) {
			//Debug.Log ("moveParticleTo-Method -- gridcount NEW before remove: null & 0");
		} else {
			//Debug.Log ("moveParticleTo-Method -- gridcount NEW before remove: " + _gridArray [gR.x, gR.y].Count + " gr["+gR.x+","+gR.y+"]");
		}
		insertParticleToGrid (p, gR);
		//Debug.Log ("moveParticleTo-Method -- gridcount NEW after  remove: " + _gridArray [gR.x, gR.y].Count + " gr["+gR.x+","+gR.y+"]");
	}

	public SIHJR_PVFS_GridReference gridPositionOf (SIHJR_PVFS_Particle particle)
	{
		float centerDiffX, centerDiffY;
		int gridX, gridY;

		if (particle.x > _left && particle.x < _right) {
			//calculate difference between center points
			centerDiffX = _bounds.center.x - particle.x;
			//then use the influenceWidth (_h from controller which is the gridWidth)
			centerDiffX = centerDiffX / _influenceWidth;	//can be negative
			//becasue it can be negative, add the difference to the center (half gridWidthCount)
			gridX = (int)Mathf.Ceil (_gridWidthCellCount / 2f + centerDiffX);
		} else {
			gridX = -1;
		}


		if (particle.y > _bottom && particle.y < _top) {
			//calculate difference between center points
			centerDiffY = _bounds.center.y - particle.y;
			//then use the influenceWidth (_h from controller which is the gridWidth)
			centerDiffY = centerDiffY / _influenceWidth;	//can be negative
			//becasue it can be negative, add the difference to the center (half gridWidthCount)
			gridY = (int)Mathf.Ceil (_gridHeightCellCount / 2f + centerDiffY);
		} else {
			gridY = -1;
		}

		SIHJR_PVFS_GridReference gridReference = new SIHJR_PVFS_GridReference (gridX, gridY);
		return gridReference;
	}

	/**
	 * computes the neighbouring particles around the pivot gridreverence with the distance of circleDistanceAroundPivot
	 */
	public IList getCellContentAroundPivot (SIHJR_PVFS_GridReference gridReference, int circleDistanceAroundPivot)
	{
		IList list = new ArrayList ();
		//list.Add (_gridArray [gridReference.x, gridReference.y]);
		int l, t, r, b;
		l = Mathf.Max (0,	gridReference.x - circleDistanceAroundPivot);
		t = Mathf.Max (0,	gridReference.y - circleDistanceAroundPivot);
		r = Mathf.Min (_gridWidthCellCount,		gridReference.x + circleDistanceAroundPivot);
		b = Mathf.Min (_gridHeightCellCount,	gridReference.y + circleDistanceAroundPivot);

		/*
		Debug.Log ("around pX: " + gridReference.x + "-pY: " + gridReference.y + " is :::::::: l: " + l + ", t: " + t + ", r: " + r + ", b:" + b);
		if (circleDistanceAroundPivot == 0 && _gridArray [gridReference.x, gridReference.y] != null) {
			Debug.Log ("count around pX: " + gridReference.x + "-pY: " + gridReference.y + " is :::::::: " + _gridArray [gridReference.x, gridReference.y].Count);
		}
		*/

		//go through grid
		for (int i = l; i<=r; i++) {
			//Debug.Log("first loop");
			for (int j = t; j<=b; j++) {
				//Debug.Log("second loop");
				//go through list
				//Debug.Log("grid: " + _gridArray [i, j] + " -- equals null?: " + (_gridArray [i, j] == null));
				if (_gridArray [i, j] != null && _gridArray [i, j].Count > 0) {
					//Debug.Log("grid at [" + i + "," + j + "] count: " + _gridArray [i, j].Count);
					foreach (SIHJR_PVFS_Particle p in _gridArray [i, j]) {
						list.Add (p);
					}
				}
			}
		}

		return list;
	}
}

