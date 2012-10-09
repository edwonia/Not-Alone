using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour
{
	public int zOffset;
	
	private int mouseFingerDown = -1;		//neither!
	private int previousFinger = -1;		//no difference
	
	private Vector2 prevTouchPos = Vector2.zero;
	
	private bool mouseIsMovingWhileDown = false;
	private ArrayList currentMousePoints = new ArrayList();
	private float previousMangle = 0.0f;
	
	public tk2dAnimatedSprite touchAnim;
	
	public bool doLinkInk = false;
	public bool isLocalPlayer = true;
	public bool isPlayer1 = false;
	
	private string[] touchAnimations = new string[3];
	
	public int MouseFingerDown()
	{
		return mouseFingerDown;
	}
	
	void Start()
	{
		if(Network.isClient && gameObject.tag == "PLAYER1")
		{
			isLocalPlayer = false;
		}
		if(Network.isServer && gameObject.tag == "PLAYER2")
		{
			isLocalPlayer = false;
		}
		
		if(!isLocalPlayer)
		{
			touchAnimations[0] = "fingerprintBeginAnim";
			touchAnimations[1] = "fingerprintLoopAnim";
			touchAnimations[2] = "fingerprintEndAnim";
		}
		else
		{
			touchAnimations[0] = "touchBeginAnim";
			touchAnimations[1] = "touchLoopAnim";
			touchAnimations[2] = "touchEndAnim";	
		}
	
		if (touchAnim != null)
			touchAnim.animationCompleteDelegate = AnimationComplete;					

		DontDestroyOnLoad(this);
		//print("(start) touch with id: " + networkView.viewID);
		//particlesPS = GameObject.Find("Particles").GetComponent<ParticleSystem>();
		//particlesTF = GameObject.Find("Particles").GetComponent<Transform>();//		
		//particlesPS.particleSystem.enableEmission = false;
	}
	
	void OnEnable()
	{
		networkView.observed = this;
	}
	
	void OnDisable()
	{

	}
	
	void Update()
	{
		if(previousFinger != mouseFingerDown)
		{
			if(mouseFingerDown != -1)
			{
				if(touchAnim != null)
					touchAnim.Play(touchAnimations[0]);
			}
			if(mouseFingerDown == -1)
			{
				// play end animation
				if (touchAnim != null)
					touchAnim.Play(touchAnimations[2]);
			}
		}
		
		float centerx = 0.5f;
		float centery = 0.5f;
		
		float xdiffCur = centerx - prevTouchPos.x;
		float ydiffCur = centery - prevTouchPos.y;
		float xdiffOld = centerx - transform.position.x;
		float ydiffOld = centery - transform.position.y;
		
		float angleOld = ToDegrees((float)System.Math.Atan2(ydiffOld, xdiffOld));
		float angleCur = ToDegrees((float)System.Math.Atan2(ydiffCur, xdiffCur));
		
		touchAnim.transform.Rotate(touchAnim.transform.forward, (angleOld-angleCur));
		prevTouchPos.x = transform.position.x;
		prevTouchPos.y = transform.position.y;

		previousFinger = mouseFingerDown;		
	}
	
	// plays the looping animation after the begin animation ends
	public void AnimationComplete (tk2dAnimatedSprite touchAnim, int clipId)
	{
		switch (clipId)
		{
		case 0:
			if (touchAnim != null)
				touchAnim.Play(touchAnimations[1]); break;
		}
	}
	
	public void UpdateAgainstBlackHole(BlackHoleScript blackHole)
	{
		if(mouseIsMovingWhileDown && blackHole != null)
		{
			Camera camcam = Camera.main;
			
			Vector3 blackHoleCenter = camcam.WorldToScreenPoint(blackHole.transform.position);
			
			float compundedMangle = 0.0f;
            float totalmangle = 0.0f;

            for (int i = 1; i < currentMousePoints.Count; ++i)
            {
                Vector2 old = (Vector2)currentMousePoints[i - 1];
                Vector2 cur = (Vector2)currentMousePoints[i];

                float yDistCur = cur.y - blackHoleCenter.y;
                float xDistCur = cur.x - blackHoleCenter.x;

                float yDistOld = old.y - blackHoleCenter.y;
                float xDistOld = old.x - blackHoleCenter.x;

                float maxRadius = 150;
                float minRadius = 30;

                float curLen = (float)System.Math.Sqrt((xDistCur * xDistCur) + (yDistCur * yDistCur));
                //float oldLen = (float)System.Math.Sqrt((xDistOld * xDistOld) + (yDistOld * yDistOld));

                if (curLen > minRadius && curLen < maxRadius)
                {
                    float degsCur = ToDegrees((float)System.Math.Atan2(yDistCur, xDistCur));
                    float degresOld = ToDegrees((float)System.Math.Atan2(yDistOld, xDistOld));

                    if (System.Math.Sign(degresOld) == System.Math.Sign(degsCur))
                    {
                        compundedMangle += System.Math.Abs(System.Math.Abs(degsCur) - System.Math.Abs(degresOld));
                        totalmangle += degsCur - degresOld;
                    }
                }
            }		
			
			int playerNm = 2;
			if(isPlayer1)
				playerNm = 1;
			
			blackHole.AddToRotationSpeed((totalmangle-previousMangle) * 0.4f, playerNm);
			previousMangle = totalmangle;
		}		
	}
	
	public float ToDegrees(float radians)
	{
		return (float)(radians * (180.0 / 3.14159265359f));
	}
	
	void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info)
	{
		int mouseState = mouseFingerDown;
		Vector3 pos = transform.position;
		
		if(stream.isWriting)
		{			
			stream.Serialize(ref pos);
			stream.Serialize(ref mouseState);
		}
		else if(stream.isReading)
		{
			stream.Serialize(ref pos);
			stream.Serialize(ref mouseState);			
		}
		
		mouseFingerDown = mouseState;
		transform.position = pos;
	}
	
	
	#region Finger Gestures
	
	public void OnPlayerFingerDown (int finger, Vector2 pos)
	{		
		audio.Play();
		currentMousePoints.Add(pos);
		transform.position = Camera.main.ScreenToWorldPoint(new Vector3(pos.x, pos.y, zOffset));
		mouseFingerDown = finger;	//either 0, or 1 i believe..
		mouseIsMovingWhileDown = true;
	}
	
	public void OnPlayerFingerMove (int finger, Vector2 pos)
	{
		//DebugStreamer.message = "mouse move pos: " + pos.ToString();
		currentMousePoints.Add(pos);
		transform.position = Camera.main.ScreenToWorldPoint(new Vector3(pos.x, pos.y, zOffset));
		mouseFingerDown = finger;	//either 0, or 1 i believe..
	}
	
	public void OnPlayerFingerUp (int finger, Vector2 pos, float timeHeldDown)
	{
		mouseIsMovingWhileDown = false;
		mouseFingerDown = -1;	//up!
		previousMangle = 0;
		currentMousePoints.Clear();
	}
	
	#endregion
}
