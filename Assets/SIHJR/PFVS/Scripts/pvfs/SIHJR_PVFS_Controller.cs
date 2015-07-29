using UnityEngine;
using System.Collections;

public class SIHJR_PVFS_Controller
{

	//private int _idCount;

	private IList _particles;
	private IList _springs;
	//private IList _neighborPairsPi;	//pairs are there in two arrays, maybe this is stupid for computation speed, but let it this way for now
	//private IList _neighborPairsPj;	//other solution would be: use neigbour class and spirings IN neigbours class
	private IList _neighborPairs;
	private IList _bodies;
	private float _h;	//influence radius: particle to particle
	private float _k;	//stiffness parameter
	private float _kNear;	//stiffness parameter to near particles
	private float _kSpring;	//stiffness parameter to springs
	private float _q;	//temporal value from particle-influence computation
	private float _d;	//temporal value from particle-spring computation (tolerable deformation)
	private float _u;	//temporal value from viscosity computation (projected velocity)
	private float _pressureTemp;
	private float _pressureTempNear;
	private Vector2 _dx;	//displacement Vector;
	private Vector2 _D;	// density relaxation displacement as Vector (temporal between surrounding points)
	private float _L;	// spring relaxation Threshhold (restLength)
	private float _y_gamma;		//spring yield ratio
	private float _a_alpha;		//spring plasticity constant
	private float _s_sigma;		//viscosity (used for higher viscosity)
	private float _b_beta;		//viscosity (used for less viscose fluids)
	private float _m_my;		//where µ is a friction parameter enabling slip (µ = 0) or noslip (µ = 1) boundary conditions.
	//private float _myOwnStickyParameter;
	private Vector2 _gravity;
	private SIHJR_PVFS_Grid _grid;
	private SIHJR_PVFS_VisualController _visualController;

	public SIHJR_PVFS_Controller ()
	{
		//_idCount = 0;

		_particles = new ArrayList ();
		_springs = new ArrayList ();
		//_neighborPairsPi = new ArrayList ();
		//_neighborPairsPj = new ArrayList ();
		_neighborPairs = new ArrayList ();

		_bodies = new ArrayList ();

		_h = 0.2f;
		_k = 0.5f;
		_kNear = 0.7f;
		_kSpring = 0.2f;
		_q = 0f;
		_pressureTemp = 0f;
		_pressureTempNear = 0f;
		//_dx = Vector2.zero;
		//_D = Vector2.zero;
		_L = 0.31f;
		_y_gamma = 0.2f;
		_a_alpha = 0.2f;
		_s_sigma = 1.2f;
		_b_beta = 1.02f;
		_m_my = 0f;
		//_myOwnStickyParameter = 0.1f;

		_gravity = new Vector2 (0f, -0.981f);
	}

	public void Step ()
	{
		if (_particles.Count > 0) {
			SimulationStep ();
		}
	}

	public SIHJR_PVFS_Particle SpawnParticle (float x, float y)
	{
		//int id = nextID();

		//SIHJR_PVFS_Particle p = new SIHJR_PVFS_Particle(id, x, y);
		SIHJR_PVFS_Particle p = new SIHJR_PVFS_Particle (x, y);
		_particles.Add (p);
		_grid.insertParticleToGrid (p);
		return p;
	}
	/*

	private int nextID() {
		if (_idCount < int.MaxValue) {
			_idCount++;
		} else {
			//reset idcounter to 0
			_idCount = 0;
			//TODO: 4billion particles are very much, but its possible to reach this value, so lok for a better way to connect visuals and  particle
		}
		return _idCount;
	}
	*/

	//algorithm 1 in pvfs document
	private void SimulationStep ()
	{
		/*
		Algorithm 1: Simulation step.
		1. foreach particle i
		2. 		// apply gravity
		3. 		vi <- vi +Dtg
		4. // modify velocities with pairwise viscosity impulses
		5. applyViscosity // (Section 5.3)
		6. foreach particle i
		7. 		// save previous position
		8. 		x-prev(i) 	<-	x(i)
		9. 		// advance to predicted position
		10. 	x(i) <- x(i) + Dt*v(i)
		11. // add and remove springs, change rest lengths
		12. adjustSprings // (Section 5.2)
		13. // modify positions according to springs,
		14. // double density relaxation, and collisions
		15. applySpringDisplacements // (Section 5.1)
		16. doubleDensityRelaxation // (Section 4)
		17. resolveCollisions // (Section 6)
		18. foreach particle i
		19. // use previous position to compute next velocity
		20. vi (xi􀀀xprev
		i )=Dt
		*/


		// apply gravity
		foreach (SIHJR_PVFS_Particle p in _particles) {
			p.applyGravity (_gravity, Time.deltaTime);
		}

		// modify velocities with pairwise viscosity impulses
		applyViscosity ();


		foreach (SIHJR_PVFS_Particle p in _particles) {
			// save previous position
			p.storePrevPosition ();

			// advance to predicted position
			p.advanceToPredictedPosition (Time.deltaTime);
		}
		
		//TODO is here the step for neighbour search? becasue it seems here are the actualk positions
		refreshGridPositions ();
		computeNeighbors ();


		// add and remove springs, change rest lengths
		//adjustSprings ();

		// modify positions according to springs,
		//double density relaxation, and collisions
		//applySpringDisplacements ();
		doubleDensityRelaxation ();
		resolveCollisions ();

		
		// use previous position to compute next velocity
		foreach (SIHJR_PVFS_Particle p in _particles) {
			p.computeVelocity (Time.deltaTime);
		}
	}

