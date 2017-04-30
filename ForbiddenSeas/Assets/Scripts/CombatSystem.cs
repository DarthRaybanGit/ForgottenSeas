﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CombatSystem : NetworkBehaviour
{
    [ClientRpc]
    void RpcTakenDamage(string o)
    {
            Debug.Log(gameObject.name + "Colpito " + o);
    }

    void mainAttack()
    {
    }

    void SpecialAttack()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (isServer)
        {
            Debug.Log(gameObject.name + "Preso danno");
            //gameObject.GetComponent<FlagshipStatus>().shipClass
            other.GetComponent<FlagshipStatus>().takeDamage(100);
            RpcTakenDamage(other.name);
        }
    }

}
