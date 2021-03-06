﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFloatingName : MonoBehaviour
{
    [Range(1,4)]
    public int id;
    public Transform target;
    public Vector2 offset;
    Vector2 targetPos;
    public float amount = 1f, total = 1f;
    Image bar;

    public bool trovato = false;

    void Start()
    {
        StartCoroutine(WaitForReady());
        bar = transform.GetChild(0).GetChild(0).GetComponent<Image>();
    }

    void Update ()
    {
        if(trovato && target)
        {
            if (target.gameObject.GetComponent<Player>().isLocalPlayer)
            {
                targetPos = Camera.main.WorldToScreenPoint(target.position);
                transform.position = targetPos + offset;
                transform.GetChild(1).GetComponent<Text>().text = target.gameObject.GetComponent<Player>().playerName; //da sostituire con player name
                transform.GetChild(2).GetComponent<Text>().text = target.gameObject.GetComponent<Player>().playerName;

            }
            else
            {
                targetPos = Camera.main.WorldToScreenPoint(target.position);
                targetPos += offset;
                if (targetPos.y > (Camera.main.pixelHeight * 0.85f))
                    targetPos = new Vector2(targetPos.x, 0.85f * Camera.main.pixelHeight);
                if (Camera.main.WorldToScreenPoint(target.position).z < 0)
                {
                    targetPos = new Vector2(2f * Camera.main.pixelWidth, 2f * Camera.main.pixelHeight);
                    //Debug.Log("Trick");
                }

                transform.position = targetPos;
                transform.GetChild(1).GetComponent<Text>().text = target.gameObject.GetComponent<Player>().playerName; //da sostituire con player name
                transform.GetChild(2).GetComponent<Text>().text = target.gameObject.GetComponent<Player>().playerName;
            }
            FillBar();
            transform.GetChild(3).GetComponent<Text>().text = "" + target.gameObject.GetComponent<FlagshipStatus>().m_Health;
            transform.GetChild(4).GetComponent<Text>().text = "" + target.gameObject.GetComponent<FlagshipStatus>().m_Health;
        }
    }

    IEnumerator WaitForReady()
    {
        yield return new WaitUntil(() => LocalGameManager.Instance.GameCanStart());


        Debug.Log("Sto cercando i player!");

        if (id > LocalGameManager.Instance.m_Players.Length)
        {
            gameObject.SetActive(false);
        }
        else
        {
            foreach(GameObject g in LocalGameManager.Instance.m_Players)
            {
                if(g.GetComponent<Player>().playerId == id)
                {
                    target = g.transform;
                    break;
                }

            }
            Debug.Log("Ho trovato il player " + target.GetComponent<Player>().playerName + " " + target.GetComponent<Player>().playerId);
            trovato = true;
            total = (float)target.GetComponent<FlagshipStatus>().m_Health;
            target.gameObject.GetComponent<Player>().myTag = gameObject;
        }
    }

    void FillBar()
    {
        amount = (float)target.gameObject.GetComponent<FlagshipStatus>().m_Health;
        if (amount <= total * 0.2f)
        {
            bar.color = new Color(1f, 54f /255f,  54f /255f, bar.color.a);


        }
        else
        {
            bar.color = new Color(target.gameObject.GetComponent<Player>().isLocalPlayer ? 167f / 255f : 234f / 255f, target.gameObject.GetComponent<Player>().isLocalPlayer ? 251f / 255f : 101f / 255f, target.gameObject.GetComponent<Player>().isLocalPlayer ? 109f / 255f : 1f, bar.color.a);
        }

        bar.fillAmount = 1f / total * amount;

    }
}
