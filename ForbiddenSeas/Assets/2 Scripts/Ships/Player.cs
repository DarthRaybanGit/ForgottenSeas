﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour {

    public GameObject[] m_AdmiralList = new GameObject[4];
    public GameObject m_LocalCamera;
    public int playerId;

    public bool m_HasTreasure = false;
    public GameObject m_LocalTreasure;

    public Vector3 m_SpawnPoint;
    public Text m_reputationTextUI;
    public Text m_scoreTextUI;

    [SyncVar]
    public int m_score = 0;
    [SyncVar]
    public int m_Class = 0;

    public void Start()
    {
        if (!isLocalPlayer)
        {
            Destroy(m_LocalCamera.GetComponent<AudioListener>());
            return;
        }
        if (isLocalPlayer || isServer)
        {
            Physics.IgnoreLayerCollision(10, 10);
        }
    }

    public override void OnStartLocalPlayer()
    {
        if (!isServer)
        {
            if (isLocalPlayer)
            {
                //Camera.main.gameObject.SetActive(true);
                /*m_LocalCamera.tag = "MainCamera";
                m_LocalCamera.SetActive(true);
                m_LocalCamera.GetComponent<Camera>().enabled = true;
                Debug.Log("Ho finito di settare la camera.");*/
                LocalGameManager.Instance.m_LocalPlayer = gameObject;
                CmdStartGeneralLoop((int)this.netId.Value);
                m_SpawnPoint = transform.position;
                LocalGameManager.Instance.m_GameIsStarted = true;
                m_reputationTextUI = GameObject.FindGameObjectWithTag("ReputationUI").GetComponent<Text>();
                m_scoreTextUI = GameObject.FindGameObjectWithTag("ScoreUI").GetComponent<Text>();
            }

        }

    }



    public override void OnStartServer()
    {
        m_SpawnPoint = transform.position;
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



            StartCoroutine(LocalGameManager.Instance.c_WaitUntilEveryPlayersOnline());
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
    public void CatchAPowerUp(PowerUP p)
    {
        LocalGameManager.Instance.m_PowerUp[(int)p] = false;
        switch (p)
        {
            case PowerUP.REGEN:
                break;
            case PowerUP.DAMAGE_UP:
                break;
            case PowerUP.SPEED_UP:
                break;
        }
    }


    [Server]
    public void CatchTheTreasure()
    {
        if (!LocalGameManager.Instance.m_Treasure.activeSelf)
            return;

        LocalGameManager.Instance.m_Treasure.SetActive(false);
        Debug.Log("Il player " + playerId + " ha preso il tesoro!");

        //Debug.Log("Gli sto facendo prendere il tesoro!");
        GetComponent<FlagshipStatus>().m_reputation += ReputationValues.TREASURE;
        TargetRpcUpdateReputationUI(GetComponent<NetworkIdentity>().connectionToClient);

        m_HasTreasure = true;
        LocalGameManager.Instance.RpcNotifyNewTreasureOwner(netId, LocalGameManager.Instance.m_Treasure.GetComponent<NetworkIdentity>().netId);
        StartCoroutine(yohohoBarGrow(this));


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

        m_score++;
        GetComponent<FlagshipStatus>().m_reputation += ReputationValues.ARRH;
        TargetRpcUpdateReputationUI(GetComponent<NetworkIdentity>().connectionToClient);
        TargetRpcUpdateScoreUI(GetComponent<NetworkIdentity>().connectionToClient);
        RpcHideTreasure();
        StartCoroutine(LocalGameManager.Instance.c_RespawnTreasure());
    }

    [TargetRpc]
    public void TargetRpcUpdateReputationUI(NetworkConnection u)
    {
        StartCoroutine(UpdateReputationUI());
    }

    IEnumerator UpdateReputationUI()
    {
        yield return new WaitForSeconds(0.1f);
        m_reputationTextUI.text = "Reputation " + GetComponent<FlagshipStatus>().m_reputation.ToString();
    }

    [TargetRpc]
    public void TargetRpcUpdateScoreUI(NetworkConnection u)
    {
        StartCoroutine(UpdateScoreUI());
    }

    IEnumerator UpdateScoreUI()
    {
        yield return new WaitForSeconds(0.1f);
        m_scoreTextUI.text = "Score " + m_score.ToString();
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

    public bool isThisPlayerLocal()
    {
        return isLocalPlayer;
    }
}
