var pitch=0.0; 
var yaw=0.0; 
var roll=0.0; 
 
function Update() { 
	transform.Rotate((Vector3.right*pitch) * Time.deltaTime); 
	transform.Rotate((Vector3.up*yaw) * Time.deltaTime); 
	transform.Rotate((Vector3.forward*roll) * Time.deltaTime); 
} 