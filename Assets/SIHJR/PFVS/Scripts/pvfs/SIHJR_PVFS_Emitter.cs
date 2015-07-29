using UnityEngine;
using System.Collections;

public class SIHJR_PVFS_Emitter : MonoBehaviour {

	private SIHJR_PVFS_Controller _controller;
	private SIHJR_PVFS_VisualController _visualController;


	public float _startTime = 0f;
	public float _spawnRate = 1f;
	private float _spawnNext;

	public GameObject _boundary;
	public Collider2D _body;
	public GameObject _visualObject;
	public Vector2 _gravity;

	private int count = 0;

	// Use this for initialization
	void Start () {
		_controller = new SIHJR_PVFS_Controller ();
		_visualController = new SIHJR_PVFS_VisualController (_visualObject);
		_controller.setGridBoundary (_boundary);
		_controller.setVisualController (_visualController);
		_controller.addBody (_body);

		_spawnNext = _startTime;
	}
	
	// Update is called once per frame
	void Update () {
		//set new Gravity of controller
		//_controller.Gravity = new Vector2 (0.0f, -0.981f);//Physics2D.gravity;
		_controller.Gravity = _gravity;
		//Physics2D.gravity = new Vector2(Physics2D.gravity.x + 0.5f, Physics2D.gravity.y - 0.5f);
		//Debug.Log ("grav: " + Physics2D.gravity);

		//refresh particles and visuals
		_controller.Step();
		_visualController.Step ();


		//timer for new spawn
		if (_spawnNext <= 0f) {
			if (count < 1000) {
				//spawn thingy
				float randomX = Random.Range (-0.05f, 0.05f);
				float randomY = Random.Range (-0.05f, 0.05f);
				//Debug.Log (" random Pos [x:" + randomX + ",y:" + randomY + "]");


				GameObject vis = _visualController.SpawnVisual(randomX,randomY);
				SIHJR_PVFS_Particle par = _controller.SpawnParticle(randomX,randomY);
				par.id = count;
				//par.randomInitialVelocity();
				_visualController.Connect(par, vis);

				//make 10er visible
				if (count % 10 == 0) vis.GetComponent<SpriteRenderer>().color = Color.red;
				else vis.GetComponent<SpriteRenderer>().color = Color.blue;

				count++;
			}

			//set new delay
			_spawnNext = _spawnRate;
		} else {
			_spawnNext -= Time.deltaTime;
		}
	}
}
