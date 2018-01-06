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
        #region domodes
        void doModes()
        {
	        Echo("mode=" + iMode.ToString());
	        if (AnyConnectorIsConnected() && (iMode!=MODE_LAUNCH ) && iMode!=MODE_DOCKED )
	        {
		        setMode(MODE_DOCKED);
	        }
	        doModeAlways();

	        if(iMode==MODE_IDLE && (craft_operation & CRAFT_MODE_SLED) > 0)
		        setMode(MODE_SLEDMMOVE);

	        if(iMode==MODE_LAUNCH){doModeLaunch();return;}
	        if(iMode==MODE_RELAUNCH){doModeRelaunch();return;}
	        if(iMode==MODE_DOCKING){doModeDocking();return;}
	        if(iMode==MODE_DOCKED){doModeDocked();return;}
   
        }
        #endregion


        #region modeidle 
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
            setMode(MODE_IDLE);
            //            if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
            StatusLog(moduleName + " Manual Control", textPanelReport);
        }
        #endregion

        #region modealways
        void doModeAlways()
        {
            /*
                        MyShipMass myMass;
                        myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
                        double maxThrust = calculateMaxThrust(thrustBackwardList);
                        double maxDeltaV = maxThrust / myMass.PhysicalMass;
                        Echo("maxDeltaV=" + niceDoubleMeters(maxDeltaV));
                        // distance *.75
                        // distance /2
                        // /(maxdeltaV*2)

                        dtmFar = calculateStoppingDistance(thrustBackwardList, 54); // calculate maximum stopping distance at full speed
                        dtmApproach = calculateStoppingDistance(thrustBackwardList, 54*.75);
                        dtmPrecision =calculateStoppingDistance(thrustBackwardList, 5f);
                        Echo("dtmFar=" + niceDoubleMeters(dtmFar));
                        Echo("dtmApproach=" + niceDoubleMeters(dtmApproach));
                        Echo("dtmPrecision=" + niceDoubleMeters(dtmPrecision));
                        Echo("CurrentStop=" + niceDoubleMeters(calculateStoppingDistance(thrustBackwardList, velocityShip)));
            */

            /*
            Echo("W=" + shipDim.WidthInMeters() + " H=" + shipDim.HeightInMeters() + "  L=" + shipDim.LengthInMeters());

            calculateGridBBPosition();
            Vector3D[] points = new Vector3D[4];
            _obbf.GetFaceCorners(4, points);
            // back output order is BL, BR, TL, TR

            // TODO: Use correct 'side' based on connector position.
            /// DUH: or just just use connector as orientation. calculateGridBBPosition() 

            for(int i=0;i<4;i++)
            {
                Echo(i + ":" + Vector3DToString(points[i]));
            }
            Echo("W2=" + (points[0] - points[1]).Length().ToString("0.0") + " H2=" + (points[0] - points[2]).Length().ToString("0.0"));

            _obbf.GetFaceCorners(0,points);
// face 0=right output order is  BL, TL, BR, TR ???
            double length=(points[0] - points[2]).Length();
            debugGPSOutput("R0", points[0]);
            debugGPSOutput("R1", points[1]);
            debugGPSOutput("R2", points[2]);
            debugGPSOutput("r3", points[3]);
            Echo("Length=" + length.ToString("0.0"));

            */
            if (iMode != MODE_ATTENTION)
            {
                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                antennaMaxPower(false, range);
            }
            processPendingSends();
            processReceives();
            if (AnyConnectorIsConnected() && (iMode != MODE_LAUNCH) && iMode != MODE_DOCKED)
            {
                setMode(MODE_DOCKED);
            }
            logState();
            checkBases();
            Echo(baseInfoString());
        }
        #endregion

        /*
        long lMomID = 0;
        Vector3D vMomPosition;
        string sMomName = "";

        bool bMomRequestSent = false;
        */
        void processReceives()
        {
 //           double x, y, z;

            if (sReceivedMessage != "")
            {
                Echo("Received Message=\n" + sReceivedMessage);
//                sInitResults += "Received Message=\n" + sReceivedMessage;

                if (BaseProcessMessages(sReceivedMessage))
                    return;

                string[] aMessage = sReceivedMessage.Trim().Split(':');

                if (aMessage.Length > 1)
                {
                    if (aMessage[0] != "WICO")
                    {
                        Echo("not wico system message");
                        return;
                    }
                    if (aMessage.Length > 2)
                    {
                        if (aMessage[1] == "MOM")
                        {
                        /* OBSOLETE
                            Echo("MOM says hello!");
                            // FORMAT:			antSend("WICO:MOM:" + Me.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(gpsCenter.GetPosition()));
                            int iOffset = 2;
                            string sName = aMessage[iOffset++];

                            long id = 0;
                            long.TryParse(aMessage[iOffset++], out id);
                            x = Convert.ToDouble(aMessage[iOffset++]);
                            y = Convert.ToDouble(aMessage[iOffset++]);
                            z = Convert.ToDouble(aMessage[iOffset++]);
                            Vector3D vPosition = new Vector3D(x, y, z);
                            if (lMomID == 0)
                            {
                                lMomID = id;
                                sMomName = sName;
                                vMomPosition = vPosition;
                            }
                            else if (lMomID == id)
                            {
                                vMomPosition = vPosition;
                            }
                            else
                            {
                                double distancesqmom = Vector3D.DistanceSquared(vMomPosition, gpsCenter.GetPosition());
                                double distancenewmom = Vector3D.DistanceSquared(vPosition, gpsCenter.GetPosition());
                                if (distancesqmom > distancenewmom)
                                {
                                    lMomID = id;
                                    sMomName = sName;
                                    vMomPosition = vPosition;
                                }
                            }
                        */
                        }

                    }
                }
            }
            /* OBSOLETE
            if (lMomID == 0)
            {
                Echo("Orphan!!!");
                if (!bMomRequestSent)
                {
                    antSend("WICO:HELLO:" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(gpsCenter.GetPosition()));
                    bMomRequestSent = true;
                }
            }
            else
                Echo("Mom=" + sMomName);
                */
        }

        #region logstate
        void logState()
        {
            string s;
            string s2;
            double dist;

            string sShipName = "";

            // should use cached antenna list...
            List<IMyTerminalBlock> Antenna = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(Antenna, localGridFilter);
            if (Antenna.Count > 0)
                sShipName = Antenna[0].CustomName.Split('!')[0].Trim();

//            StatusLog("clear", gpsPanel);

            s = "Home";

            if (bValidLaunch1)
            {
                s2 = "GPS:" + sShipName + " Docking Entry:" + Vector3DToString(vLaunch1) + ":";
                StatusLog(s2, gpsPanel);
            }

            if (bValidDock)
            {
                s2 = "GPS:" + sShipName + " Dock:" + Vector3DToString(vDock) + ":";
                StatusLog(s2, gpsPanel);
            }

            if (bValidHome)
            {
                dist = 0;
                if(gpsCenter!=null)   dist = (gpsCenter.GetPosition() - vHome).Length();
                s += ": " + dist.ToString("0") + "m";
                s2 = "GPS:" + sShipName + " Home Entry:" + Vector3DToString(vHome) + ":";
                StatusLog(s2, gpsPanel);
            }
            else s += ": NOT SET";
            if (gpsCenter != null)
            {
                s2 = "GPS:" + sShipName + " Current Position:" + Vector3DToString(gpsCenter.GetPosition()) + ":";
                StatusLog(s2, gpsPanel);
            }
        }
        #endregion

    }
}