	//collision computation	#############################################################################################################################
	void resolveCollisions ()
	{
		/*
		Algorithm 6: Particle-body interactions.
		1. foreach body
		2. 		save original body position and orientation
		3. 		advance body using V and w
		4. 		clear force and torque buffers
		5. 		foreach particle inside the body
		6. 			compute collision impulse I
		7. 			add I contribution to force and torque buffers
		8. foreach body
		9. 		modify V with force and w with torque
		10. 	advance from original position using V and w
		11. resolve collisions and contacts between bodies
		12. foreach particle inside a body
		13. 	compute collision impulse I
		14. 	apply I to the particle
		15. 	extract the particle if still inside the body
		*/


		/*
		Impulses due to particle-body collisions are based on
		a zero-restitution collision model with wet friction. This
		model requires the current particle velocity v_i, computed using
		the difference between the current and previous particle
		positions. We then compute the particle relative velocity
		¯v = v_i - v_p, where v_p is the body velocity at the contact
		point. This velocity is separated into normal and tangential
		components:
		¯v'normal = (¯v (dot) n') 'n (9)
		¯v'tangent = ¯v - ¯v'normal: (10)
		The impulse computed at lines 6 and 13 cancels the normal
		velocity and removes a fraction of the tangential velocity:
		I = ¯v'normal - µ ¯v'tangent
		where µ is a friction parameter enabling slip (µ = 0) or noslip
		(µ = 1) boundary conditions.
		*/


		//part 1, lines 1-7
		//TODO left out becasue the body shouldnt move
		//part 2, lines 8-11
		//TODO left out becasue the body shouldnt move
		//part 3, lines 12-15
		Collider2D body = (Collider2D)_bodies [0];
		Vector2 normalVector;
		Vector2 impulse;
		Vector2 relVelocity; //particle relative velocity
		Vector2 normalVelocity;
		Vector2 tangentVelocity;
		float xPos, yPos;
		foreach (SIHJR_PVFS_Particle p in _particles) {

			if (true == body.OverlapPoint (new Vector2 (p.x, p.y))) {
				//Collision2D col = new Collision2D();
				//ContactPoint2D cp = new ContactPoint2D();

				/*Vector4[] tangents = body.GetComponent<MeshFilter>().mesh.tangents;
				for (int i = 0 ; i < tangents.Length; i++) {
					Debug.Log("### a TANGENT : " + tangents[i]);
					Debug.DrawLine(new Vector3(tangents[i].x, tangents[i].y, 0), Vector3.zero, Color.cyan);
				}
				*/
				//Vector3[] normals = body.GetComponent<MeshFilter>().mesh.normals;
				/*for (int i = 0 ; i < normals.Length; i++) {
					Debug.Log("### a NORMAL : " + normals[i]);
					Debug.DrawLine(normals[i], Vector3.zero, Color.cyan);
				}*/
				/*
				Vector3[] vertices = body.GetComponent<MeshFilter>().mesh.vertices;
				for (int i = 0 ; i < vertices.Length; i++) {
					Debug.Log("### a VERTICE : " + vertices[i]);
					Debug.DrawLine(vertices[0], vertices[1], Color.cyan);
				}

				Debug.Log("particle hits body: " + p.previousPosition());
				Debug.DrawLine(new Vector3(p.x, p.y, 0), Vector3.zero, Color.green);
				*/
				//p.x = p.previousPosition().x;
				//p.y = p.previousPosition().y;




				//TEST CALCULATE IT WITH NORMAL = UP VECTOR
				relVelocity = p.velocity - Vector2.zero; //TODO: Vector2.zero should be the velocity of thebody computed from previous steps, but for now, no body moves = zero velocity
				//normalVector = Vector2.up;
				normalVector = new Vector2 (p.x - body.bounds.center.x, p.y - body.bounds.center.y);
				normalVector.Normalize ();
				normalVelocity = Vector2.Dot (relVelocity, normalVector) * normalVector; //TODO Vector.up should be the normal from the body shape
				tangentVelocity = relVelocity - normalVelocity;
				_m_my = 0.1f;
				impulse = normalVelocity + _m_my * tangentVelocity;	//TODO kurios: + und - sind umgedreht im vgl zum paper
				//Debug.Log ("Inpulse is: " + impulse);
				//Debug.Log ("Normal  is: " + normalVector);
				//Debug.Log("actual Velocity is: " + p.velocity);
				//p.velocity = new Vector2(p.velocity.x - impulse.x, p.velocity.y - impulse.y);
				//Debug.Log("corrected Velocity is: " + p.velocity);

				//Debug.Log ("actual position is: " + p.x + "," + p.y);
				p.x = p.x - impulse.x * Time.deltaTime;	//TODO kurios: + und - sind umgedreht im vgl zum paper
				p.y = p.y - impulse.y * Time.deltaTime;	//TODO kurios: + und - sind umgedreht im vgl zum paper
				//Debug.Log ("corrected position is: " + p.x + "," + p.y);

			}
			

			//collision to plane
			/*
			if (p.y < -0.9f) {
				//TEST CALCULATE IT WITH NORMAL = UP VECTOR
				relVelocity = p.velocity - Vector2.zero; //TODO: Vector2.zero should be the velocity of thebody computed from previous steps, but for now, no body moves = zero velocity
				//Debug.Log ("relVelocity is: " + relVelocity);
				normalVector = Vector2.up;
				//normalVector = new Vector2 (p.x - body.bounds.center.x, p.y - body.bounds.center.y);
				//normalVector = new Vector2 (1,1);
				normalVector.Normalize ();
				//Debug.Log ("normalVector is: " + normalVector);
				normalVelocity = Vector2.Dot (relVelocity, normalVector) * normalVector; //TODO Vector.up should be the normal from the body shape
				//Debug.Log ("normalVelocity is: " + normalVelocity);
				tangentVelocity = relVelocity - normalVelocity;
				//Debug.Log ("tangentVelocity is: " + tangentVelocity);
				_m_my = 0.1f;
				impulse = normalVelocity + _m_my * tangentVelocity;
				//Debug.Log ("Inpulse is: " + impulse);
				//Debug.Log("actual Velocity is: " + p.velocity);
				//p.velocity = new Vector2(p.velocity.x - impulse.x, p.velocity.y - impulse.y);
				//Debug.Log("corrected Velocity is: " + p.velocity);
				
				//Debug.Log ("actual position is: " + p.x + "," + p.y);
				xPos = p.x - impulse.x * Time.deltaTime;
				yPos = p.y - impulse.y * Time.deltaTime;
				Vector2 correctionVector = new Vector2(p.x - xPos, p.y - yPos);
				//Debug.Log ("correctionVector is: " + correctionVector);
				//Debug.Log ("correctionVector * TangentialVelocity is: " + (Vector2.Dot(correctionVector,tangentVelocity) * tangentVelocity));
				p.x = xPos;
				p.y = yPos;
				//Debug.Log ("corrected position is: " + p.x + "," + p.y);
			}
			*/
			
			/*
			if (p.x < -0.9f) {
				//TEST CALCULATE IT WITH NORMAL = UP VECTOR
				relVelocity = p.velocity - Vector2.zero; //TODO: Vector2.zero should be the velocity of thebody computed from previous steps, but for now, no body moves = zero velocity
				//Debug.Log ("relVelocity is: " + relVelocity);
				normalVector = Vector2.left;
				//normalVector = new Vector2 (p.x - body.bounds.center.x, p.y - body.bounds.center.y);
				//normalVector = new Vector2 (1,1);
				normalVector.Normalize ();
				//Debug.Log ("normalVector is: " + normalVector);
				normalVelocity = Vector2.Dot (relVelocity, normalVector) * normalVector; //TODO Vector.up should be the normal from the body shape
				//Debug.Log ("normalVelocity is: " + normalVelocity);
				tangentVelocity = relVelocity - normalVelocity;
				//Debug.Log ("tangentVelocity is: " + tangentVelocity);
				_m_my = 0.1f;
				impulse = normalVelocity + _m_my * tangentVelocity;
				//Debug.Log ("Inpulse is: " + impulse);
				//Debug.Log("actual Velocity is: " + p.velocity);
				//p.velocity = new Vector2(p.velocity.x - impulse.x, p.velocity.y - impulse.y);
				//Debug.Log("corrected Velocity is: " + p.velocity);
				
				//Debug.Log ("actual position is: " + p.x + "," + p.y);
				xPos = p.x - impulse.x * Time.deltaTime;
				yPos = p.y - impulse.y * Time.deltaTime;
				Vector2 correctionVector = new Vector2(p.x - xPos, p.y - yPos);
				//Debug.Log ("correctionVector is: " + correctionVector);
				//Debug.Log ("correctionVector * TangentialVelocity is: " + (Vector2.Dot(correctionVector,tangentVelocity) * tangentVelocity));
				p.x = xPos;
				p.y = yPos;
				//Debug.Log ("corrected position is: " + p.x + "," + p.y);
			}

			if (p.x > 0.9f) {
				//TEST CALCULATE IT WITH NORMAL = UP VECTOR
				relVelocity = p.velocity - Vector2.zero; //TODO: Vector2.zero should be the velocity of thebody computed from previous steps, but for now, no body moves = zero velocity
				//Debug.Log ("relVelocity is: " + relVelocity);
				normalVector = Vector2.right;
				//normalVector = new Vector2 (p.x - body.bounds.center.x, p.y - body.bounds.center.y);
				//normalVector = new Vector2 (1,1);
				normalVector.Normalize ();
				//Debug.Log ("normalVector is: " + normalVector);
				normalVelocity = Vector2.Dot (relVelocity, normalVector) * normalVector; //TODO Vector.up should be the normal from the body shape
				//Debug.Log ("normalVelocity is: " + normalVelocity);
				tangentVelocity = relVelocity - normalVelocity;
				//Debug.Log ("tangentVelocity is: " + tangentVelocity);
				_m_my = 0.1f;
				impulse = normalVelocity + _m_my * tangentVelocity;
				//Debug.Log ("Inpulse is: " + impulse);
				//Debug.Log("actual Velocity is: " + p.velocity);
				//p.velocity = new Vector2(p.velocity.x - impulse.x, p.velocity.y - impulse.y);
				//Debug.Log("corrected Velocity is: " + p.velocity);
				
				//Debug.Log ("actual position is: " + p.x + "," + p.y);
				xPos = p.x - impulse.x * Time.deltaTime;
				yPos = p.y - impulse.y * Time.deltaTime;
				Vector2 correctionVector = new Vector2(p.x - xPos, p.y - yPos);
				//Debug.Log ("correctionVector is: " + correctionVector);
				//Debug.Log ("correctionVector * TangentialVelocity is: " + (Vector2.Dot(correctionVector,tangentVelocity) * tangentVelocity));
				p.x = xPos;
				p.y = yPos;
				//Debug.Log ("corrected position is: " + p.x + "," + p.y);
			}
			*/
		}

	}

