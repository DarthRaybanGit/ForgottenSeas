﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ClassChoice : NetworkBehaviour {

    public bool m_pressed = false;

    public void chosePlayerClass(int n)
    {


        if (n < 4)
        {
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("GameManager"))
            {

                GameManager gm = g.GetComponent<GameManager>();
                gm.setLocalClass(n);
                //gm.m_LocalClassViewer.GetComponent<ClassShower>().RpcSetFlag();

            }
        }


    }

}
