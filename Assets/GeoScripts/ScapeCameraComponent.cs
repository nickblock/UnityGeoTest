using UnityEngine;
using UnityEngine.XR;

#if UNITY_ANDROID && !UNITY_EDITOR
using GoogleARCoreInternal;
#endif


namespace ScapeKitUnity
{

    public class ScapeCameraComponent : MonoBehaviour
    {
    	public Camera mCamera;

    	private bool isFetching = false;
    	private bool inited = false;

    	private Vector3 PositionAtScapeMeasurements;
    	private Quaternion RotationAtScapeMeasurements;

    	private Quaternion ScapeDirectionFix;

    	public float timeout = 1000.0f;
    	private float currentTime = 0.0f;

        public float MotionSpeed = 1.0f;
        public float RotationSpeed = 1.0f;
        private float YRotation = 0.0f;

        public float StartLongitude = 0.0f;
        public float StartLatitude = 0.0f;

        private void initScape()
        {
            // ScapeClient.Instance.WithResApiKey().StartClient();

            // ScapeClient.Instance.ScapeSession.ScapeMeasurementsEvent += OnScapeMeasurementsEvent;
            // ScapeClient.Instance.ScapeSession.ScapeSessionErrorEvent += OnScapeSessionError;

            inited = true;
        }

        private void GetMeasurements()
        {
            if(!inited)
            {
                initScape();
            }

            PositionAtScapeMeasurements = mCamera.transform.localPosition;
            RotationAtScapeMeasurements = mCamera.transform.localRotation;

            // ScapeClient.Instance.ScapeSession.GetMeasurements(GeoSourceType.rawSensorsAndScapeVisionEngine);
        }

        void Awake() { 

        	if(!mCamera) {
        		mCamera = Camera.main;
        	}
        	ScapeDirectionFix = Quaternion.AngleAxis(0.0f, Vector3.up);

            GeoWorldRoot.GetInstance().SetWorldOrigin(new Coordinates() {
                longitude = StartLongitude,
                latitude = StartLatitude
            });
        }

    	void Update()
    	{
    		if(!isFetching) 
    		{
    			isFetching = true;

    			GetMeasurements();
    		}
    		else {
    			currentTime += Time.deltaTime;
    			if(currentTime > timeout) {
    				currentTime = 0;
    				isFetching = false;
    			}
    		}
    		UpdateCameraFromARCore();
            CamControls();
    	}

        void CamControls() {

            YRotation += Input.GetAxis ("Horizontal") * RotationSpeed;

            Quaternion rot = Quaternion.AngleAxis(YRotation, Vector3.up);

            float dirSpeed = Input.GetAxis ("Vertical") * MotionSpeed;
            Vector3 dir = rot * new Vector3(0,0,dirSpeed);

            float flyUp = Input.GetAxis("FlyUp") * MotionSpeed;
            dir += new Vector3(0, flyUp, 0);

            mCamera.transform.localPosition += dir;
            mCamera.transform.localRotation = rot;
        }

    	void UpdateCameraFromARCore() {

#if UNITY_ANDROID && !UNITY_EDITOR
            mCamera.transform.localPosition = GoogleARCore.Frame.Pose.position;
            mCamera.transform.localRotation = ScapeDirectionFix * GoogleARCore.Frame.Pose.rotation;
#endif
    	}

        void SynchronizeARCamera(ScapeMeasurements scapeMeasurements) 
        {
            Coordinates LocalCoordinates = GeoConversions.CoordinatesFromVector(new Vector2(PositionAtScapeMeasurements.x, PositionAtScapeMeasurements.z));
            Coordinates OriginCoordinates = new Coordinates() {
                longitude = scapeMeasurements.coordinates.longitude - LocalCoordinates.longitude,
                latitude = scapeMeasurements.coordinates.latitude - LocalCoordinates.latitude
            };

            ScapeLogging.Log(message: "SynchronizeARCamera() scapecoords = " + GeoConversions.CoordinatesToString(scapeMeasurements.coordinates));
            ScapeLogging.Log(message: "SynchronizeARCamera() localcoords = " + GeoConversions.CoordinatesToString(LocalCoordinates));
            ScapeLogging.Log(message: "SynchronizeARCamera() origincoords = " + GeoConversions.CoordinatesToString(OriginCoordinates));

            GeoWorldRoot.GetInstance().SetWorldOrigin(OriginCoordinates);

    		Quaternion worldEulerRotation = new Quaternion((float)scapeMeasurements.orientation.x, 
    										(float)scapeMeasurements.orientation.y, 
    										(float)scapeMeasurements.orientation.z, 
    										(float)scapeMeasurements.orientation.w);

    		ScapeDirectionFix = worldEulerRotation * Quaternion.Inverse(RotationAtScapeMeasurements);
    		ScapeLogging.Log(message: "SynchronizeARCamera() ScapeDirectionFix = " + ScapeDirectionFix);
    	}

        void OnScapeMeasurementsEvent(ScapeMeasurements scapeMeasurements)
        {
        	if(scapeMeasurements.measurementsStatus == ScapeMeasurementStatus.ResultsFound) 
        	{
				SynchronizeARCamera(scapeMeasurements);
        	}	
        	isFetching = false;
        }

        void OnScapeSessionError(ScapeSessionError scapeDetails)
        {
        	isFetching = false;
        }
    }
}