	public void addBody (Collider2D body)
	{
		_bodies.Add (body);
	}

	//useful computation	#############################################################################################################################
	/*
	private void applyGravity(SIHJR_PVFS_Particle p) {

	}

	private void storePrevPosition (SIHJR_PVFS_Particle p)
	{

	}

	private void advanceToPredictedPosition (SIHJR_PVFS_Particle p)
	{

	}	
	*/
	
	private void computeNeighbors ()
	{
		
		float radiiDistTemp;
		int circleDistanceAroundPivot = 1;
		IList list;
		SIHJR_PVFS_Particle pj;

		//fluish neighbor pairs
		_neighborPairs.Clear ();

		foreach (SIHJR_PVFS_Particle pi in _particles) {
			//clear neighbours
			pi.neighbors.Clear();
			//get particles in cells around this particle
			list = _grid.getCellContentAroundPivot (pi.gridReference, circleDistanceAroundPivot);
			
			//Debug.Log ("[piID:"+pi.id+"] neighbour count before: " + list.Count);
			//remove particles whose distance is too far away
			for (int i = list.Count-1; i>=0; i--) {
				pj = (SIHJR_PVFS_Particle)list [i];
				radiiDistTemp = radiiDistance (pi, pj);
				//radiiVecTemp = normalizedDirectionVector(pi,pj);
				_q = radiiDistTemp / _h;

				if (_q > 1) {
					//remove particle
					list.RemoveAt (i);
					//Debug.Log ("[at piID:"+pi.id+", pjID:" + pj.id + "] neighbour remove _q: " + _q + " where radii: " + radiiDistTemp);
				}
				if (pi == pj) {
					//remove particle becasue its the same referenced particle
					list.RemoveAt (i);
					//Debug.Log ("[at piID:"+pi.id+", pjID:" + pj.id + "] neighbour remove SAME particle: ");
				}
			}
			//Debug.Log ("[piID:"+pi.id+"] neighbour count after: " + list.Count);


			pi.neighbors = list;

			//go through list and add neighbor pairs
			foreach (SIHJR_PVFS_Particle n in list) {
				_neighborPairs.Add(new SIHJR_PVFS_NeighborPair(pi, n));
			}
		}

	}

