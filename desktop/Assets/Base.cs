using UnityEngine;
using System.Collections;

public class Base : MonoBehaviour {

	private static float APPEAR_SPEED = 0.025f;
	private static float ALPHA_MIN = 1.0f;
	private static float ALPHA_MAX = 1.0f;

	private enum Transition {
		None,
		Appearing,
		Disappearing
	};

	private Transition transition = Transition.None;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		float destAlpha = renderer.material.color.a;
		switch (transition) {
		case Transition.Appearing:
			destAlpha = Mathf.Min (ALPHA_MAX, renderer.material.color.a + APPEAR_SPEED);
			if (destAlpha >= ALPHA_MAX) {
				transition = Transition.None;
			}
			break;
		case Transition.Disappearing:
			destAlpha = Mathf.Max (ALPHA_MIN, renderer.material.color.a - APPEAR_SPEED);
			if (destAlpha <= ALPHA_MIN) {
				transition = Transition.None;
			}
			if (destAlpha <= 0.0f) {
				renderer.enabled = false;
			}
			break;
		}
		renderer.material.color = new Color (renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, destAlpha);
	}

	public void Show()
	{
		transition = Transition.Appearing;
		if (!renderer.enabled) {
			renderer.material.color = new Color (renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 0.0f);
			renderer.enabled = true;
		}
	}

	public void Hide()
	{
		transition = Transition.Disappearing;
	}
}
