using UnityEngine;
using System.Collections;

public class SIHJR_PVFS_Particle {

	private int _id;

	private float _x;
	private float _y;
	private float _xPrev;
	private float _yPrev;

	private Vector2 _velocity;
	private SIHJR_PVFS_GridReference _gridReference;

	private float _density;
	private float _densityNear;
	private float _densityRest;

	//private float _q;
	private IList _neighbors;


	//public SIHJR_PVFS_Particle(int id) {
	public SIHJR_PVFS_Particle() {
		//init (id);
		init ();
	}
	//public SIHJR_PVFS_Particle(int id, int x, int y) {
	public SIHJR_PVFS_Particle(float x, float y) {
		//init (id);
		init ();

		_x = x;
		_y = y;
	}

	//private void init(int id) {
	private void init() {
		_id = 0;

		_x = 0;
		_y = 0;
		
		_velocity = Vector2.zero;
		
		_densityRest = 8f;

		_neighbors = new ArrayList ();
	}

	public void randomInitialVelocity ()
	{
		_velocity = new Vector2 (Random.Range (-0.5f, 0.5f), Random.Range (-0.5f, 0.5f));
	}



	public float x {
		get { return _x; }
		set { _x = value; }
	}
	
	public float y {
		get { return _y; }
		set { _y = value; }
	}

	public Vector2 velocity {
		get { return _velocity; }
		set { _velocity = value; }
	}
	
	public SIHJR_PVFS_GridReference gridReference {
		get { return _gridReference; }
		set { _gridReference = value; }
	}

	/*
	public float q {
		get { return _q; }
		set { _q = value; }
	}
	*/
	
	public int id {
		get { return _id; }
		set { _id = value; }
	}

	
	public float density {
		get { return _density; }
		set { _density = value; }
	}
	public float densityNear {
		get { return _densityNear; }
		set { _densityNear = value; }
	}
	public float densityRest {
		get {
			if (_densityRest == -1)
			{
				//TODO: equation should be:
				//1-r/h
				//-> implement it;
				_densityRest = 1f;
			}
			return _densityRest;
		}
	}

	public IList neighbors {
		get { return _neighbors; }
		set { _neighbors = value; }
	}

	public Vector2 previousPosition ()
	{
		return new Vector2 (_xPrev, _yPrev);
	}

	//displace vector 
	public void displaceVector (Vector2 dVector)
	{
		//TODO: maybe its - instead of + dVector (despite written down in algorithm)
		_x = _x + dVector.x;
		_y = _y + dVector.y;
	}

	public void applyGravity (Vector2 _gravity, float deltaTime)
	{
		//Debug.Log ("[pID:"+_id+"] -- gravity -- velocity previous: [x:" + _velocity.x + ",y:" + _velocity.y + "]");
		_velocity = _velocity + deltaTime * _gravity;
		//Debug.Log ("[pID:"+_id+"] -- gravity -- velocity afterwards: [x:" + _velocity.x + ",y:" + _velocity.y + "]");
	}

	//sture actual pos
	public void storePrevPosition ()
	{
		_xPrev = _x;
		_yPrev = _y;
	}

	//move particle to new position based on velocity
	public void advanceToPredictedPosition (float deltaTime)
	{
		_x = _x + deltaTime * _velocity.x;
		_y = _y + deltaTime * _velocity.y;
	}

	//recalculates velocity based on old position and actual position (which was moved around a lot by fluid simulation)
	public void computeVelocity (float deltaTime)
	{
		//Debug.Log ("[pID:"+_id+"] velocity previous: [x:" + _velocity.x + ",y:" + _velocity.y + "]");
		_velocity.x = (_x - _xPrev) / deltaTime;
		_velocity.y = (_y - _yPrev) / deltaTime;
		//_velocity.x = (_x - _xPrev);
		//_velocity.y = (_y - _yPrev);
		//Debug.Log ("[pID:"+_id+"] velocity afterwards: [x:" + _velocity.x + ",y:" + _velocity.y + "]");
	}
}