	private void refreshGridPositions ()
	{
		/*
		foreach (SIHJR_PVFS_Particle p in _particles) {
			SIHJR_PVFS_GridReference gR = _grid.gridPositionOf(p);
			//TODO maybe dont use "if" branches but just set the values, becasue its computed anyways!?
			//TODO BUT !!!!!!!! then the list in grid needs to be updated every time, so maybe its okay
			if (gR.x != p.gridReference.x || gR.y != p.gridReference.y) {
				//Debug.Log("grX: " + gR.x + " - grY: " + gR.y);
				if (gR.x == -1 || gR.y == -1) {
					//remove particle
					Debug.Log("REMOVE #### grX: " + gR.x + " - grY: " + gR.y + " (pX:" + p.x + ",pY:" + p.y + ")");

					removeParticle(p);
				} else {
					//change grid cell
					Debug.Log("change #### grX: " + gR.x + " - grY: " + gR.y + " (pX:" + p.x + ",pY:" + p.y + ")");
					_grid.moveParticleTo(p, gR);
				}
			}
		}*/

		_grid.clearGrid ();

		SIHJR_PVFS_Particle p;
		SIHJR_PVFS_GridReference gR;
		for (int i = _particles.Count-1; i>=0; i--) {
			p = (SIHJR_PVFS_Particle)_particles [i];
			//gR = _grid.gridPositionOf (p);
			gR = _grid.gridPositionOf (p);

			if (gR.x == -1 || gR.y == -1) {
				//remove particle
				//Debug.Log ("[pID:"+p.id+"] REMOVE #### grX: " + gR.x + " - grY: " + gR.y + " (pX:" + p.x + ",pY:" + p.y + ")");
				
				removeParticle (p);
			} else {
				//change grid cell
				/*
				Debug.Log ("[pID:"+p.id+"] change #### grX: " + gR.x + " - grY: " + gR.y + 
				           " __FROM__ p.grX: " + p.gridReference.x + " - p.grY: " + p.gridReference.y + 
				           " (pX:" + p.x + ",pY:" + p.y + ")" + 
				           " _____ gridCount [FROM]: " + _grid.getCellContentAroundPivot(p.gridReference,0).Count);
				*/
				//_grid.moveParticleTo (p, gR);

				_grid.insertParticleToGrid(p);

			}
		}



		/*
		SIHJR_PVFS_Particle p;
		SIHJR_PVFS_GridReference gR;
		for (int i = _particles.Count-1; i>=0; i--) {
			p = (SIHJR_PVFS_Particle)_particles [i];

			gR = _grid.gridPositionOf (p);

			//Debug.Log("grX: " + gR.x + " - grY: " + gR.y);

			//TODO maybe dont use "if" branches but just set the values, becasue its computed anyways!?
			//TODO BUT !!!!!!!! then the list in grid needs to be updated every time, so maybe its okay
			if (gR.x != p.gridReference.x || gR.y != p.gridReference.y) {
				//Debug.Log("grX: " + gR.x + " - grY: " + gR.y);
				if (gR.x == -1 || gR.y == -1) {
					//remove particle
					//Debug.Log ("[pID:"+p.id+"] REMOVE #### grX: " + gR.x + " - grY: " + gR.y + " (pX:" + p.x + ",pY:" + p.y + ")");
					
					removeParticle (p);
				} else {
					//change grid cell
					/*
					Debug.Log ("[pID:"+p.id+"] change #### grX: " + gR.x + " - grY: " + gR.y + 
					           " __FROM__ p.grX: " + p.gridReference.x + " - p.grY: " + p.gridReference.y + 
					           " (pX:" + p.x + ",pY:" + p.y + ")" + 
					           " _____ gridCount [FROM]: " + _grid.getCellContentAroundPivot(p.gridReference,0).Count);
					* /
					_grid.moveParticleTo (p, gR);
				}
			}
		}
		*/
	}

