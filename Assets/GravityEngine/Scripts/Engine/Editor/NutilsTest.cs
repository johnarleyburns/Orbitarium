using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class NutilsTest {

    [Test]
    public void AngleFromSinCos()
    {
    	// check each quadrant
		float[] angles = { 0, 0.3f*Mathf.PI, 0.7f*Mathf.PI, 1.3f*Mathf.PI, 1.7f*Mathf.PI};
        foreach (float angle in angles ) {
			float a = NUtils.AngleFromSinCos( Mathf.Sin(angle), Mathf.Cos(angle));
			Debug.Log( "angle=" + angle + " a=" + a);
			Assert.IsTrue( Mathf.Abs(angle - a) < 1E-2);
        }
    }
}
