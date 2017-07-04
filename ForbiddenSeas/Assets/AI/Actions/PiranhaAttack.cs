using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;
using System;

[RAINAction]
public class PiranhaAttack : RAINAction
{

    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
    }

    public override ActionResult Execute(RAIN.Core.AI ai)
    {
        for (int i = 0; i < ai.Senses.Match("PiranhaAttack", "Player").Count; i++)
        {
            Debug.Log("Attacca ora " + ai.Senses.Match("PiranhaAttack", "Player")[i].Entity.Form.gameObject.name);
            ai.Senses.Match("PiranhaAttack", "Player")[i].Entity.Form.GetComponent<FlagshipStatus>().TakeDmgFromPiranha();
        }
        return ActionResult.SUCCESS;
    }

    public override void Stop(RAIN.Core.AI ai)
    {
        base.Stop(ai);
    }
}