	private void removeParticle (SIHJR_PVFS_Particle p)
	{
		//remove visual
		_visualController.removeVisualFor (p);
		//remove from list
		_particles.Remove (p);
		//remove all neighbor connections
		//SIHJR_PVFS_Particle pi, pj;
		/*
		foreach (SIHJR_PVFS_Particle pi in _neighborPairsPi) {
			pj = (SIHJR_PVFS_Particle)_neighborPairsPj [_neighborPairsPi.IndexOf (pi)];		//TODO computational nonesense? -> AND WONT WORK -> Change it
			if (pi == p || pj == p) {
				_neighborPairsPi.Remove(p);
				_neighborPairsPj.Remove(p);
			}
		}
		*/

		/*
		foreach (SIHJR_PVFS_NeighborPair pair in _neighborPairs) {
			//pj = (SIHJR_PVFS_Particle)_neighborPairsPj [_neighborPairsPi.IndexOf (pi)];
			pi = pair.particle1;
			pj = pair.particle2;

			/*
			if (pi == p || pj == p) {
				_neighborPairsPi.Remove(p);
				_neighborPairsPj.Remove(p);
			}
			* /
			if (pair.contains(p)) {
				_neighborPairs.Remove(pair);
			}
		}
		*/
		SIHJR_PVFS_NeighborPair pair;
		for (int i = _neighborPairs.Count - 1; i >= 0; i--) {
			pair = (SIHJR_PVFS_NeighborPair)_neighborPairs [i];
			//pi = pair.particle1;
			//pj = pair.particle2;
			
			/*
			if (pi == p || pj == p) {
				_neighborPairsPi.Remove(p);
				_neighborPairsPj.Remove(p);
			}
			*/
			if (pair.contains (p)) {
				//_neighborPairs.Remove(pair);
				_neighborPairs.RemoveAt (i);
			}
		}
	}

