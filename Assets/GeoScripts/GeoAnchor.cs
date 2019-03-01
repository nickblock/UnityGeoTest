using UnityEngine;

namespace ScapeKitUnity
{
	public class GeoAnchor : MonoBehaviour
	{
		public double Longitude;
		public double Latitude;
		public double MaxDistance = 1000.0;

		private Vector2 WorldPos;
		private Vector3 ScenePos;
		private Coordinates WorldCoordinates;
		private bool needsUpdate = false;

		void OriginEvent(Coordinates SceneOriginCoordinates) {

			Vector2 SceneOrigin = GeoConversions.VectorFromCoordinates(SceneOriginCoordinates);

			ScenePos = WorldPos - SceneOrigin;

			string name = this.gameObject.name;

			ScapeLogging.Log(message: "OriginEvent() " + name + " ScenePos = " + ScenePos.ToString());
			ScapeLogging.Log(message: "OriginEvent() " + name + " WorldCoords = " + GeoConversions.CoordinatesToString(WorldCoordinates));

			if(ScenePos.magnitude < MaxDistance) {

				needsUpdate = true;
	
				this.gameObject.SetActive(true);
			}
			else {
				ScapeLogging.Log(message: "OriginEvent() "+ name +" beyond max distance (" + ScenePos.magnitude + ")");
				
				this.gameObject.SetActive(false);
			}
		}

		void Awake() {

			ScapeLogging.Log(message: "OGeoAnchor::Awake " + this.gameObject.name);

			// this.gameObject.SetActive(false);

			GeoWorldRoot.GetInstance().RegisterGeoEvent(this.OriginEvent);
			
			WorldCoordinates = new Coordinates{longitude = Longitude, latitude = Latitude};
			WorldPos = GeoConversions.VectorFromCoordinates(WorldCoordinates);
		}

		void Update() {
			if(needsUpdate) {
				this.gameObject.transform.position = new Vector3(ScenePos.x, 0.0f, ScenePos.y);	

				ScapeLogging.Log(message: "OriginEvent() set position = " + this.gameObject.transform.localPosition);	
						
				needsUpdate = false;
			}
		}
	} 
}