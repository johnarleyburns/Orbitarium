var altTime:float;
private var start;
var on:boolean;
private var halo;

function Start(){

	start = Time.realtimeSinceStartup ;
	halo = GetComponent("Halo");
}

function Update () {

	if (Time.realtimeSinceStartup - start > altTime){
		start = Time.realtimeSinceStartup ;
		on = !on;
	}
	halo.enabled=on;
}