	public void setGridBoundary (GameObject _boundary)
	{
		_grid = new SIHJR_PVFS_Grid (_boundary, _h);
	}

	public void setVisualController (SIHJR_PVFS_VisualController visC)
	{
		_visualController = visC;
	}
	
	//Viscosity computation	#############################################################################################################################
	void applyViscosity ()
	{
		/*
		Algorithm 5: Viscosity impulses.
		1. foreach neighbor pair i j, (i < j)
		2. 		q <- r_ij/h
		3. 		if q < 1
		4. 			// inward radial velocity
		5. 			u <- (v_i - v_j) * r'_ij
		6. 			if u > 0
		7. 				// linear and quadratic impulses
		8. 				I <- Dt * (1-q)(s*u + b*u²) * r'_ij			//s = sigma, b = beta		//I is same datatype like variable _D, thats why _D is used
		9. 				vi vi􀀀I=2
		10. 			vj vj +I=2
		*/

		SIHJR_PVFS_Particle pi, pj;
		float radiiDistTemp;
		Vector2 radiiVecTemp;
		/*
		foreach (SIHJR_PVFS_Particle pi in _neighborPairsPi) {
			pj = (SIHJR_PVFS_Particle) _neighborPairsPj [_neighborPairsPi.IndexOf (pi)];		//TODO computational nonesense?

			radiiDistTemp = radiiDistance (pi, pj);
			radiiVecTemp = normalizedDirectionVector(pi,pj);
			_q =  radiiDistTemp / _h;
			if (_q < 1) {
				_u = Vector2.Dot( (pi.velocity - pj.velocity), radiiVecTemp);

				if (_u > 0) {
					//_D is used instead of using another variable _I (to safe memory)
					_D = Time.deltaTime * (1-_q) * (_s_sigma * _u + _b_beta * Mathf.Pow(_u,2)) * radiiVecTemp;
					pi.velocity = pi.velocity - _D/2f;
					pj.velocity = pj.velocity + _D/2f;
				}
			}

		}
		*/

		foreach (SIHJR_PVFS_NeighborPair pair in _neighborPairs) {
			pi = pair.particle1;
			pj = pair.particle2;

			radiiDistTemp = radiiDistance (pi, pj);
			radiiVecTemp = normalizedDirectionVector (pi, pj);
			_q = radiiDistTemp / _h;
			if (_q < 1) {
				_u = Vector2.Dot ((pi.velocity - pj.velocity), radiiVecTemp);
				
				if (_u > 0) {
					//_D is used instead of using another variable _I (to safe memory)
					_D = Time.deltaTime * (1 - _q) * (_s_sigma * _u + _b_beta * Mathf.Pow (_u, 2)) * radiiVecTemp;
					pi.velocity = pi.velocity - _D / 2f;
					pj.velocity = pj.velocity + _D / 2f;
				}
			}
		}


	}
	
	//spring computation	#############################################################################################################################
	void applySpringDisplacements ()
	{
		/*
		Algorithm 3: Spring displacements.
		1. foreach spring i j
		2. D <- Dt² * k'spring * (1-L_ij/h) * (L_ij-r_ij)* r'ij
		3. xi <- xi -D/2
		4. xj <- xj +D/2


		To simulate elastic behavior, we add springs between pairs
		of neighboring particles. Springs create displacements on the
		two attached particles. The displacement magnitude is proportional
		to L-r, where r is the distance between the particles
		and L is the spring rest length. It is also scaled with the
		factor 1-L/h, which gradually reduces to zero the force exerted
		by long springs. The process is detailed in Algorithm 3.
		*/



		//go through every spring
		if (_springs.Count > 0) {
			foreach (SIHJR_PVFS_Spring s in _springs) {

				// compute density and near-density
				//pi.neighbors = computeNeighbors (pi);
				_D = Mathf.Pow (Time.deltaTime, 2) * _kSpring * (1 - s.L / _h) * (s.L - radiiDistance (s.particle1, s.particle2)) * normalizedDirectionVector (s.particle1, s.particle2);
				s.particle1.displaceVector (-_D / 2f);
				s.particle2.displaceVector (_D / 2f);
			}
		}

	}

