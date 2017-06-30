﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KrakenAI : MonoBehaviour {

	public GameObject players;
	public Vector3 targetPos;
	public Quaternion DesRot;
	public float speed= 0.1f;
	public bool onMove=false;
	public bool onChase = false;
	private Rigidbody rb;
	public float SpeedFactor = 1.2f;
	public float ActualSpeed =0f;
	public float UnderwaterSpeed = 7f;
	public float AfloatSpeed = 4f;
	public float AttackRange = 15f;
	public float ChaseRange = 30f;
	public bool underwater = true;
	public float maxSpeed;
	public float Acceleration = 0.03f;
	public float deceleration = 0.5f;

	// Use this for initialization
	void Start () {
		//StartCoroutine (PlayerPos());
		targetPos=transform.position;
		DesRot = transform.rotation;
		rb = GetComponent<Rigidbody> ();
	}
		
	void FixedUpdate () {
		if (!onMove && !onChase)			//guarda alla posizione dove deve andare
		{
			StartCoroutine (TargetPos ());
			onMove = true;
		}

		if (underwater)						//imposto la maxspeed in base allo stato attuale
			maxSpeed = UnderwaterSpeed;
		else
			maxSpeed = AfloatSpeed;
		
		if (ActualSpeed < maxSpeed )		//lo faccio muovere
			ActualSpeed = Mathf.Lerp (ActualSpeed, maxSpeed , Acceleration);
		else
		{
			if (ActualSpeed > 0.5f)
				ActualSpeed = Mathf.Lerp (ActualSpeed, maxSpeed , deceleration);
			else
				ActualSpeed = 0;
		}

		rb.AddForce(transform.forward * -1 * ActualSpeed * SpeedFactor);
		rb.velocity = transform.forward * -1 * rb.velocity.magnitude;
		DesRot=Quaternion.LookRotation(targetPos);
		transform.rotation=Quaternion.Lerp(transform.rotation,DesRot,Time.deltaTime*20f);
		transform.position = Vector3.Lerp (transform.position, targetPos, speed * Time.deltaTime);
	}

	IEnumerator TargetPos()
	{
		transform.LookAt(players);
		targetPos = players.transform.position;
	}

}
