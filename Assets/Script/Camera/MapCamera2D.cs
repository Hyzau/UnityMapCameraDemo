using UnityEngine;

/*
 * A simple script which allow the camera to move over a 2D map
 * It handle both mouse and touch input
 * TODO : calculate camera frustum and enforce boundary based on frustrum instead of camera position
 */ 
public class MapCamera2D : MonoBehaviour
{
    private float defaultZ;
    private Vector3 startPos, movePos, currentPos, wantedPos;
    private float currentZoom, wantedZoom;
    private float aspectRatio;
    private Camera mCamera;
    public bool ignoreClick = false;
    public float damping = 5.0f;
    public float zoomSpeed = 1.0f;
    public float moveSpeed = 3.0f;
    // The first element is the min, the second is the max
    private Vector2 xLimitZoomed;
    private Vector2 yLimitZoomed;
    public Vector2 xlimit = new Vector2(0, 100);
    public Vector2 ylimit = new Vector2(0, 100);
    
    public Vector2 zoomLimit = new Vector2(1, 15);
    


    void Awake()
    {
        mCamera = Camera.main;
        this.defaultZ = mCamera.transform.position.z;  // Distance camera is above map
        this.currentPos = mCamera.transform.position;
        this.wantedPos = this.currentPos;
        this.currentZoom = mCamera.orthographicSize;
        this.aspectRatio = mCamera.aspect;
        this.wantedZoom = this.currentZoom;
        this.xLimitZoomed = new Vector2();
        this.yLimitZoomed = new Vector2();
        this.calculateZoomedLimit();
    }

    public void calculateZoomedLimit()
    {
        float currentDiffX = this.aspectRatio * this.currentZoom;
        this.xLimitZoomed.x = this.xlimit.x + currentDiffX;
        this.xLimitZoomed.y = this.xlimit.y - currentDiffX;
        float currentDiffY = this.currentZoom;

        this.yLimitZoomed.x = this.ylimit.x + currentDiffY;
        this.yLimitZoomed.y = this.ylimit.y - currentDiffY;
    }

	// this function overrides any boundaries and should be used for debugging
    public void setPosition(Vector3 pos)
    {
        pos.z = this.defaultZ;
        this.currentPos = pos;
        this.wantedPos = pos;
        mCamera.transform.position = pos;
    }
	
	
	public void setPositionWithinBounds(Vector3 pos)
    {
        this.applyTransform(pos);
    }

    public Vector3 getPosition()
    {
        return this.currentPos;
    }

    public void setZoom(float zoom)
    {
        this.currentZoom = zoom;
        this.wantedZoom = zoom;
        mCamera.orthographicSize = zoom;
        this.calculateZoomedLimit();
        this.applyTransform(this.currentPos);// in case we zoom in a corner, we need to move the camera within the new boundaries
    }

    public float getZoom()
    {
        return this.currentZoom;
    }

    /*
     * A public function to move the camera with another script.
     * Any move input cancel the movement
     */
    public void moveTo(Vector3 target)
    {
        target.x = Mathf.Clamp(target.x , xLimitZoomed.x, xLimitZoomed.y);
        target.y = Mathf.Clamp(target.y, yLimitZoomed.x, yLimitZoomed.y);
        target.z = this.defaultZ; // avoid Z movement
        this.wantedPos = target;
    }
	
	public void moveTo(Vector2 target)
    {
        target.x = Mathf.Clamp(target.x , xLimitZoomed.x, xLimitZoomed.y);
        target.y = Mathf.Clamp(target.y, yLimitZoomed.x, yLimitZoomed.y);
        this.wantedPos = target;
		this.wantedPos.z = this.defaultZ; // avoid Z movement
    }

    /*
     * A public function to zoom the camera with another script.
     * Any zoom input cancel the zoom
     */
    public void zoomTo(float target)
    {
        target = Mathf.Clamp(target, zoomLimit.x, zoomLimit.y);
        this.wantedZoom = target;
    }