	private void adjustSprings ()
	{
		/*
		Algorithm 4: Spring adjustment.
		1. foreach neighbor pair i j, (i < j)
		2. 		q <- r_ij/h
		3. 		if q < 1
		4. 			if there is no spring i j
		5. 				add spring i j with rest length h
		6. 			// tolerable deformation = yield ratio * rest length
		7. 			d <- g * L_ij									/g = gamma
		8. 			if r_ij > L+d // stretch
		9. 				L_ij <- L_ij + Dt a(r_ij - L - d)		/a = alpha
		10. 		else if r_ij < L-d // compress
		11. 			L_ij <- L_ij - Dt a(L - d - r_ij)		/a = alpha
		12. foreach spring i j
		13. 	if Li j > h
		14. 	remove spring i j
		*/

		SIHJR_PVFS_Particle pi, pj;
		SIHJR_PVFS_Spring s;
		float radiiDistTemp;
		/*
		foreach (SIHJR_PVFS_Particle pi in _neighborPairsPi) {
			pj = (SIHJR_PVFS_Particle) _neighborPairsPj [_neighborPairsPi.IndexOf (pi)];		//TODO computational nonesense?

			radiiDistTemp = radiiDistance (pi, pj);
			_q =  radiiDistTemp / _h;
			if (_q < 1) {
				s = addSpring(pi, pj);		//return spring | function adds or dont adds but returns the spring when already inserted

				_d = _y_gamma * s.L;

				if(radiiDistTemp > _L + _d) {
					//compress
					s.L = s.L + Time.deltaTime * _a_alpha * (radiiDistTemp - _L - _d);
				} else if (radiiDistTemp < _L - _d) {
					//stretch
					s.L = s.L - Time.deltaTime * _a_alpha * (_L - _d - radiiDistTemp);
				}
			}
		}
		*/

		
		foreach (SIHJR_PVFS_NeighborPair pair in _neighborPairs) {
			pi = pair.particle1;
			pj = pair.particle2;
			
			radiiDistTemp = radiiDistance (pi, pj);
			_q = radiiDistTemp / _h;
			//Debug.Log ("[pIDs pi:"+pair.particle1.id+"&pj:" + pair.particle2.id+"] ------------------------------------ Spring _q: " + _q);
			//if (_q < 1) {
				s = addSpring (pi, pj);		//return spring | function adds or dont adds but returns the spring when already inserted

				_d = _y_gamma * s.L;

				//Debug.Log ("[pIDs pi:"+s.particle1.id+"&pj:" + s.particle2.id+"] ------------------------------------ Spring radii:" + radiiDistTemp + ", _d:" + _d + ", _L:" + _L);
				if (radiiDistTemp > _L + _d) {
					//compress
					s.L = s.L + Time.deltaTime * _a_alpha * (radiiDistTemp - _L - _d);
				} else if (radiiDistTemp < _L - _d) {
					//stretch
					s.L = s.L - Time.deltaTime * _a_alpha * (_L - _d - radiiDistTemp);
				}
			//}
		}



		//remove springs
		if (_springs.Count > 0) {
			/*
			foreach (SIHJR_PVFS_Spring si in _springs) {
				if (si.L > _h)
				{
					_springs.Remove(si);
				}
			}
			*/
			SIHJR_PVFS_Spring spring;
			for (int i = _springs.Count - 1; i >= 0; i--) {
				spring = (SIHJR_PVFS_Spring)_springs [i];
				//Debug.Log ("[pIDs pi:"+spring.particle1.id+"&pj:" + spring.particle2.id+"] ------------------------------------ Spring L is: " + spring.L);
				if (Mathf.Abs(spring.L) > _h) {
					//Debug.Log ("[pIDs pi:"+spring.particle1.id+"&pj:" + spring.particle2.id+"] ------------------------------------ Spring removed");
					_springs.RemoveAt (i);
				}
			}
		}
	}

	SIHJR_PVFS_Spring addSpring (SIHJR_PVFS_Particle pi, SIHJR_PVFS_Particle pj)
	{
		SIHJR_PVFS_Spring s = null;
		bool alreadyIn = false;
		foreach (SIHJR_PVFS_Spring si in _springs) {
			if (si.consistsOf (pi, pj)) {
				alreadyIn = true;
				s = si;
				break;
			}
		}
		if (alreadyIn == false) {
			s = new SIHJR_PVFS_Spring (pi, pj, _h);
			_springs.Add (s);
			//Debug.Log ("[pIDs pi:"+s.particle1.id+"&pj:" + s.particle2.id+"] ------------------------------------ Spring added");

		} else {
			//this branch should never be reached, but the compiler does need it
			//Debug.Log ("[pIDs pi:"+s.particle1.id+"&pj:" + s.particle2.id+"] ------------------------------------ Spring already inside");

		}
		return s;
	}

