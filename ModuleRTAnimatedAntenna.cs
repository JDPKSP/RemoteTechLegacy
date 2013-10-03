﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTAnimatedAntenna : RemoteTechAntennaCore
    {
        [KSPField]
        public string
        Animation = "",
        BreakTransformName = "",
        Mode1Name = "Mode1",
        Mode0Name = "Mode0",
        ToggleName = "Toggle";

        [KSPField(isPersistant = true)]
        public bool Locked = false, broken = false;

        [KSPField]
        public bool
            fixAnimLayers = false,
            MasterOf0 = false,
            MasterOf1 = false,
            MasterOfLoop0 = false,
            MasterOfLoop1 = false,
            LoopLock = false,
            ModeLock = false,
            willWakeInPanic = false;

        [KSPField(isPersistant = true)]
        public int
        animState = 0;

        [KSPField]
        public float
        animPlayStart = 0,
        MinimumDrag = 0,
        MaximumDrag = 0,
        Dragmodifier = 0,
        MaxQ = -1,
        DragCoeff = 0.02F,
        EnergyDrain0 = 0,
        EnergyDrain1 = 0,
        Mode0EnergyCost = 0,
        Mode1EnergyCost = 0,
        antennaRange0 = 0,
        antennaRange1 = 0,
        dishRange0 = 0,
        dishRange1 = 0;

        public override string GetInfo()
        {
            string text = "";

            if (antennaRange0 != antennaRange1)
            {
                if (text.Length > 0) text += "\n";
                text += "Antenna range: " + RTUtils.length(this.antennaRange0 * 1000) + "m / " + RTUtils.length(this.antennaRange1 * 1000) + "m";
            }
            else if (antennaRange > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Antenna range: " + RTUtils.length(antennaRange * 1000) + "m";
            }

            if (dishRange0 != dishRange1)
            {
                if (text.Length > 0) text += "\n";
                text += "Dish range: " + RTUtils.length(dishRange0 * 1000) + "m / " + RTUtils.length(dishRange1 * 1000) + "m";
            }
            else if (dishRange > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Dish range: " + RTUtils.length(dishRange * 1000) + "m";
            }

            if (EnergyDrain0 != EnergyDrain1)
            {
                if (text.Length > 0) text += "\n";
                text += "Energy req.: " + RTUtils.eCost(EnergyDrain0) + " / " + RTUtils.eCost(EnergyDrain1); // NK
            }
            else if (this.EnergyDrain > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Energy req.: " + RTUtils.eCost(EnergyDrain); // NK
            }

            return text;
        }


        protected Animation anim
        {
            get { return part.FindModelAnimators(Animation)[0]; }
        }


        void act0()
        {
            if (MasterOfLoop0)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.SetMode0();


            if (MasterOf0)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.SetMode0();

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.SetMode0();

                foreach (ModuleRTAnimTrackAntenna m in part.Modules.OfType<ModuleRTAnimTrackAntenna>())
                    m.SetMode0();
            }
            else
                SetMode0();

            if (ModeLock)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.Locked = true;

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    if (m != this)
                        m.Locked = true;
            }

            if (LoopLock)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.Locked = true;

            part.SendMessage("UpdateGUI");
        }

        void act1()
        {
            if (MasterOfLoop1)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.SetMode1();

            if (MasterOf1)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.SetMode1();

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.SetMode1();
            }
            else
                SetMode1();

            if (ModeLock)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.Locked = false;

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    if (m != this)
                        m.Locked = false;
            }

            if (LoopLock)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.Locked = false;

            part.SendMessage("UpdateGUI");
        }


        [KSPAction("ActionToggle", KSPActionGroup.None, guiName = "Toggle")]
        public void ActionToggle(KSPActionParam param)
        {
            if (Locked) return;
            if (!InControl) return;
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
            else
                if (animState == 1 && RequestActPower(Mode0EnergyCost))
                {
                    act0();
                }
        }

        [KSPAction("Mode1Action", KSPActionGroup.None, guiName = "Mode1")]
        public void Mode1Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPAction("Mode0Action", KSPActionGroup.None, guiName = "Mode0")]
        public void Mode0Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && animState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "Mode1Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode1Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }
        [KSPEvent(name = "Mode0Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode0Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if (animState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "OverrideMode1Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode1Event()
        {
            if (Locked) return;
            if (!powered) return;
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPEvent(name = "OverrideMode0Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode0Event()
        {
            if (Locked) return;
            if (!powered) return;
            if (animState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }

        public void UpdateGUI()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (Locked || broken)
            {
                Events["Mode1Event"].active = Events["OverrideMode1Event"].active = Events["Mode0Event"].active = Events["OverrideMode0Event"].active = false;
            }
            else
            {
                if (animState == 1)
                {
                    Events["Mode1Event"].active = Events["OverrideMode1Event"].active = false;
                    Events["Mode0Event"].active = Events["OverrideMode0Event"].active = true;
                }
                else
                {
                    Events["Mode1Event"].active = Events["OverrideMode1Event"].active = true;
                    Events["Mode0Event"].active = Events["OverrideMode0Event"].active = false;
                }
            }
        }

        public void SetMode1()
        {
            if (broken) return;

            anim[Animation].speed = Mathf.Abs(anim[Animation].speed);

            anim.Play(Animation);


            if (anim[Animation].normalizedTime == 1)
                anim[Animation].normalizedTime = 0;

            animState = 1;

            if (this.MaximumDrag > 0)
            {
                part.minimum_drag = this.MinimumDrag + Dragmodifier;
                part.maximum_drag = this.MaximumDrag + Dragmodifier;
            }

            SetRange1();
        }
        void SetRange1()
        {
            EnergyDrain = EnergyDrain1;
            antennaRange = antennaRange1;
            dishRange = dishRange1;

            if (!HighLogic.LoadedSceneIsFlight) return;
            RTGlobals.network = new RelayNetwork();
            try
            {
                RTGlobals.coreList[vessel].path = RTGlobals.network.GetCommandPath(RTGlobals.coreList[vessel].Rnode);
            }
            catch { }
            UpdatePA();
        }
        public void SetMode0()
        {
            if (broken) return;

            anim[Animation].speed = -Mathf.Abs(anim[Animation].speed);

            anim.Play(Animation);


            if (anim[Animation].normalizedTime == 0)
                anim[Animation].normalizedTime = 1;

            animState = 0;

            if (this.MaximumDrag > 0)
            {
                part.minimum_drag = this.MinimumDrag;
                part.maximum_drag = this.MaximumDrag;
            }

            SetRange0();
        }
        void SetRange0()
        {
            EnergyDrain = EnergyDrain0;
            antennaRange = antennaRange0;
            dishRange = dishRange0;

            if (!HighLogic.LoadedSceneIsFlight) return;
            RTGlobals.network = new RelayNetwork();
            try
            {
                RTGlobals.coreList[vessel].path = RTGlobals.network.GetCommandPath(RTGlobals.coreList[vessel].Rnode);
            }
            catch { }
            UpdatePA();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (broken)
            {
                RTUtils.findTransformsWithCollider(part.FindModelTransform(BreakTransformName), ref toRemove);
                foreach (Transform t in toRemove)
                    Destroy(t.gameObject);
                toRemove.Clear();

                if (this.MaximumDrag > 0)
                {
                    part.minimum_drag = this.MinimumDrag;
                    part.maximum_drag = this.MaximumDrag;
                }

                EnergyDrain = antennaRange = dishRange = 0;
                part.SendMessage("UpdateGUI");
                UpdatePA();
                RTGlobals.network = new RelayNetwork();
                return;
            }


            Actions["Mode1Action"].guiName = Events["Mode1Event"].guiName = Mode1Name;
            Actions["Mode0Action"].guiName = Events["Mode0Event"].guiName = Mode0Name;
            Actions["ActionToggle"].guiName = ToggleName;

            Events["OverrideMode1Event"].guiName = "Override " + Mode1Name;
            Events["OverrideMode0Event"].guiName = "Override " + Mode0Name;

            if (animState == 1)
                act1();
            else
                act0();

            if (fixAnimLayers)
            {
                int i = 0;
                foreach (AnimationState s in anim)
                {
                    s.layer = i;
                    i++;
                }
            }

            anim[Animation].normalizedTime = this.animState;

            anim[Animation].wrapMode = WrapMode.Clamp;

            if (state == StartState.Editor)
            {
                if (animPlayStart == 1)
                    SetMode1();
                else if (animPlayStart == -1)
                    SetMode0();
            }

            base.OnStart(state);
        }


        bool RequestActPower(float requiredAmount)
        {
            if (requiredAmount == 0)
                return true;

            float amount = part.RequestResource("ElectricCharge", requiredAmount);
            if (amount == requiredAmount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void RequestPower()
        {
            if (EnergyDrain == 0)
            {
                powered = true;
            }
            else
            {
                float amount = part.RequestResource("ElectricCharge", EnergyDrain * TimeWarp.deltaTime);
                powered = amount != 0;
            }
        }




        List<Transform> toRemove = new List<Transform>();
        bool explodeMe = false;
        public override void OnUpdate()
        {
            if (!flightStarted || broken) return;

            if (EnergyDrain1 > 0)
                RequestPower();

            if (vessel != null && RTUtils.PhysicsActive)
            {
                if (willWakeInPanic && animState == 0 && !InControl && !anim.IsPlaying(Animation))
                {
                    SetMode1();
                    UpdateGUI();
                }

                if (explodeMe && Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, part.transform.position) > 250)
                {
                    explodeMe = false;
                    part.explode();
                }

                if (MaxQ > 0 && animState == 1 && (Math.Pow(vessel.srf_velocity.magnitude, 2) * vessel.atmDensity * 0.5) > MaxQ)
                {
                    if (BreakTransformName == "")
                    {
                        part.decouple(0f);
                        explodeMe = true;
                    }
                    else
                    {
                        broken = true;
                        RTUtils.findTransformsWithCollider(part.FindModelTransform(BreakTransformName), ref toRemove);

                        foreach (Transform t in toRemove)
                        {
                            Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();

                            rb.angularDrag = part.rigidbody.angularDrag;
                            rb.angularVelocity = part.rigidbody.angularVelocity;
                            rb.drag = 0;
                            rb.mass = part.mass / 5;
                            rb.velocity = part.rigidbody.velocity;
                            rb.isKinematic = false;
                            t.parent = null;
                            rb.AddForce(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5));
                            rb.AddTorque(UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20));

                            DragModel dm = t.gameObject.AddComponent<DragModel>();
                            dm.enabled = true;
                            dm.tf = t;
                            dm.rb = rb;
                            dm.dc = DragCoeff;
                            dm.mb = vessel.mainBody;
                        }

                        if (this.MaximumDrag > 0)
                        {
                            part.minimum_drag = this.MinimumDrag;
                            part.maximum_drag = this.MaximumDrag;
                        }
                        EnergyDrain = antennaRange = dishRange = 0;
                        part.SendMessage("UpdateGUI");
                        UpdatePA();
                        RTGlobals.network = new RelayNetwork();
                    }
                }
            }

        }


    }
}
