using UnityEngine;
using System.Collections;

public class SIHJR_PVFS_GridReference
{
	private int _x;
	private int _y;

	public SIHJR_PVFS_GridReference (int gridX, int gridY)
	{
		_x = gridX;
		_y = gridY;
	}

	public int x {
		get { return _x; }
		set { _x = value; }
	}
	
	public int y {
		get { return _y; }
		set { _y = value; }
	}
}