	//density computation	#############################################################################################################################
	private void doubleDensityRelaxation ()
	{
		/*
		Algorithm 2: Double density relaxation.
		1. foreach particle i
		2. 		r <- 0
		3. 		rnear <- 0
		4. 		// compute density and near-density
		5. 		foreach particle j € neighbors( i )
		6. 			q <- r_ij/h
		7. 			if q < 1
		8. 				r <- r+(1-q)²
		9. 				r'near <- r'near + (1-q)³
		10. 	// compute pressure and near-pressure
		11. 	P <- k(r-r0)
		12. 	P'near <- k'near * r'near
		
		13. 	dx <- 0
		14. 	foreach particle j € neighbors( i )
		15. 		q <- r_ij/h
		16. 		if q < 1
		17. 			// apply displacements
		18. 			D <- Dt² * P_i(1-r_ij/h) * r'_ij
		19. 			x_j <- x_j + D/2
		20. 			dx <- dx - D/2
		21. 	x_i <- x_i + dx
		*/

		foreach (SIHJR_PVFS_Particle pi in _particles) {
			pi.density = 0f;
			pi.densityNear = 0f;

			// compute density and near-density
			foreach (SIHJR_PVFS_Particle pj in pi.neighbors) {
				_q = radiiDistance (pi, pj) / _h;
				if (_q < 1f) {
					pi.density = pi.density + Mathf.Pow (1 - _q, 2);
					pi.densityNear = pi.densityNear + Mathf.Pow (1 - _q, 3);
				}
			}
			// compute pressure and near-pressure
			_pressureTemp = _k * (pi.density - pi.densityRest);
			_pressureTempNear = _kNear * pi.densityNear;
			//Debug.Log ("[pID:"+pi.id+"] displacement _pressureTemp: " + _pressureTemp + ", _pressureTempNear:" + _pressureTempNear);


			//displacement vector AFTER all neighbors are displaced (other particles can push
			//		the particle 360° around but doesnt need to push at all, so its computed over all neighbor particles)
			_dx.Set (0f, 0f);

			foreach (SIHJR_PVFS_Particle pj in pi.neighbors) {
				_q = radiiDistance (pi, pj) / _h;
				//Debug.Log ("[pIDs:"+pi.id+"&" + pj.id+"] displacement radii _q: " + _q);
				if (_q < 1f) {
					_D = Mathf.Pow (Time.deltaTime, 2) * (_pressureTemp * (1 - _q) + _pressureTempNear * Mathf.Pow (1 - _q, 2)) * normalizedDirectionVector (pi, pj);
					//Debug.Log ("[pIDs:"+pi.id+"&" + pj.id+"] displacement _D[x: " + _D.x + ",y:" + _D.y + "] with normalizedVvector [x:"+
					//           normalizedDirectionVector (pi, pj).x+",y:"+normalizedDirectionVector (pi, pj).y+" ########################################################");
					//displace other poarticles
					//i.e. away from (the looked at) particle
					pj.displaceVector (_D / 2f);
					_dx = _dx - _D / 2f;
				}
			}
			//displace particle
			//Debug.Log ("[pID:"+pi.id+"]displacement by _dx[x:" + _dx.x + ",y:" + _dx.y + "]  --  with neighborCount: " + pi.neighbors.Count);
			pi.displaceVector (_dx);
		}
	}

	private float radiiDistance (SIHJR_PVFS_Particle pi, SIHJR_PVFS_Particle pj)
	{
		//pythagoras d = sqrt( |xi-xj|² + |yi-yj|² )
		float a = Mathf.Sqrt (
			Mathf.Pow (pi.x - pj.x, 2) + 
			Mathf.Pow (pi.y - pj.y, 2)
			);
		if (float.IsNaN (a)) {
			Debug.Log("IS NAN !!!!!!!! ------------ !!!!!!!! ------------ !!!!!!!! ------------ !!!!!!!! ------------ !!!!!!!!");
			Debug.Log ("[pIDs pi:"+pi.id+"&pj:" + pj.id+"] NAN WAS: pi.x:" + pi.x + ", pj.x:" + pj.x + ", pi.y:" + pi.y + ", pj.y:" + pj.y);
		}
		return a;
		/*
		return Mathf.Sqrt (
			Mathf.Pow (pi.x - pj.x, 2) + 
			Mathf.Pow (pi.y - pj.y, 2)
		);
		*/
	}

	private Vector2 normalizedDirectionVector (SIHJR_PVFS_Particle pi, SIHJR_PVFS_Particle pj)
	{
		Vector2 temp = new Vector2 ();
		temp.x = pi.x - pj.x;
		temp.y = pi.y - pj.y;
		temp.Normalize ();
		return temp;
	}
	
	public Vector2 Gravity {
		get { return _gravity; }
		set { _gravity = value;}
	}
}