    /*
     * Immediatly apply a Zoom to the camera.
     * If the camera was previously zooming through wantedZoom, cancel it.
     */
    private void applyZoom(float diff)
    {
        mCamera.orthographicSize = Mathf.Clamp(mCamera.orthographicSize - diff, zoomLimit.x, zoomLimit.y);
        this.currentZoom = mCamera.orthographicSize;
        this.wantedZoom = this.currentZoom;
        this.calculateZoomedLimit();
        this.applyTransform(this.currentPos);// in case we zoom in a corner, we need to move the camera within the new boundaries
    }

    /*
     * Immediatly apply a transform to the camera.
     * If the camera was previously moving through wantedPos, cancel it.
     */
    private void applyTransform(Vector3 newPos)
    {
        newPos.x = Mathf.Clamp(newPos.x, xLimitZoomed.x, xLimitZoomed.y);
        newPos.y = Mathf.Clamp(newPos.y, yLimitZoomed.x, yLimitZoomed.y);
		newPos.z = this.defaultZ;
        mCamera.transform.position = newPos;
        // Below line are used to cancel any automatic move on use input
        this.currentPos = newPos;
        this.wantedPos = newPos;
    }

    /*
     * Animate the movement to a certain position with an uniform amount of time
     */
    private void moveCameraToTarget()
    {
        mCamera.transform.position = Vector3.Lerp(mCamera.transform.position, this.wantedPos, (Time.deltaTime * damping));
        this.currentPos = mCamera.transform.position;
    }

    /*
     * Animate the movement to a certain position with an uniform amount of time
     */
    private void zoomCameraToTarget()
    {
        mCamera.orthographicSize = Mathf.Lerp(this.currentZoom, this.wantedZoom, (Time.deltaTime * damping));
        this.currentZoom = mCamera.orthographicSize;
    }

    void Update()
    {
        // If another script want to move th camera
        if (Vector3.Distance(this.wantedPos, this.currentPos) > 0.1f)
        {
            this.moveCameraToTarget();
        }
        if (Mathf.Abs(this.wantedZoom - this.currentZoom) > 0.1f)
        {
            this.zoomCameraToTarget();
        }
        if (this.ignoreClick == true)
            return;
        // Handle Mouse movement
        // Because Unity consider touch as mouse bouton, we must add a touchCount here
        if (Input.GetMouseButtonDown(0) && Input.touchCount == 0)
        {
            startPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && Input.touchCount == 0)
        {
            movePos = Input.mousePosition - startPos;
            float magnitude = (movePos).magnitude;
            if (magnitude > 0.1f)
            {
                // To avoid acceleration as the button is pressed, we need to normalize movePos
                movePos.Normalize();
                movePos = movePos * Time.deltaTime * moveSpeed * (magnitude / 300.0f);
                Vector3 tmp = new Vector3(mCamera.transform.position.x - movePos.x, mCamera.transform.position.y - movePos.y, defaultZ);
                applyTransform(tmp);
            }
            
        }
        // Handle Touch Movement
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            startPos = Input.GetTouch(0).position;
        }
        else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            movePos = new Vector3(Input.GetTouch(0).position.x - startPos.x, Input.GetTouch(0).position.y - startPos.y, startPos.z);
            float magnitude = (movePos).magnitude;
            if (magnitude > 0.1f)
            {
                movePos.Normalize();
                movePos = movePos * Time.deltaTime * moveSpeed * (magnitude / 300.0f);
                Vector3 tmp = new Vector3(mCamera.transform.position.x - movePos.x, mCamera.transform.position.y - movePos.y, defaultZ);
                this.applyTransform(tmp);
            }
        }
        else if (Input.touchCount == 2) // pinch to zoom
        {
            Touch tZero = Input.GetTouch(0);
            Touch tOne = Input.GetTouch(1);

            Vector2 zeroDelta = tZero.position - tZero.deltaPosition;
            Vector2 oneDelta = tOne.position - tOne.deltaPosition;
            float initialMagnitude = (zeroDelta - oneDelta).magnitude;
            float currentMagnitude = (tZero.position - tOne.position).magnitude;

            float diff = currentMagnitude - initialMagnitude;
            this.applyZoom(diff * 0.005f * zoomSpeed);
        }
        // Handle mouse wheel zoom
        float mWheel = Input.GetAxis("Mouse ScrollWheel");
        if (mWheel != 0)
        {
            this.applyZoom(mWheel * 4 * zoomSpeed);
        }
        
    }
}
