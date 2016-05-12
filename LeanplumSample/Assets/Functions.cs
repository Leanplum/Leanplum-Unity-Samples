using LeanplumSDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Functions : MonoBehaviour {

	
	// Update is called once per frame
	public void ForceContentUpdate () {
		Leanplum.ForceContentUpdate ();
	}

	public void TrackEvent1 () {
		Leanplum.Track ("Event 1 fired");
	}
}
