#pragma strict

var camEntity : Transform;
var distance:float=5;

var angleX:float = 45.0;
var angleY:float = 45.0;
var angleZ:float = 45.0;

var translateX:float = 1.0;
var translateY:float = 1.0;
var translateZ:float = 1.0;

function Update() {

	if (camEntity) {
		
		var dist:float = Vector3.Distance(camEntity.position, transform.position);
		
		if (dist<distance) {
		
			transform.Rotate(transform.TransformDirection(Vector3.right), angleX * Mathf.Deg2Rad * Time.deltaTime);
			transform.Rotate(transform.TransformDirection(Vector3.up), angleY * Mathf.Deg2Rad * Time.deltaTime);
			transform.Rotate(transform.TransformDirection(Vector3.forward), angleZ * Mathf.Deg2Rad * Time.deltaTime);

			transform.position += transform.TransformDirection(Vector3.right) * translateX * Time.deltaTime;
			transform.position += transform.TransformDirection(Vector3.up) * translateY * Time.deltaTime;
			transform.position += transform.TransformDirection(Vector3.right) * translateZ * Time.deltaTime;
			
		}
		
 	}else { 
	
		transform.Rotate(transform.TransformDirection(Vector3.right), angleX * Mathf.Deg2Rad * Time.deltaTime);
		transform.Rotate(transform.TransformDirection(Vector3.up), angleY * Mathf.Deg2Rad * Time.deltaTime);
		transform.Rotate(transform.TransformDirection(Vector3.forward), angleZ * Mathf.Deg2Rad * Time.deltaTime);

		transform.position += transform.TransformDirection(Vector3.right) * translateX * Time.deltaTime;
		transform.position += transform.TransformDirection(Vector3.up) * translateY * Time.deltaTime;
		transform.position += transform.TransformDirection(Vector3.forward) * translateZ * Time.deltaTime;	
	
	}

}

function OnDrawGizmosSelected () {
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere (transform.position, distance);
}