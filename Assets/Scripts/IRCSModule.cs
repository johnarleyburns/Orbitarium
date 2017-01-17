using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRCSModule {

    void RCSBurst(Vector3 dir);

    void RCSAngularBurst(Quaternion dir);

    void RCSCutoff();
}
