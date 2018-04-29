using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class planettest : MonoBehaviour {


	//get player()
	//get player position()
	//get player distance to planet()
	//get player distance to ground0()
	//get player distance to atmosphere()
	//get player geosphere face()
	//get player current tiles()
	//compute horizon size()

	//generate planet proxy()
	//update planet tiling()
	//set tile size()
	//set tile resolution
	//set tile array()
	//generate tiles()
	//
	//tile array coordinate
	//tile sphere projected coordinates

	//get global area size()
	//set planet radius in area()
	//set planet position()


	//https://forums.tigsource.com/index.php?topic=58921.0
	//Cube sphere coordinate
	
	//ground terrain + player : should certainly be a static data

	//create planet proxy
	//planet: radius (nombre de tile(planet specific) * taille d'une tile (project specific), 
	//loop:---------------------> responsability of this class?
	//	get player position 
	//	if position changed:
	//		generate tile
	//		generate lod
	
	
	
	
	static int Tilesize;
	
	
	//data
	int hashes;
	float worldsize;//radius
	float tilesize;
	float halftile;
	
	int radius;
	int atmosRadius;
	int maxAltitude;

	int type;

	int groundradius = 2;
	int groundsize;



	Mesh plane;
	Mesh terrain;
	Vector2[] Tcache; 
	
	public GameObject terrainCenter;
	public GameObject player;
	GameObject Planet;

	tile[,] TileGrid;
	
	public Vector3 playerRelativeToPlanetPosition;
	public Vector3 playerSurfacePosition;
	Vector3 terrainPosition;
	MeshGenerator.axis terrainFacing;
	
	
	//_____________________________________________________________________________
	void GetPlayerPosition(){//get player position relative to the planet surface 
		playerRelativeToPlanetPosition = this.player.transform.position - this.Planet.transform.position;
		playerSurfacePosition = playerRelativeToPlanetPosition.normalized;}
			
	void FindSurfacePosition(){//the position of terrain, given to "center tile"
		this.terrainPosition = HashSpherePosition(playerSurfacePosition);
		this.terrainCenter.transform.position = this.terrainPosition;
		this.terrainFacing = MeshGenerator.findCubeFace(this.terrainPosition);}
				
	void initTile(){//put tileobj in grid
		for (int i = 0; i<groundsize*groundsize  ;i++){
			int ax = i%groundsize;
			int ay = i/groundsize;
			this.TileGrid[ax,ay] = new tile();
			this.TileGrid[ax,ay].mesh = new Mesh();
			this.TileGrid[ax,ay].obj = Instantiate(this.terrainCenter);
			this.TileGrid[ax,ay].obj.name = "tile n "+i.ToString();}}
			
	void updateTerrainPosition(){//find position of each tile relative to hash and "center" position
		for (int i = 0; i<this.TileGrid.Length;i++){
			int ax = i%groundsize;
			int ay = i/groundsize;
			Vector2 Newposition;
			Vector2 tpos;
			tpos = GroundPlane(this.terrainFacing,terrainCenter.transform.position);//flatten position to 2d around "center" tile
			//2d position of the tile
			Newposition.x = tpos.x + ((float)ax-groundradius)/this.hashes;
			Newposition.y = tpos.y + ((float)ay-groundradius)/this.hashes;
			
			//->>add check for bound cross and update terrain facing <<<<<<<<<<<<<<<<<<<<<<<<<<<<<
			//tile = position3d, position2d, face, disable, mesh, depth? child?
			Newposition = checkBound(Newposition,this.TileGrid[ay,ax]);
			
			//find tile 3d pos according to facing
			this.TileGrid[ax,ay].obj.transform.position = PlaneToCubeFace(this.TileGrid[ay,ax].facing,Newposition);//<-- facing is to update - position to new face
			}}
			
			
			
			
	void generateTilemesh(){//put mesh and tile object inside the grid
		
		//mesh --> note: generate tcache first, use tcache to create tri and uv, reuse data for each tile
		this.Tcache = CreateTerrainCache(true,this.tilesize,this.hashes*2);						//create a cache of plane position

		for (int  i = 0 ; i< this.TileGrid.Length;i++){
			int ax = i%groundsize;
			int ay = i/groundsize;
			
			if(this.TileGrid[ax,ay].isdisable){continue;}//if disable, set renderer to false and skip that part

			this.TileGrid[ax,ay].mesh = MeshGenerator.createPlane(Vector3.zero,true, this.TileGrid[ay,ax].facing,this.tilesize,this.hashes*2);
			this.TileGrid[ax,ay].obj.gameObject.GetComponent<MeshFilter> ().mesh =  this.TileGrid[ax,ay].mesh;}}
	
	void updatetile(){//for each mesh in grid update and adapt the vertices to the facing
		for (int i = 0; i< this.TileGrid.Length;i++){
			int ax = i%groundsize;
			int ay = i/groundsize;
			
			this.TileGrid[ax,ay].obj.SetActive(true);
			if(this.TileGrid[ax,ay].isdisable){this.TileGrid[ax,ay].obj.SetActive(false);continue;}//if disable, set renderer to false and skip that part

			//reset the vertices to plane using the cache
			Vector3[] tc = this.TileGrid[ax,ay].mesh.vertices;								//cache the vertices of terrain
			for (int itc = 0; tc.Length > itc ;itc++){										//loop the cache
				tc[itc] = UpdateTerrainData(this.TileGrid[ay,ax].facing,this.Tcache[itc]);}	//using tcache update the plane
			//set the reseted position back to mesh	
			this.TileGrid[ax,ay].mesh.vertices = tc;}}
		
	void Offsetgrid(){//for each tileobj in grid, get the position, use position to reonctruct meshvert position in world
		for (int i = 0; i< this.TileGrid.Length;i++){
			int ax = i%groundsize;
			int ay = i/groundsize;
			
			if(this.TileGrid[ax,ay].isdisable){continue;}//if disable, set renderer to false and skip that part
			
			//set mesh to position on cube (using object position)
			Vector3 offset = this.TileGrid[ax,ay].obj.transform.position - this.Planet.transform.position;
			//project all vertices on sphere
			Vector3[] Varray = new Vector3[ this.TileGrid[ax,ay].mesh.vertices.Length];		//temp array to iterate
			Vector3[] mvert = this.TileGrid[ax,ay].mesh.vertices;//unnecessary?		;		//temp vertice array to read
			for (int iv = 0; this.TileGrid[ax,ay].mesh.vertices.Length > iv; iv++){			//loop all vertices
				Varray[iv] = MeshGenerator.SQRspherized(mvert[iv]+offset )-offset;}			//project on sphere
			this.TileGrid[ax,ay].mesh.vertices = Varray;
			
			MeshGenerator.RefreshMesh(this.TileGrid[ax,ay].mesh);}} 
		
	//_____________________________________________________________________________
	void CreateProxyPlanet(){
		this.plane = MeshGenerator.CreateCubeSphere();
		MeshGenerator.RefreshMesh (this.plane);
		this.Planet.GetComponent<MeshFilter> ().mesh = this.plane;}
		
		
	void GenerateTile(){
		GetPlayerPosition();
		FindSurfacePosition();
		
		initTile();
		updateTerrainPosition();//get the center position and init all position
		//mesh
		generateTilemesh();}			
	
	void UpdateTerrain(){
		GetPlayerPosition();
		FindSurfacePosition();
		
		updateTerrainPosition();
		//mesh
		updatetile();
		Offsetgrid();
	}

	
	void InitData(){
		this.Planet = this.gameObject;
		this.hashes = 5;
		this.worldsize = 1f;//radius
		this.tilesize = worldsize / hashes;
		this.halftile = tilesize / 2f;
		this.groundsize	= this.groundradius * 2 +1;
		
		this.TileGrid = new tile[groundsize,groundsize];}
	
	
	
	//____________________________________________________________________________
	// flow control
	void Awake (){
		InitData();
		CreateProxyPlanet();
		GenerateTile();}
		
	void Update(){
		UpdateTerrain();}
		
		
		
	//_____________________________________________________________________________
	// utils
	Vector3 SnapRound(Vector3 v){
		return new Vector3(
			Mathf.Floor(v.x),
			Mathf.Floor(v.y),
			Mathf.Floor(v.z));}
			
	Vector3 mulVector(Vector3 a,Vector3 b){
		return new Vector3(a.x * b.x,a.y * b.y,a.z * b.z);
	}
			
	Vector3 VectorPlane(MeshGenerator.axis a){
		if (a == MeshGenerator.axis.back ||  a == MeshGenerator.axis.front) return new Vector3(1,1,0);
		if (a == MeshGenerator.axis.bottom ||  a == MeshGenerator.axis.top) return new Vector3(1,0,1);
		return new Vector3(0,1,1);}
	
	Vector2 GroundPlane(MeshGenerator.axis a, Vector3 v) {
		if (a == MeshGenerator.axis.back	||  a == MeshGenerator.axis.front) return new Vector2 (v.x,v.y);//Vector3(1,1,0);
		if (a == MeshGenerator.axis.bottom	||  a == MeshGenerator.axis.top) return new Vector2 (v.x,v.z);//Vector3(1,0,1);
		return new Vector2 (v.y,v.z);//Vector3(0,1,1);
	}
	
	Vector3 PlaneToCubeFace(MeshGenerator.axis a, Vector2 plane){
		if (a == MeshGenerator.axis.front ) return new Vector3(plane.x,plane.y,-1);
		if (a == MeshGenerator.axis.back  ) return new Vector3(plane.x,plane.y, 1);
		if (a == MeshGenerator.axis.top   ) return new Vector3(plane.x, 1,plane.y);
		if (a == MeshGenerator.axis.bottom) return new Vector3(plane.x,-1,plane.y);
		if (a == MeshGenerator.axis.side2 ) return new Vector3(-1,plane.x,plane.y);
		return new Vector3(1,plane.x,plane.y);}
	
	Vector3 HashSpherePosition(Vector3 v){
		Vector3 cubed = MeshGenerator.sphere2cube (v);
		return VectorPlane(MeshGenerator.findCubeFace(cubed))
			* halftile + SnapRound( cubed * hashes ) / hashes;}
		
	Vector2[] CreateTerrainCache( bool center, float dimension = 300f, int division = 10){
		int vertnum = division+1;
		int vertnum2 = vertnum*vertnum;
		float div = (float) division;
		float tileSize = dimension/div;
		Vector2 UVoffset;
		Vector3 centerOffset;
		
		//set the center to the middle
		if (center) {
			UVoffset		= new Vector2 (0.5f, 0.5f);
			centerOffset	= new Vector3 (
				-div * tileSize * 0.5f,
				-div * tileSize * 0.5f,
				0.0f);}
		else {
			UVoffset		= Vector2.zero;
			centerOffset	= Vector3.zero;}

		//init mesh attributes
		Vector2[] v = new Vector2[vertnum2];

		//building the data
		int i = 0;while (i < vertnum2) {
			//set the x,y of the vertixces in the grid
			v[i] = new Vector2( 
				centerOffset.x + (float)(i % vertnum * tileSize),
				centerOffset.y + (float)(i / vertnum * tileSize));
			
			i++;}
		
		return v;
	}
		
	Vector3 UpdateTerrainData(MeshGenerator.axis face, Vector2 p){
		Vector3 v = Vector3.zero;
		switch (face) {
		case MeshGenerator.axis.top:
				v = new Vector3 (
					p.x,
					0,
					p.y);
				break;
			case MeshGenerator.axis.bottom:
				v = new Vector3 (
					p.x,
					0,
					-p.y);
				break;
			case MeshGenerator.axis.front:
				v = new Vector3 (
					p.x,
					p.y,
					0f);
				break;
			case MeshGenerator.axis.back:
				v = new Vector3 (
					-p.x,
					p.y,
					0f);
				break;
			case MeshGenerator.axis.side1:
				v = new Vector3 (
					0,
					p.y,
					p.x);
				break;
			case MeshGenerator.axis.side2:
				v = new Vector3 (
					0,
					p.y,
					-p.x);
				break;}
		return v;}
		
		
		
	
	class tile{
		public GameObject obj;
		public Mesh mesh;

		//public Vector2 TilePosition2D;
		//public Vector3 TileToSpherePosition;
		public MeshGenerator.axis facing;
		public bool isdisable;}
	
		
		
	Vector2 checkBound(Vector2 pos, tile t){
		//1 -> size of the face
		bool top	= pos.x >  1;
		bool bottom	= pos.x < -1;
		bool right	= pos.y >  1;
		bool left	= pos.y < -1;
		t.isdisable = (left || right) && (top || bottom);
		
		MeshGenerator.axis Newface = this.terrainFacing;
		Vector2 neo = pos;

		if(left || right || top || bottom){
			switch(this.terrainFacing){
			case MeshGenerator.axis.front ://XY: top = x+	right = y+
				if (left)  {Newface = MeshGenerator.axis.bottom; 	neo.x =		pos.x % 1;		neo.y =-1-	pos.y % 1;	}
				if (right) {Newface = MeshGenerator.axis.top; 		neo.x =		pos.x % 1;		neo.y =-1+	pos.y % 1;	}
				if (top)   {Newface = MeshGenerator.axis.side1;   	neo.y =-1+	pos.x % 1;		neo.x =		pos.y % 1;	}
				if (bottom){Newface = MeshGenerator.axis.side2;		neo.y =-1-	pos.x % 1;		neo.x =		pos.y % 1;	}
				break;
			case MeshGenerator.axis.back ://XY: top = x+	right = y+
				if (left)  {Newface = MeshGenerator.axis.bottom; 	neo.x =		pos.x % 1;		neo.y = 1+	pos.y % 1;	}
				if (right) {Newface = MeshGenerator.axis.top; 		neo.x =		pos.x % 1;		neo.y = 1-	pos.y % 1;	}
				if (top)   {Newface = MeshGenerator.axis.side1;   	neo.y =1-	pos.x % 1;		neo.x =		pos.y % 1;	}
				if (bottom){Newface = MeshGenerator.axis.side2;		neo.y =1+	pos.x % 1;		neo.x =		pos.y % 1;	}
				break;
			case MeshGenerator.axis.side1 ://YZ: top = y+	right = z+
				if (left)  {Newface = MeshGenerator.axis.front; 	neo.y =		pos.x % 1;		neo.x = 1+	pos.y % 1;	}
				if (right) {Newface = MeshGenerator.axis.back;  	neo.y =		pos.x % 1;		neo.x = 1-	pos.y % 1;	}
				if (top)   {Newface = MeshGenerator.axis.top;   	neo.x =	1-	pos.x % 1;		neo.y =		pos.y % 1;	}
				if (bottom){Newface = MeshGenerator.axis.bottom;	neo.x = 1+	pos.x % 1;		neo.y =		pos.y % 1;	}
				break;
			case MeshGenerator.axis.side2 ://YZ: top = y+	right = z+
				if (left)  {Newface = MeshGenerator.axis.front;  	neo.y =		pos.x % 1;		neo.x = -1-	pos.y % 1;	}
				if (right) {Newface = MeshGenerator.axis.back;		neo.y =		pos.x % 1;		neo.x =	-1+	pos.y % 1;	}
				if (top)   {Newface = MeshGenerator.axis.top;   	neo.x =-1+	pos.x % 1;		neo.y =		pos.y % 1;	}
				if (bottom){Newface = MeshGenerator.axis.bottom;	neo.x =-1-	pos.x % 1;		neo.y =		pos.y % 1;	}
				break;
			case	MeshGenerator.axis.top ://XZ: top = x+	right = z+
				if (left)  {Newface = MeshGenerator.axis.front;		neo.x =		pos.x % 1;		neo.y = 1+	pos.y % 1;	}
				if (right) {Newface = MeshGenerator.axis.back;		neo.x =		pos.x % 1;		neo.y = 1-	pos.y % 1;	}
				if (top)   {Newface = MeshGenerator.axis.side1;		neo.x =	1-	pos.x % 1;		neo.y =		pos.y % 1;	}
				if (bottom){Newface = MeshGenerator.axis.side2;		neo.x = 1+	pos.x % 1;		neo.y =		pos.y % 1;	}
				break;
			case MeshGenerator.axis.bottom ://XZ: top = x+	right = z+
				if (left)  {Newface = MeshGenerator.axis.front;		neo.x =		pos.x % 1;		neo.y =-1-	pos.y % 1;	}
				if (right) {Newface = MeshGenerator.axis.back;		neo.x =		pos.x % 1;		neo.y =-1+	pos.y % 1;	}
				if (top)   {Newface = MeshGenerator.axis.side1;		neo.x =-1+	pos.x % 1;		neo.y =		pos.y % 1;	}
				if (bottom){Newface = MeshGenerator.axis.side2;		neo.x =-1-	pos.x % 1;		neo.y =		pos.y % 1;	}
				break;}}
				
		t.facing = Newface;
		
		//string debugtext =  t.isdisable ? "discard" : "keep";
		//Debug.Log(this.terrainFacing + "  left  "+ left + " - right " + right + " - top " + top + " -bottom " + bottom + "  :  " + debugtext);
		//Debug.Log(groundsize + " // " +neo.x + "," + neo.y);
		
		return neo;
	}
		
		
		
		
	void plaincoordinate(Vector3 pos,MeshGenerator.axis face, int hash){//parameter need replace with member

		Vector3 facepose  = new Vector3(
			Mathf.Floor(pos.x*hash)+hash,
			Mathf.Floor(pos.y*hash)+hash,
			Mathf.Floor(pos.z*hash)+hash);
			
		Vector3 faceplane = mulVector( VectorPlane(face),facepose);	//get the coordinate to a plane
		Vector2 groundPosition = GroundPlane(face,faceplane);		//turn coordinate into 2d 
		
		int groundsize	= this.groundradius * 2 +1;//is a member, obsolete
		int terrainsize = this.hashes * 2; //should be member?
		if (groundsize > terrainsize){groundsize = (terrainsize/ 2)-1;}//error handling, rescale to terrainsize
		
		//find overlap
		Vector2 tileFaceSize0 = Vector2.zero;
		Vector2 tileFacesize1 = new Vector2(groundsize,groundsize);
		
		int leftbound	= -this.groundradius + (int)groundPosition.x;
		int rightbound	=  this.groundradius + (int)groundPosition.x;
		int upperbound	=  this.groundradius + (int)groundPosition.y;
		int bottombound	= -this.groundradius + (int)groundPosition.y;
		
		//iterate grid base on bound, assign tile to face base on coordinate check
		
		//---------------------------------
		//old way of figure out zone cut around limit
		//horizontal bound
		if (leftbound < 0){//Debug.Log("l<0");
			tileFaceSize0.x = Mathf.Abs((float)leftbound);
			tileFacesize1.x = groundsize - tileFaceSize0.x;}
		else if (rightbound > terrainsize-1){//Debug.Log("r>s");
			tileFaceSize0.x = (float)(rightbound - terrainsize+1);
			tileFacesize1.x = (float) groundsize - tileFaceSize0.x;}
		//vertical bound
		if (upperbound > terrainsize-1){//Debug.Log("u>s");
			tileFaceSize0.y = (float)(upperbound - terrainsize+1);
			tileFacesize1.y = (float) groundsize - tileFaceSize0.y;}
		else if (bottombound < 0){//Debug.Log("b<0");
			tileFaceSize0.y = Mathf.Abs((float)bottombound);
			tileFacesize1.y = groundsize - tileFaceSize0.y;}
	
		//int Hbound;
		//if leftbound then Hbound = 1
		//else if rightbound then Hbound = -1
		//else Hbound = 0
	
		//int Vbound;
		//if bottombound then Vbound = 1
		//else if upperbound then Vbound = -1
		//else Vbound = 0
	
		//return new vector2 (Hbound, Vbound)
	
		//return new Vector4(tileFacesize1.x,tileFacesize1.y, tileFaceSize0.x,tileFaceSize0.y);
		
		//create terrain
		//	 face 0		->	x1,y1
		//if face 1		->	x0,y1
		//if face 2		->	x1,y0
		//if disable	->	x0,y0
			
		//Debug.Log("size: "+hashes*2);
		//Debug.Log("right: " + rightbound + " center: " + groundPosition + " left: " +leftbound);
		//Debug.Log("up: " + upperbound + " center: " + groundPosition + " down: " + bottombound);
		//Debug.Log(facepose );
		
		//Debug.Log(
		//	"   x0: " + tileFaceSize0.x + " - y0: " + tileFaceSize0.y +
		//	" ----- x1: " + tileFacesize1.x + " - y1: " + tileFacesize1.y// +
		//);
		
		//new strategy:
		//iterate all tiles, if coordinate outside, assign face accordingly
	}	
		
	void tiletesting(){
		circular2dArray tiletest = new circular2dArray();
		tiletest.initGrid(10,10);
		tiletest.populateGrid();
		tiletest.ScanGrid();
		tiletest.setOffset(tiletest.grid.GetLength(0)/2,tiletest.grid.GetLength(1)/2);
		tiletest.ReadGrid();
	}
		
	class circular2dArray{
		public int[,] grid;
		
		int offsetx;
		int offsety;
		
		public void initGrid(int sizex,int sizey){this.grid = new int[sizex,sizey];}
		
		public void populateGrid(){
			int i = 0;while (i < grid.GetLength(0)){
				int j = 0;while (j < grid.GetLength(1)){
					grid[i,j] = i*grid.GetLength(0)+j;
					j++;}
				i++;}}
				
		public void setCell(int x, int y, int v){
			x = (x + offsetx) % grid.GetLength(0);
			y = (y + offsety) % grid.GetLength(1);
			grid[x,y] = v;}
			
		public int getCell(int x, int y){
			x = (x + offsetx) % grid.GetLength(0);
			y = (y + offsety) % grid.GetLength(1);
			return this.grid[x,y];}
			
		public void setOffset(int x, int y){offsetx = x;offsety = y;}
		public void getOffset(int x, int y){Debug.Log("> "+offsetx+" : "+offsety);}

		//shiftgridup
		//shiftgriddown
		//shiftgridright
		//shiftgridleft
		
		//updateshiftgridup
		//updateshiftgriddown
		//updateshiftgridright
		//updateshiftgridleft
		
		public void ScanGrid(){
			string scan = "";
			int i = 0;while (i < grid.GetLength(0)){
				
				int j = 0;while (j < grid.GetLength(1)){
					scan += grid[i,j].ToString() + " ";
					j++;}
					
				//Debug.Log("scanned: " + scan);scan = "";
				i++;}}
				
		public void ReadGrid(){
			string scan = "";
			int i = 0;while (i < grid.GetLength(0)){
				
				int j = 0;while (j < grid.GetLength(1)){
					scan += getCell(i,j).ToString() + " ";
					j++;}
					
				//Debug.Log("read: " + scan);scan = "";
				i++;}}
	}
		

		
	//_____________________________________________________________________________
	// debug
	
	void OnDrawGizmos(){
		//debug display
		Vector3 gizmosize = Vector3.up *.02f;
		Vector3 sized = new Vector3 (0.1f, 0.1f, 0.1f);
		
		//hash
		Vector3 cubed = MeshGenerator.sphere2cube (playerSurfacePosition);	//cube projection
		Vector3 bucket = VectorPlane(MeshGenerator.findCubeFace(cubed)) 
			* halftile + SnapRound( cubed * hashes ) / hashes;				//hash the position 

		//Debug.Log(MeshGenerator.findCubeFace(cubed));
		
		//----------------------------------------------------------------------------------------------
		//display the debug
		foreach (Vector3 v in this.plane.vertices) {//for each vertex in mesh add a gizmo to their cube projected position
			Vector3 vc = MeshGenerator.sphere2cube (v); Gizmos.color = Color.white;
			Gizmos.DrawLine (vc - gizmosize, vc + gizmosize );}
			
		//Gizmos.color = Color.white;Gizmos.DrawWireCube (ProxyRelativePosition, sized);//draw proxy
		Gizmos.color = Color.white;Gizmos.DrawWireSphere (playerSurfacePosition, 0.1f);//draw proxy on the surface
		Gizmos.color = Color.red;Gizmos.DrawCube (bucket, (sized/2) - 0.02f*Vector3.one);//draw bucketed proxy on the cube
		Gizmos.color = Color.green;Gizmos.DrawWireCube (cubed, sized/2);//draw proxy on the cube
		//Gizmos.color = Color.white;Gizmos.DrawLine (this.transform.position, ProxyRelativePosition);//draw to proxy on the cube
		//Debug.Log (playerSurfacePosition);
	}
}

