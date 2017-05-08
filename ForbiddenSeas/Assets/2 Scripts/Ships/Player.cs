﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour {

    public GameObject[] m_AdmiralList = new GameObject[4];
    public GameObject m_LocalCamera;
    public int playerId;

    public bool m_HasTreasure = false;
    public GameObject m_LocalTreasure;


    [SyncVar]
    public int m_Class = 0;

    public void Start()
    {
        if (!isLocalPlayer)
        {
            Destroy(m_LocalCamera.GetComponent<AudioListener>());
            return;
        }
        else
        {

        }
    }

    public override void OnStartLocalPlayer()
    {
        if (!isServer)
        {
            if (isLocalPlayer)
            {
                Camera.main.gameObject.SetActive(false);
                m_LocalCamera.tag = "MainCamera";
                m_LocalCamera.SetActive(true);
                m_LocalCamera.GetComponent<Camera>().enabled = true;
                Debug.Log("Ho finito di settare la camera.");
                CmdStartGeneralLoop((int)this.netId.Value);
                LocalGameManager.Instance.m_GameIsStarted = true;
            }

        }
    }



    public override void OnStartServer()
    {
        LocalGameManager.Instance.m_GameIsStarted = true;
    }

    public void SetClass(int playerClass)
    {
        m_Class = playerClass;
    }


    //Funzione per richiedere al server di far partire il Loop di gioco.
    //Il primo player che spawna all'interno dell'arena farà partire il gioco.

    //Problemi --> Se uno lagga e la scena viene caricata dopo un certo tot? Parte con il gioco già startato e sarà quindi svantaggiato?
    //Soluzione: Inserire un countdown in cui il server aspetta il check per tutti i player e in quel caso far partire subito il gioco?
    //Se entro quel countdown tutti i player non saranno entrati in partita riporterebbe tutti alla Lobby.

    [Command]
    public void CmdStartGeneralLoop(int connectionID)
    {
        ImOnline(connectionID);
        if (LocalGameManager.Instance.m_GameIsStarted && !LocalGameManager.Instance.m_GameGeneralLoopIsStarted)
        {
            Debug.Log("Sono il primo! Aspettiamo gli altri prima di iniziare il game!");
            LocalGameManager.Instance.m_GameGeneralLoopIsStarted = true;

            foreach(int i in OnlineManager.s_Singleton.currentPlayers.Keys)
            {
                Debug.Log("è presente il Player " + i);
            }

            int[] to_Send = new int[OnlineManager.s_Singleton.currentPlayers.Keys.Count];
            int[] to_SendIds = new int[OnlineManager.s_Singleton.currentPlayers.Keys.Count];

            int count = 0;

            foreach (int i in OnlineManager.s_Singleton.currentPlayers.Keys)
            {
                to_Send[count] = i;
                to_SendIds[count] = OnlineManager.s_Singleton.currentPlayers[i][(int)PlayerInfo.ID];
                count++;
            }




            StartCoroutine(LocalGameManager.Instance.c_WaitUntilEveryPlayersOnline(to_Send, to_SendIds));
        }

    }

    public void ImOnline(int connectionID)
    {
        Debug.Log("Sta notificando il player " + connectionID);

        foreach(int i in OnlineManager.s_Singleton.currentPlayers.Keys)
        {
            if(OnlineManager.s_Singleton.currentPlayers[i][(int) PlayerInfo.ID] == connectionID)
                OnlineManager.s_Singleton.currentPlayers[i][(int)PlayerInfo.IS_LOADED] = 1;
        }
    }


    //Funzione per richiedere al server il timestamp con il quale sincronizzarsi.
    [Command]
    public void CmdAskForCurrentTime()
    {
        LocalGameManager.Instance.m_serverTimeSended = true;
        LocalGameManager.Instance.RpcNotifyServerTime(Time.timeSinceLevelLoad);
    }

    [Server]
    public void CatchTheTreasure(NetworkInstanceId playerId)
    {
        if (!LocalGameManager.Instance.m_Treasure.activeSelf)
            return;

        LocalGameManager.Instance.m_Treasure.SetActive(false);
        Debug.Log("Il player " + playerId + " ha preso il tesoro!");


        GameObject g = NetworkServer.FindLocalObject(playerId);
        if (g)
        {
            if (g.GetComponent<Player>())
            {
                //Debug.Log("Gli sto facendo prendere il tesoro!");
                Player pl = g.GetComponent<Player>();
                pl.m_HasTreasure = true;
                LocalGameManager.Instance.RpcNotifyNewTreasureOwner((int)playerId.Value, LocalGameManager.Instance.m_Treasure.GetComponent<NetworkIdentity>().netId);
                StartCoroutine(yohohoBarGrow(pl));
            }

        }

    }

    [Server]
    public IEnumerator yohohoBarGrow(Player pl)
    {

        while (pl.m_HasTreasure && pl.gameObject.GetComponent<FlagshipStatus>().m_yohoho < 100)
        {

            yield return new WaitForSeconds((int)FixedDelayInGame.YOHOHO_UPDATE_INTERVAL);

            pl.gameObject.GetComponent<FlagshipStatus>().m_yohoho += 100 / (float)FixedDelayInGame.YOHOHO_FULLFY_SPAN;
            if (pl.gameObject.GetComponent<FlagshipStatus>().m_yohoho > 100)
                pl.gameObject.GetComponent<FlagshipStatus>().m_yohoho = 100;

        }
    }

    [Server]
    public IEnumerator LostTheTreasure()
    {
        Vector3 futureSpawn = gameObject.transform.position + Vector3.forward;
        RpcHideTreasure();
        Destroy(LocalGameManager.Instance.m_Treasure);
        yield return new WaitForSeconds(0.5f);

        Destroy(LocalGameManager.Instance.m_Treasure);
        LocalGameManager.Instance.m_Treasure = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.TREASURE]);
        LocalGameManager.Instance.m_Treasure.transform.position = futureSpawn;

        NetworkServer.Spawn(LocalGameManager.Instance.m_Treasure);

    }

    [Server]
    public void ScoreAnARRH()
    {
        Debug.Log("Player " + (int)netId.Value + " ha segnato un ARRH!");

        //Aumenta punteggi etc

        RpcHideTreasure();
        StartCoroutine(LocalGameManager.Instance.c_RespawnTreasure());
    }

    [ClientRpc]
    public void RpcHideTreasure()
    {
        if (m_HasTreasure)
        {
            m_HasTreasure = false;
            m_LocalTreasure.SetActive(false);
            LocalGameManager.Instance.m_TreasureIsInGame = false;
        }
    }

    public int GetPlayerId()
    {
        return playerId;
    }
}