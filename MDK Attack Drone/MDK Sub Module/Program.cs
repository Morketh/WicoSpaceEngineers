﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        string OurName = "Wico Craft";
        string moduleName = "AttackDrone";
        string sVersion = "3.2";

        const string sGPSCenter = "Craft Remote Control";

        /*
        Vector3I iForward = new Vector3I(0, 0, 0);
        Vector3I iUp = new Vector3I(0, 0, 0);
        Vector3I iLeft = new Vector3I(0, 0, 0);
        */
        Vector3D currentPosition;
        const string velocityFormat = "0.00";

        bool bWeaponsHot = true;
        long iAttackPlan = 0;
        bool bFriendlyFire = true;


        IMyTerminalBlock anchorPosition;
        IMyTerminalBlock gpsCenter = null;
//        Vector3D vCurrentPos;
        //IMyTerminalBlock gpsCenter = null;
        class OurException : Exception
        {
            public OurException(string msg) : base("WicoAttackDrone" + ": " + msg) { }
        }


        void moduleDoPreModes()
        {
        }

        void modulePostProcessing()
        {
            Echo(sInitResults);
            echoInstructions();
        }

        void ResetMotion(bool bNoDrills = false)  
        {
            powerDownThrusters(thrustAllList);
            gyrosOff();
//            powerDownRotors(rotorNavLeftList);
//            powerDownRotors(rotorNavRightList);
            if (gpsCenter is IMyRemoteControl) ((IMyRemoteControl)gpsCenter).SetAutoPilotEnabled(false);
            if (gpsCenter is IMyShipController) ((IMyShipController)gpsCenter).DampenersOverride = true;
 //           if (!bNoDrills) turnDrillsOff();
        }

    }
}