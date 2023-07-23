﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMugen.Combat
{

    public class EngineInitialization : MonoBehaviour
    {

        public void SetSeed()
        {
            Seed = (!saveSeedDebug ? Environment.TickCount : Seed);
        }

        public CombatMode Mode;
        public int stageID;
        public int musicID;
        public int Seed;
        public bool saveSeedDebug;

        [Header("Player1")]
        public TeamMode Team1Mode;
        public List<PlayerCreation> Team1;

        [Header("Player2")]
        public TeamMode Team2Mode;
        public List<PlayerCreation> Team2;


    }
}