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

        //TODO: Add loading and saving NAV specific settings to its own section

        // NAV
        //        double arrivalDistanceMax = 100;

        /// <summary>
        /// false means just orient (no motion)
        /// </summary>
        bool bGoOption = true; // false means just orient.

        /// <summary>
        /// We are a sled. Default false
        /// </summary>
        bool bSled = false;

        /// <summary>
        /// We are rotor-control propulsion. Default false
        /// </summary>
        bool bRotor = false;

        /*
        States:
        0 -- Master Init


        160 Main Travel to target



        *** below here are thruster-only routines (for now)

        170 Collision Detected From 160
            Calculate collision avoidance 
            then ->172

        171 dummy state for debugging.
        172 do travel movemenet for collision avoidance. 
        if arrive target, ->160
        if secondary collision ->173

        173 secondary collision
        if a type we can move around, try to move ->174
        else go back to collision detection ->170

        174 initilize escape plan
        ->175

        175 scan for an 'escape' route (pathfind)
        timeout of (default) 5 seconds ->MODE_ATTENTION
        after scans, ->180

        180 travel to avoidance waypoint
        on arrival ->160 (main travel)
        on collision ->173

        200 Arrived at target
        ->MODE_ARRIVEDTARGET

        */
        void doModeGoTarget()
        {
            StatusLog("clear", textPanelReport);

            StatusLog(moduleName + ":Going Target!", textPanelReport);
            StatusLog(moduleName + ":GT: current_state=" + current_state.ToString(), textPanelReport);
//            bWantFast = true;
            Echo("Going Target: state=" + current_state.ToString());
            if (current_state == 0)
            {
                ResetTravelMovement();
                if ((craft_operation & CRAFT_MODE_SLED) > 0)
                {
                    bSled = true;
                    if (shipSpeedMax > 45) shipSpeedMax = 45;
                }
                else bSled = false;

                if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
                {
                    bRotor = true;
                    if (shipSpeedMax > 15) shipSpeedMax = 15;
                }
                else bRotor = false;

                GyroControl.SetRefBlock(shipOrientationBlock);
                double elevation = 0;

                ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);

                if (bValidNavTarget)
                {
                    if (elevation> shipDim.HeightInMeters())
                    {
                        current_state = 150;
                    }
                    else current_state = 160;
                }
                else setMode(MODE_ATTENTION);
                bWantFast = true;
            }
            else if(current_state==150)
            {
                //if (!bSled && !bRotor && dGravity > 0)
                if (dGravity > 0)
                {

                    float fSaveAngle = minAngleRad;
                    minAngleRad = 0.1f;
                    bool bAligned= GyroMain("");
                    Echo("bAligned=" + bAligned.ToString());
                    minAngleRad = fSaveAngle;
                    if (bAligned)
                    {
                        current_state = 160;
                    }
                    bWantFast = true;
                }
                else current_state = 160;

            }
            else if (current_state == 160)
            { //	160 move to Target
                Echo("Moving to Target");
                Vector3D vTargetLocation = vNavTarget;

                Vector3D vVec = vTargetLocation - shipOrientationBlock.GetPosition();
                double distance = vVec.Length();
                Echo("distance=" + niceDoubleMeters(distance));
                Echo("velocity=" + velocityShip.ToString("0.00"));

                StatusLog("clear",sledReport);
                StatusLog("Moving to Target\nD:" + niceDoubleMeters(distance) + " V:" + velocityShip.ToString(velocityFormat),sledReport);

                //      Echo("TL:" + vTargetLocation.X.ToString("0.00") + ":" + vTargetLocation.Y.ToString("0.00") + ":" + vTargetLocation.Z.ToString("0.00"));
                //		if(distance<17)
//                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
//                range = Math.Max(range, distance);
//                antennaMaxPower(false,range);
                if (bGoOption && (distance < arrivalDistanceMin))
                {
                    Echo("we have arrived");
                    if (NAVEmulateOld)
                    {
                        var tList = GetBlocksContains<IMyTerminalBlock>("NAV:");
                        for (int i1 = 0; i1 < tList.Count(); i1++)
                        {
                            // don't want to get blocks that have "NAV:" in customdata..
                            if (tList[i1].CustomName.StartsWith("NAV:"))
                            {
                                Echo("Found NAV: command:");
                                tList[i1].CustomName = "NAV: C Arrived Target";
                            }
                        }
                    }
                    //				bValidTargetLocation = false;
                    //                    gyrosOff();
                    ResetMotion();
                    bValidNavTarget = false; // we used this one up.
                    setMode(MODE_ARRIVEDTARGET);
                    return;
                }
//                bool bYawOnly = false;
//                if (bSled || bRotor) bYawOnly = true;

//                debugGPSOutput("TargetLocation", vTargetLocation);
                bool bDoTravel = true;
                double elevation = 0;

                ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                Echo("Elevation=" + elevation.ToString("0.0"));
                Echo("MinEle=" + NAVGravityMinElevation.ToString("0.0"));
                if (!bSled && !bRotor && NAVGravityMinElevation>0 && elevation< NAVGravityMinElevation)
                {
                    powerUpThrusters(thrustUpList, 100);
//                    bDoTravel = false;
                }
                /*
                if(!bSled && !bRotor && dGravity>0)
                {
                    float fSaveAngle = minAngleRad;
                    minAngleRad = 0.1f;
                    bDoTravel = GyroMain("");
                    Echo("Travel=" + bDoTravel.ToString());
                    minAngleRad = fSaveAngle;
                    if (!bDoTravel)
                        bWantFast = true;
                }
                */
                if(bDoTravel)
                {
                    Echo("Do Travel");
                    doTravelMovement(vTargetLocation, 3.0f, 200, 170);
                }
            }

            else if(current_state==170)
            { // collision detection
              //                IMyTextPanel tx = gpsPanel;
              //                gpsPanel = textLongStatus;
              //           StatusLog("clear", gpsPanel);

                bWantFast = true;
                Vector3D vTargetLocation = vNavTarget;
                ResetTravelMovement();
                calcCollisionAvoid(vTargetLocation);

//                gpsPanel = tx;
//                current_state = 171; // testing
                current_state = 172;
            }
            else if (current_state == 171)
            { 
                // just hold this state
                bWantFast = false;
            }

            else if (current_state == 172)
            {
                //                 Vector3D vVec = vAvoid - shipOrientationBlock.GetPosition();
                //                double distanceSQ = vVec.LengthSquared();
                Echo("Collision Avoid");
                StatusLog("clear", sledReport);
                StatusLog("Collision Avoid", sledReport);
                doTravelMovement(vAvoid, 5.0f, 160, 173);
            }
            else if (current_state == 173)
            {       // secondary collision
                if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid 
                    || lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid 
                    || lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid 
                    )
                {
                    current_state = 174;
                }
                else current_state = 170;// setMode(MODE_ATTENTION);
                bWantFast = true;
            }
            else if (current_state == 174)
            {
                initEscapeScan();
                ResetTravelMovement();
                dtNavStartShip = DateTime.Now;
                current_state = 175;
                bWantFast = true;
            }
            else if (current_state == 175)
            {
                DateTime dtMaxWait = dtNavStartShip.AddSeconds(5.0f);
                DateTime dtNow = DateTime.Now;
                if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                {
                    setMode(MODE_ATTENTION);
                    doTriggerMain();
                    return;
                }
                if (scanEscape())
                {
                    Echo("ESCAPE!");
                    current_state = 180;
                }
                bWantMedium = true;
//                bWantFast = true;
           }
            else if(current_state==180)
            {
                doTravelMovement(vAvoid,1f, 160, 173);
            }
            else if(current_state==200)
            { // we have arrived at target
                StatusLog("clear", sledReport);
                StatusLog("Arrived at Target", sledReport);
                ResetMotion();
                bValidNavTarget = false; // we used this one up.
//                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                antennaMaxPower(false);
                sleepAllSensors();
                setMode(MODE_ARRIVEDTARGET);
                if(NAVEmulateOld)
                {
                    var tList = GetBlocksContains<IMyTerminalBlock>("NAV:");
                    for (int i1 = 0; i1 < tList.Count(); i1++)
                    {
                        // don't want to get blocks that have "NAV:" in customdata..
                        if (tList[i1].CustomName.StartsWith("NAV:"))
                        {
                            Echo("Found NAV: command:");
                            tList[i1].CustomName = "NAV: C Arrived Target";
                        }
                    }
                }
                bWantFast = true;
                doTriggerMain();
            }
        }

        void powerForward(float fPower)
        {
            if (bRotor)
            {
                /*
                // need to ramp up/down rotor power or they will flip small vehicles and spin a lot
                float maxVelocity = rotorNavLeftList[0].GetMaximum<float>("Velocity");
                float currentVelocity = rotorNavLeftList[0].GetValueFloat("Velocity");
                float cPower = (currentVelocity / maxVelocity * 100);
                cPower = Math.Abs(cPower);
                if (fPower > (cPower + 5f))
                    fPower = cPower + 5;
                if (fPower < (cPower - 5))
                    fPower = cPower - 5;

                if (fPower < 0f) fPower = 0f;
                if (fPower > 100f) fPower = 100f;
                */
                powerUpRotors(fPower);
            }
            else
                powerUpThrusters(thrustForwardList, fPower);
        }

        void powerDown()
        {
            powerDownThrusters(thrustAllList);
            powerDownRotors();
        }
    }
}