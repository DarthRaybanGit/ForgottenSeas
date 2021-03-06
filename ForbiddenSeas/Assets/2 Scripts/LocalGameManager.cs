﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LocalGameManager : NetworkBehaviour
{

    public static LocalGameManager Instance = null;

    public GameObject m_LocalPlayer;
    public bool m_PlayerSetted = false;
    public bool m_PlayerSettedRemote = false;

    public Dictionary<int,int> m_PlayersID;
    public GameObject[] m_Players;
    public bool m_PlayerRegistered = false;


    public GameObject[] m_LocalClassViewer;

    public bool m_GameIsStarted = false;
    public bool m_GameGeneralLoopIsStarted = false;

    //Variabili per la sincronizzazione del tempo di gioco
    public bool m_serverTimeSended = false;
    public bool m_timeIsSynced = false;
    public float m_gapFromStart;
    public float m_ServerOffsetTime;
    public float m_GameStartTime;

    public static float m_MatchEndTime;

	public AudioClip m_TreasureClip;
    public GameObject m_Treasure;
    public bool m_TreasureIsInGame = false;
	public bool m_TreasureOwned = false;
    public GameObject[] m_Ports;

	public AudioClip m_WinClip;
	public AudioClip m_LoseClip;

    public GameObject m_CanvasHUD;
    public GameObject m_CanvasEtichette;


    public bool m_CutIsPlaying = false;
    public bool m_IsWindowOver = false;
    public bool m_LoadingCompleted = false;

    public bool m_canAttack = false;

    public bool yohoho_icon = false;


    //Server

    public AudioClip m_CoinClip;
    public float m_CoinsRadius;
    public float m_CoinsDisplacement;
    public int m_CoinNumbers = 50;
    public GameObject[] m_Coins;
    public bool[] m_CoinsPresence;

    [SyncVar]
    public SyncListInt m_playerArrh = new SyncListInt();
    [SyncVar]
    public SyncListInt m_playerKills = new SyncListInt();
    [SyncVar]
    public SyncListInt m_playerDeaths = new SyncListInt();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        DontDestroyOnLoad(transform.gameObject);
        m_PlayersID = new Dictionary<int, int>();

    }

    public override void OnStartServer()
    {
        m_Ports = new GameObject[4];
    }


    [TargetRpc]
    public void TargetRpcNotifyClientConnection(NetworkConnection nc)
    {
        m_PlayerSettedRemote = true;
    }

    [ClientRpc]
    public void RpcNotifyPlayersInGame(NetworkInstanceId[] players)
    {
        StartCoroutine(registrazione(players));
    }

    IEnumerator registrazione(NetworkInstanceId[] play)
    {
        //Debug.Log("Localmente " + GameObject.FindGameObjectsWithTag("Player").Length);

        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Player").Length == play.Length);

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        LocalGameManager.Instance.m_Players = new GameObject[players.Length];

        //Debug.Log("Ho trovato " + players.Length + " giocatori. " + m_Players.Length);

        for(int i = 0; i < play.Length; i++)
        {
            //Debug.Log("ID: " + play[i] + " Object " + ClientScene.FindLocalObject(play[i]));
            LocalGameManager.Instance.m_Players[i] = ClientScene.FindLocalObject(play[i]);
            //LocalGameManager.Instance.m_Players[i].GetComponent<Player>().playerId = i;
        }

        m_PlayerRegistered = true;
    }


    //Funzioni public per la restituzione del gameobject dato l'ID

    public GameObject GetPlayer(int playerId)
    {
        if (!m_PlayerRegistered)
            return null;

        foreach(GameObject g in m_Players)
        {
            if (g && g.GetComponent<Player>())
                if (g.GetComponent<Player>().playerId == playerId)
                    return g;
        }

        return null;
    }

    [Server]
    public GameObject GetPlayerServer(int playerId)
    {
        foreach(GameObject g in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (g.GetComponent<Player>() && g.GetComponent<Player>().playerId == playerId)
                return g;
        }
        return null;
    }


    public GameObject GetPlayerFromNetID(NetworkInstanceId netID)
    {
        if (!m_PlayerRegistered)
            return null;

        foreach(GameObject g in m_Players)
        {
            if (g.GetComponent<NetworkIdentity>().netId == netID)
                return g;
        }

        return null;
    }

    //Funzioni per la sincronizzazione del timestamp del server sui clients.

    [ClientRpc]
    public void RpcNotifyServerTime(float time, bool first)
    {
        Debug.Log("Il server mi ha inviato il suo timing " + time);
        //m_ServerOffsetTime = time - Time.timeSinceLevelLoad;
        if (first)
        {
            m_gapFromStart = time;
            Debug.Log("Per caricare la scena ci ho messo " + m_gapFromStart);
        }

        m_ServerOffsetTime = time - Time.timeSinceLevelLoad;

        Debug.Log("Il mio offset dal server è diventato " + m_ServerOffsetTime);

        m_timeIsSynced = true;
        m_serverTimeSended = true;

        //ShowCountdown
    }

    public float syncedTime()
    {
        return isServer ? Time.timeSinceLevelLoad - m_GameStartTime : Time.timeSinceLevelLoad + m_ServerOffsetTime - m_gapFromStart;
    }


    /*
     *
     *  Co-routines per la gestione del Loop di Gioco
     *
     */

    [Server]
    public IEnumerator c_WaitForTreasure()
    {
        yield return new WaitForSeconds((int)FixedDelayInGame.TREASURE_FIRST_SPAWN);
        //Risincronizza il time per sicurezza
        //m_ServerOffsetTime = Time.timeSinceLevelLoad;
        LocalGameManager.Instance.RpcNotifyServerTime(Time.timeSinceLevelLoad, false);

        m_Treasure = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.TREASURE]);

        Debug.Log("Tesoro Spawn!!!");
        NetworkServer.Spawn(m_Treasure);
        GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().RpcAvvisoSpawnT();

        //Spawn dei Porti
        int count = 0;
        for(; count < m_Ports.Length; count++)
        {
            m_Ports[count] = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.PORTO], OnlineManager.s_Singleton.m_PortSpawnPosition[count].position, OnlineManager.s_Singleton.m_PortSpawnPosition[count].rotation);
        }


        foreach(GameObject g in m_Ports)
        {
            NetworkServer.Spawn(g, g.GetComponent<NetworkIdentity>().assetId);
        }

    }

    [Server]
    public IEnumerator c_LoopCoins()
    {
        m_Coins = new GameObject[m_CoinNumbers];
        m_CoinsPresence = new bool[m_CoinNumbers];

        List<int> toSpawn = searchWhichCoinToSpawn();

        Debug.Log("Coin SPAWN!!! " + toSpawn.Count);

        foreach (int i in toSpawn)
        {
            float z = Mathf.Sin(i) * m_CoinsRadius + Random.Range(-m_CoinsDisplacement, m_CoinsDisplacement);
            float x = Mathf.Cos(i) * m_CoinsRadius + Random.Range(-m_CoinsDisplacement, m_CoinsDisplacement);
            GameObject g = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.COIN], new Vector3(x, OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.COIN].transform.position.y, z), OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.COIN].transform.rotation);
            g.GetComponent<Coin>().m_IndexInPool = i;
            NetworkServer.Spawn(g, g.GetComponent<NetworkIdentity>().assetId);
            m_Coins[i] = g;
            m_CoinsPresence[i] = true;
        }

        while (LocalGameManager.Instance.m_GameIsStarted)
        {
            yield return new WaitForSeconds((int)FixedDelayInGame.COIN_SPAWN);
            //Risincronizza il time per sicurezza
            LocalGameManager.Instance.RpcNotifyServerTime(Time.timeSinceLevelLoad, false);


            toSpawn = searchWhichCoinToSpawn();

            Debug.Log("Coin SPAWN!!! " + toSpawn.Count);

            foreach (int i in toSpawn)
            {
                float z = Mathf.Sin(i) * m_CoinsRadius + Random.Range(-10, 10);
                float x = Mathf.Cos(i) * m_CoinsRadius + Random.Range(-10, 10);
                GameObject g = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.COIN], new Vector3(x, OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.COIN].transform.position.y, z), OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.COIN].transform.rotation);
                g.GetComponent<Coin>().m_IndexInPool = i;
                NetworkServer.Spawn(g, g.GetComponent<NetworkIdentity>().assetId);
                m_Coins[i] = g;
                m_CoinsPresence[i] = true;
            }

        }
    }

    private List<int> searchWhichCoinToSpawn()
    {
        List<int> monete = new List<int>();
        for (int i = 0; i < m_CoinsPresence.Length; i++)
        {
            if (!m_CoinsPresence[i])
                monete.Add(i);
        }
        return monete;
    }

    [Server]
    public IEnumerator c_LoopPowerUp(int delay, SpawnIndex which)
    {
        Debug.Log("Mi preparo a spawnare un power up " + delay);

        yield return new WaitForSeconds(delay);

        Debug.Log("Sto spawnando un power up");
        GameObject g = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)which], OnlineManager.s_Singleton.m_PowerUpSpawnPosition[(int) (which - SpawnIndex.REGEN)].position, OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)which].transform.rotation);

        NetworkServer.Spawn(g, g.GetComponent<NetworkIdentity>().assetId);
    }

    [Server]
    public IEnumerator c_LoopMines(int delay, int which)
    {
        Debug.Log("Mi preparo a spawnare una mina " + delay);
        yield return new WaitForSeconds(delay);
        Debug.Log("Sto spawnando una mina");
        GameObject g = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.MINA], OnlineManager.s_Singleton.m_MinesSpawnPosition[which].position, OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.MINA].transform.rotation);
        g.GetComponentInChildren<Mina>().which = which;
        NetworkServer.Spawn(g, g.GetComponent<NetworkIdentity>().assetId);
    }

    [Server]
    public IEnumerator c_RespawnTreasure()
    {
        yield return new WaitForSeconds((int)FixedDelayInGame.TREASURE_RESPAWN);
        Destroy(m_Treasure);
        m_Treasure = GameObject.Instantiate(OnlineManager.s_Singleton.spawnPrefabs.ToArray()[(int)SpawnIndex.TREASURE]);
        NetworkServer.Spawn(m_Treasure);
    }

    [Server]
    public IEnumerator c_WaitUntilEveryPlayersOnline()
    {
        float timestamp = Time.time;
        yield return new WaitUntil(() => OnlineManager.s_Singleton.EveryoneIsOnline());



        /*
        if (!OnlineManager.s_Singleton.EveryoneIsOnline())
        {
            //Se ci sono tre giocatori si inizia ugualmente
            //se no fa tornare tutti i player alla lobby
        }
        else
        {
        */
            NetworkInstanceId[] to_Send = new NetworkInstanceId[GameObject.FindGameObjectsWithTag("Player").Length];

            int count = 0;

            foreach (GameObject i in GameObject.FindGameObjectsWithTag("Player"))
            {
                to_Send[count] = i.GetComponent<NetworkIdentity>().netId;
                count++;
            }

            /*
            foreach (NetworkInstanceId i in to_Send)
            {
                Debug.Log("Sto per inviare " + i);
            }
            */


            //Start Game!
            Debug.Log("START GAME!!!");
            LocalGameManager.Instance.RpcNotifyPlayersInGame(to_Send);
        /*
        StartCoroutine(LocalGameManager.Instance.c_WaitForTreasure());
        StartCoroutine(LocalGameManager.Instance.c_LoopPowerUp());
        LocalGameManager.Instance.m_serverTimeSended = true;
        LocalGameManager.Instance.RpcNotifyServerTime(Time.timeSinceLevelLoad);
        */

        //}

        //inizializzo gli arrh dei players
        m_playerArrh.Add(0);
        m_playerArrh.Add(0);
        m_playerArrh.Add(0);
        m_playerArrh.Add(0);
        m_playerDeaths.Add(0);
        m_playerDeaths.Add(0);
        m_playerDeaths.Add(0);
        m_playerDeaths.Add(0);
        m_playerKills.Add(0);
        m_playerKills.Add(0);
        m_playerKills.Add(0);
        m_playerKills.Add(0);

    }


    public bool IsEveryPlayerRegistered()
    {
        //Debug.Log("Looking for Player: " + m_PlayerRegistered);
        if (m_PlayerRegistered)
        {
            foreach (GameObject g in m_Players)
            {
                if(!g || !g.activeSelf)
                    return false;
            }
        }
        else
        {
            return false;
        }
        return m_PlayerRegistered;
    }

    public int WhoAmI(GameObject me)
    {
        if (IsEveryPlayerRegistered())
        {
            return (me && me.GetComponent<Player>()) ? me.GetComponent<Player>().playerId : Symbols.PLAYER_NOT_SET;
        }
        else
            return Symbols.PLAYER_NOT_SET;
    }

    [Client]
    public GameObject WhoAsTheTreasure()
    {
        if (IsEveryPlayerRegistered())
        {
            foreach(GameObject g in m_Players)
            {
                if (g.GetComponent<Player>().m_HasTreasure)
                    return g;
            }
        }
        return null;
    }

    [ClientRpc]
    public void RpcNotifyNewTreasureOwner(NetworkInstanceId playerId, NetworkInstanceId treasure)
    {
        if (IsEveryPlayerRegistered())
        {
            GameObject g = GetPlayerFromNetID(playerId);
            Player pl = g ? g.GetComponent<Player>() : null;
            if (pl)
            {
                if (playerId == m_LocalPlayer.GetComponent<Player>().netId)
                {
                    GameObject.FindGameObjectWithTag("TreasureUI").GetComponent<Image>().enabled = true;
                    GameObject.FindGameObjectWithTag("Aboard").transform.GetChild(0).gameObject.SetActive(true);
					AudioSource Audio = GetComponent<AudioSource> ();
					Audio.PlayOneShot (m_TreasureClip, 0.75f);
                }
                pl.m_LocalTreasure.SetActive(true);
                pl.m_HasTreasure = true;
                GameObject tr = ClientScene.FindLocalObject(treasure);
                if (tr)
                {
                    Destroy(tr);
                }
            }
            else
            {
                Debug.Log("No player " + playerId + " trovato!");
            }
        }
    }

    public bool GameCanStart()
    {
        return !m_CutIsPlaying && !m_IsWindowOver && IsEveryPlayerRegistered();
    }

    public Sprite win;

    [ClientRpc]
    public void RpcPartitaConclusa(int id)
    {
        Debug.Log("Ha vinto il player " + id + ", " + GetPlayer(id).GetComponent<Player>().playerName);
        m_LocalPlayer.GetComponent<Animator>().SetFloat("Speed", 0f);
        m_LocalPlayer.GetComponent<CombatSystem>().enabled = false;
        m_LocalPlayer.GetComponent<MoveSimple>().enabled = false;

        GameObject.FindGameObjectWithTag("etichette").SetActive(false);
        GameObject.FindGameObjectWithTag("hud").SetActive(false);

        GameObject end = GameObject.FindGameObjectWithTag("end");
		AudioSource audio = GetComponent<AudioSource> ();
		Camera.main.gameObject.GetComponent<AudioSource>().Stop();

		if (m_LocalPlayer.GetComponent<Player> ().playerId == id)
		{
			end.transform.GetChild (0).GetComponent<Image> ().sprite = win;
			audio.PlayOneShot (m_WinClip);
		}
		else
			audio.PlayOneShot (m_LoseClip);
        end.GetComponent<Animation>().Play();
    }

    [ClientRpc]
    public void RpcToTheLobby()
    {
        GameObject g = GameObject.FindGameObjectWithTag("Finish");
        Utility.recursiveSetAlphaChannel(g.transform);
        g.transform.GetChild(0).gameObject.GetComponent<Image>().enabled = true;
        g.transform.GetChild(1).gameObject.GetComponent<Text>().enabled = true;
        Utility.recursivePlayAnimation(g.transform, "FadeIn");
        LocalGameManager.Instance.m_IsWindowOver = true;
        LocalGameManager.Instance.m_CanvasHUD.SetActive(false);
        OnlineManager.s_Singleton.StopClient();

        OnlineManager.Shutdown();
        Destroy(this.gameObject);
    }


	[TargetRpc]
	public void TargetRpcCoinSound(NetworkConnection conn)
	{
		AudioSource Audio = GetComponent<AudioSource> ();
		Audio.PlayOneShot (m_CoinClip,0.2f);
	}

}
