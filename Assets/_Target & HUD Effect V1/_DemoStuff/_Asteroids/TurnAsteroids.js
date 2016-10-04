var x:float;
var y:float;
var z:float;

var xSpeed:int;
var ySpeed:int;
var zSpeed:int;


function Start(){
	
	x = Random.Range(0.0,1.0);
	y = Random.Range(0.0,1.0);
	z = Random.Range(0.0,1.0);
	
	xSpeed = Random.Range(0,10.0);	
}

function Update () {

	transform.Rotate( Vector3(x,0,0), xSpeed * Time.deltaTime);
	transform.Rotate( Vector3(0,y,0), ySpeed * Time.deltaTime);
	transform.Rotate( Vector3(0,0,z), zSpeed * Time.deltaTime);

}