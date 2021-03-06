﻿using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //        double dAtmoCrossOver = 7000;


        // MODE_ORBITAL_LAUNCH states
        // 0 init. prepare for solo
        // check all connections. hold launch until disconnected
        // 
        // 10 capture location and init thrust settings.
        // 20 initial thrust. trying to move
        // 30 initial lift-off achieved.  start landing config retraction
        // 31 continue to accelerate
        // 
        // 40 have reached max; maintain
        // 50 wait for release..

        double dLastVelocityShip = -1;

        List<IMyTerminalBlock> thrustStage1UpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustStage1DownList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> cameraStage1LandingList = new List<IMyTerminalBlock>();

        //        double atmoEffectiveness = 1;

        float fAtmoPower = 0;
        float fHydroPower = 0;
        float fIonPower = 0;

        bool bOrbitalLaunchDebug = false;

        void doModeOrbitalLaunch()
        {
            int next_state = current_state;

 //          IMyTextPanel textPanelReport= textPanelReport;

            StatusLog("clear", textPanelReport);
            Log("clear");

            StatusLog(OurName + ":" + moduleName + ":Oribital Launch", textPanelReport);
            Log(OurName + ":" + moduleName + ":Oribital Launch");

            StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
            StatusLog(velocityShip.ToString(velocityFormat) + " m/s", textPanelReport);
            Echo("Orbital Launch. State=" + current_state.ToString());
            if (thrustStage1UpList.Count < 1)
            {
                if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                {
                    thrustStage1UpList = thrustForwardList;
                    thrustStage1DownList = thrustBackwardList;

                    cameraStage1LandingList = cameraBackwardList;
                }
                else
                {
                    Echo("Setting thrustStage1UpList");
                    thrustStage1UpList = thrustUpList;
                    thrustStage1DownList = thrustDownList;
                    cameraStage1LandingList = cameraDownList;
                }
            }

            if (current_state == 0)
            {
                //		dtStartShip = DateTime.Now;
                bWantFast = true;
                dLastVelocityShip = 0;
                if ((craft_operation & CRAFT_MODE_NOTANK) == 0) TanksStockpile(false);
                GasGensEnable(true);

                if (AnyConnectorIsConnected() || AnyConnectorIsLocked() || anyGearIsLocked())
                {
                    // launch from connected
                    vOrbitalLaunch = shipOrientationBlock.GetPosition();
                    bValidOrbitalLaunch = true;
                    bValidOrbitalHome = false; // forget any 'home' waypoint.

                    ConnectAnyConnectors(false, false);// "OnOff_Off");
                    //			blockApplyAction(gearList, "OnOff_Off"); // in case autolock is set.
                    gearsLock(false);// blockApplyAction(gearList, "Unlock");
                    current_state = 50;
                    return;
                }
                else
                {
                    // launch from hover mode
                    bValidOrbitalLaunch = false;
                    vOrbitalHome = shipOrientationBlock.GetPosition();
                    bValidOrbitalHome = true;

                    // assume we are hovering; do FULL POWER launch.
                    fAtmoPower = 0;
                    fHydroPower = 0;
                    fIonPower = 0;

                    if (ionThrustCount > 0) fIonPower = 75;
                    if (hydroThrustCount > 0)
                    {
                        for (int i = 0; i < thrustStage1UpList.Count; i++)
                        {
                            if (thrusterType(thrustStage1UpList[i]) == thrusthydro)
                                if (thrustStage1UpList[i].IsWorking)
                                {
                                    fHydroPower = 100;
                                    break;
                                }
                        }
                    }
                    if (atmoThrustCount > 0) fAtmoPower = 100;

                    powerDownThrusters(thrustStage1DownList, thrustAll, true);

                    current_state = 30;
                    return;
                }
            }
            if (current_state == 50)
            {
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + current_state.ToString(), textLongStatus, true);
                if (AnyConnectorIsConnected() || AnyConnectorIsLocked() || anyGearIsLocked())
                {
                    StatusLog("Awaiting release", textPanelReport);
                    Log("Awaiting release");
                }
                else
                {
                    StatusLog(DateTime.Now.ToString() + " " + OurName + ":Saved Position", textLongStatus, true);

                    // we launched from connected. Save position
                    vOrbitalLaunch = shipOrientationBlock.GetPosition();
                    bValidOrbitalLaunch = true;
                    bValidOrbitalHome = false; // forget any 'home' waypoint.

                    next_state = 10;
                }

                current_state = next_state;
            }
            Vector3D vTarget = new Vector3D(0, 0, 0);
            bool bValidTarget = false;
            if (bValidOrbitalLaunch)
            {
                bValidTarget = true;
                vTarget = vOrbitalLaunch;
            }
            else if (bValidOrbitalHome)
            {
                bValidTarget = true;
                vTarget = vOrbitalHome;
            }

            double alt = 0;
            if (bValidTarget)
            {
 //               alt = (vCurrentPos - vTarget).Length();
 //               StatusLog("Distance: " + alt.ToString("N0") + " Meters", textPanelReport);

                double elevation = 0;

                ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);


            }
            if (current_state == 10)
            {
                bWantFast = true;
                calculateHoverThrust(thrustStage1UpList, out fAtmoPower, out fHydroPower, out fIonPower);
                powerDownThrusters(thrustStage1DownList, thrustAll, true);
                current_state = 20;
                return;
            }

            double deltaV = velocityShip - dLastVelocityShip;
            double expectedV = deltaV * 5 + velocityShip;

            if (current_state == 20)
            { // trying to move
                StatusLog("Attempting Lift-off", textPanelReport);
                Log("Attempting Lift-off");
                // NOTE: need to NOT turn off atmo if we get all the way into using ions for this state.. and others?
                if (velocityShip < 3f)
                // if(velocityShip<1f)
                {
                    increasePower(dGravity, alt);
                    increasePower(dGravity, alt);
                }
                else
                {
                    next_state = 30; // we have started to lift off.
                    dLastVelocityShip = 0;
                }
            }
            else
            {
                bWantMedium = true;
                if (alt > 5)
                {
                    if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                        StatusLog("Wico Gravity Alignment OFF", textPanelReport);
                    else
                    {

                        StatusLog("Gravity Alignment Operational", textPanelReport);
                        string sOrientation = "";
                        if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                            sOrientation = "rocket";

                        if (!GyroMain(sOrientation))
                            bWantFast = true;
                    }
                }
            }

            if (current_state == 30)
            { // Retract landing config
                StatusLog("Movement started. Retracting Landing config ", textPanelReport);
                Log("Movement started. Retracting Landing config ");
                next_state = 31;
            }
            if (current_state == 31)
            { // accelerate to max speed
                StatusLog("Accelerating to max speed (" + fMaxWorldMps.ToString("0") + ")", textPanelReport);
                Log("Accelerating to max speed");
                if (dLastVelocityShip < velocityShip)
                { // we are Accelerating
                    if (bOrbitalLaunchDebug) Echo("Accelerating");
                    if (expectedV < (fMaxWorldMps / 2))
                    // if(velocityShip<(fMaxMps/2))
                    {
                        decreasePower(dGravity, alt); // take away from lowest provider
                        increasePower(dGravity, alt);// add it back
                    }
                    if (expectedV < (fMaxWorldMps / 5)) // add even more.
                        increasePower(dGravity, alt);

                    if (velocityShip > (fMaxWorldMps - 5))
                        next_state = 40;
                }
                else
                {
                    increasePower(dGravity, alt);//
                    increasePower(dGravity, alt);// and add some more
                }
            }

            if (current_state == 40)
            { // maintain max speed
                StatusLog("Maintain max speed", textPanelReport);
                Log("Maintain max speed");
                if (bOrbitalLaunchDebug) StatusLog("Expectedv=" + expectedV.ToString("0.00") + " max=" + fMaxWorldMps.ToString("0.00"), textPanelReport);
                if (bOrbitalLaunchDebug) Echo("Expectedv=" + expectedV.ToString("0.00") + " max=" + fMaxWorldMps.ToString("0.00"));
                double dMin = (fMaxWorldMps - fMaxWorldMps * .05);
                if (expectedV > dMin)
                // if(velocityShip>(fMaxMps-5))
                {
                    calculateHoverThrust(thrustStage1UpList, out fAtmoPower, out fHydroPower, out fIonPower);
                    if (bOrbitalLaunchDebug) Echo("hover thrust:" + fAtmoPower.ToString("0.00") + ":" + fHydroPower.ToString("0.00") + ":" + fIonPower.ToString("0.00"));
                    if (bOrbitalLaunchDebug) StatusLog("hover thrust:" + fAtmoPower.ToString("0.00") + ":" + fHydroPower.ToString("0.00") + ":" + fIonPower.ToString("0.00"), textPanelReport);
                    /*
                     * Not needed as of 1.185
                    if (fAtmoPower < 1.001)
                        fAtmoPower = 0;
                    if (fHydroPower < 1.001)
                        fHydroPower = 0;
                    if (fIonPower < 1.001)
                        fIonPower = 0;
                        */

                }
                else if (expectedV < (fMaxWorldMps - 10))
                {
                    decreasePower(dGravity, alt); // take away from lowest provider
                    increasePower(dGravity, alt);// add it back
                    increasePower(dGravity, alt);// and add some more
                }
                if (velocityShip < (fMaxWorldMps / 2))
                    next_state = 20;

                ConnectAnyConnectors(false, true);// "OnOff_On");
                blocksOnOff(gearList, true);
                //                blockApplyAction(gearList, "OnOff_On");
            }
            dLastVelocityShip = velocityShip;

            StatusLog("", textPanelReport);

            //	if (bValidExtraInfo)
            StatusLog("Car:" + progressBar(cargopcent), textPanelReport);

            //	batteryCheck(0, false);//,textPanelReport);
            //	if (bValidExtraInfo)
            if (batteryList.Count > 0)
            {
                StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                Echo("BatteryPercentage=" + batteryPercentage);
            }
            else StatusLog("Bat: <NONE>", textPanelReport);

            if (oxyPercent >= 0)
            {
                StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                //Echo("O:" + oxyPercent.ToString("000.0%"));
            }
            else Echo("No Oxygen Tanks");

            if (hydroPercent >= 0)
            {
                StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                Echo("H:" + (hydroPercent*100).ToString("0.0") + "%");
                if (hydroPercent < 0.20f)
                {
                    StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);
                    Log(" WARNING: Low Hydrogen Supplies");
                }
            }
            else Echo("No Hydrogen Tanks");
            if (batteryList.Count > 0 && batteryPercentage < batterypctlow)
            {
                StatusLog(" WARNING: Low Battery Power", textPanelReport);
                Log(" WARNING: Low Battery Power");
            }

            StatusLog("", textPanelReport);
            if (dGravity < 0.01)
            {
                powerDownThrusters(thrustAllList);
                gyrosOff();
                //                startNavCommand("!;V");
                setMode(MODE_INSPACE);
                StatusLog("clear", textPanelReport);
                Log("clear");
                return;
            }

            int iPowered = 0;

            if (fIonPower > 0)
            {
                powerDownThrusters(thrustAllList, thrustatmo, true);
                powerDownThrusters(thrustAllList, thrustion);
                iPowered = powerUpThrusters(thrustStage1UpList, fIonPower , thrustion);
                //Echo("Powered "+ iPowered.ToString()+ " Ion Thrusters");
            }
            else
            {
                powerDownThrusters(thrustAllList, thrustion, true);
                powerDownThrusters(thrustStage1UpList, thrustion);
            }

            if (fHydroPower > 0)
            {
//                Echo("Powering Hydro to " + fHydroPower.ToString());
                powerUpThrusters(thrustStage1UpList, fHydroPower , thrusthydro);
            }
            else
            { // important not to let them provide dampener power..
                powerDownThrusters(thrustStage1DownList, thrusthydro, true);
                powerDownThrusters(thrustStage1UpList, thrusthydro, true);
            }
            if (fAtmoPower > 0)
            {
                powerUpThrusters(thrustStage1UpList, fAtmoPower , thrustatmo);
            }
            else
            {
                closeDoors(outterairlockDoorList);

                // iPowered=powerDownThrusters(thrustStage1UpList,thrustatmo,true);
                iPowered = powerDownThrusters(thrustAllList, thrustatmo, true);
                //Echo("Powered DOWN "+ iPowered.ToString()+ " Atmo Thrusters");
            }

            {
                powerDownThrusters(thrustStage1DownList, thrustAll, true);
            }

            StatusLog("Thrusters", textPanelReport);
            if (ionThrustCount > 0)
            {
                if(fIonPower<10)
                    StatusLog("ION:\n/10:" + progressBar(fIonPower*10), textPanelReport);
                else
                    StatusLog("ION:" + progressBar(fIonPower), textPanelReport);
            }
            if (hydroThrustCount > 0)
            {
                if(fHydroPower<10)

                    StatusLog("HYD\n/10:" + progressBar(fHydroPower*10), textPanelReport);
                else
                StatusLog("HYD:" + progressBar(fHydroPower), textPanelReport);
            }
            if (atmoThrustCount > 0)
            {
                if(fAtmoPower<10)
                StatusLog("ATM\n/10:" + progressBar(fAtmoPower*10), textPanelReport);
                else
                StatusLog("ATM:" + progressBar(fAtmoPower), textPanelReport);
            }
            if (bOrbitalLaunchDebug)
                StatusLog("I:" + fIonPower.ToString("0.00") + "H:" + fHydroPower.ToString("0.00") + " A:" + fAtmoPower.ToString("0.00"), textPanelReport);
            current_state = next_state;

        }

        void increasePower(double dGravity, double alt)
        {
            double dAtmoEff = AtmoEffectiveness();
            /*
            Echo("atmoeff=" + dAtmoEff.ToString());
            Echo("hydroThrustCount=" + hydroThrustCount.ToString());
            Echo("fHydroPower=" + fHydroPower.ToString());
            Echo("fAtmoPower=" + fAtmoPower.ToString());
            Echo("fIonPower=" + fIonPower.ToString());
            */
            if (dGravity > .5 && (atmoThrustCount ==0 || dAtmoEff > 0.10))
            //                if (dGravity > .5 && alt < dAtmoCrossOver)
            {
                if (fAtmoPower < 100 && atmoThrustCount > 0)
                    fAtmoPower += 5;
                else if (fHydroPower == 0 && fIonPower > 0)
                { // we are using ion already...
                    if (fIonPower < 100 && ionThrustCount > 0)
                        fIonPower += 5;
                    else
                        fHydroPower += 5;
                }
                else if (fIonPower < 100 && ionThrustCount > 0)
                    fIonPower += 5;
                else if (fHydroPower < 100 && hydroThrustCount > 0)
                {
                    // fAtmoPower=100;
                    fHydroPower += 5;
                }
                else // no power left to give, captain!
                {
                    StatusLog("Not Enough Thrust!", textPanelReport);
                    Echo("Not Enough Thrust!");
                }
            }
            else if (dGravity > .5 || dAtmoEff < 0.10)
            {
                if (fIonPower < fAtmoPower && atmoThrustCount > 0 && ionThrustCount > 0)
                {
                    float f = fIonPower;
                    fIonPower = fAtmoPower;
                    fAtmoPower = f;
                }
                if (fIonPower < 100 && ionThrustCount > 0)
                    fIonPower += 10;
                else if (fHydroPower < 100 && hydroThrustCount > 0)
                {
                    fHydroPower += 5;
                }
                else if (dAtmoEff > 0.10 && fAtmoPower < 100 && atmoThrustCount > 0)
                    fAtmoPower += 10;
                else if (dAtmoEff > 0.10 && atmoThrustCount > 0)
                    fAtmoPower -= 5; // we may be sucking power from ion
                else // no power left to give, captain!
                {
                    StatusLog("Not Enough Thrust!", textPanelReport);
                    Echo("Not Enough Thrust!");
                }
            }
            else if (dGravity > .01)
            {
                if (fIonPower < 100 && ionThrustCount > 0)
                    fIonPower += 15;
                else if (fHydroPower < 100 && hydroThrustCount > 0)
                {
                    fHydroPower += 5;
                }
                else if (dAtmoEff > 0.10 && fAtmoPower < 100 && atmoThrustCount > 0)
                    fAtmoPower += 10;
                else // no power left to give, captain!
                {
                    StatusLog("Not Enough Thrust!", textPanelReport);
                    Echo("Not Enough Thrust!");
                }

            }

            if (fIonPower > 100) fIonPower = 100;
            if (fAtmoPower > 100) fAtmoPower = 100;
            if (fAtmoPower < 0) fAtmoPower = 0;
            if (fHydroPower > 100) fHydroPower = 100;

        }

        void decreasePower(double dGravity, double alt)
        {
            if (dGravity > .85 && AtmoEffectiveness() > 0.10)
            {
                if (fHydroPower > 0)
                {
                    fHydroPower -= 5;
                }
                else if (fIonPower > 0)
                    fIonPower -= 5;
                else if (fAtmoPower > 10)
                    fAtmoPower -= 5;
            }
            else if (dGravity > .3)
            {
                if (fAtmoPower > 0)
                    fAtmoPower -= 10;
                else if (fHydroPower > 0)
                {
                    fHydroPower -= 5;
                }
                else if (fIonPower > 10)
                    fIonPower -= 5;

            }
            else if (dGravity > .01)
            {
                if (fAtmoPower > 0)
                    fAtmoPower -= 5;
                else if (fHydroPower > 0)
                {
                    fHydroPower -= 5;
                }
                else if (fIonPower > 10)
                    fIonPower -= 5;
            }

            if (fIonPower < 0) fIonPower = 0;
            if (fAtmoPower < 0) fAtmoPower = 0;
            if (fHydroPower < 0) fHydroPower = 0;

        }
    }

}