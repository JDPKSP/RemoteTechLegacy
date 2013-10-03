using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class DragModel : MonoBehaviour
    {
        public Transform tf;
        public Rigidbody rb;
        public float dc = 0, maxDistance = 1000;
        public CelestialBody mb;


        public float atmDensity
        {
            get
            {
                return (float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(mb.GetAltitude(tf.position), mb));
            }
        }

        float area
        {
            get
            {
                if (tf.collider == null) return 0;

                Vector3
                    dir = -(Krakensbane.GetFrameVelocity() + rb.velocity).normalized,
                    size = tf.collider.bounds.size;

                float
                    XY = (float)Math.Cos(Mathf.Deg2Rad * Vector3.Angle(tf.up, dir)) * size.x * size.y,
                    YZ = (float)Math.Cos(Mathf.Deg2Rad * Vector3.Angle(tf.right, dir)) * size.y * size.z,
                    XZ = (float)Math.Cos(Mathf.Deg2Rad * Vector3.Angle(tf.forward, dir)) * size.x * size.z;

                return XY + YZ + XZ;
            }
        }

        Vector3 dragForceDir
        {
            get
            {
                return -((Krakensbane.GetFrameVelocity() + rb.velocity).normalized * dragForce);
            }
        }


        float dragForce
        {
            get
            {
                float vel = velocity;
                return Mathf.Clamp(((float)(Math.Pow(vel, 2) * atmDensity * 0.5F) * area * dc), 0, (vel / TimeWarp.deltaTime) * rb.mass);
            }
        }

        float velocity
        {
            get
            {
                return (float)(Krakensbane.GetFrameVelocity() + rb.velocity).magnitude;
            }
        }

        public void FixedUpdate()
        {
            if (mb.atmosphere && mb.GetAltitude(tf.position) < mb.maxAtmosphereAltitude)
            rb.AddForce(dragForceDir);
        }
    }


    public class Pivot
    {
        Transform pivot;
        float increment, angleMinus, anglePlus;
        bool fullcircle = false;

        Vector3 parentOrigRef;

        Vector2 OrigRef
        {
            get
            {
                Vector3 tmp = pivot.InverseTransformPoint(pivot.parent.TransformPoint(parentOrigRef));
                return new Vector2(tmp.x, tmp.y);
            }
        }


        public Pivot(Transform pivotin, float incrementIn, Vector2 bounds)
        {
            pivot = pivotin;
            increment = Mathf.Deg2Rad * incrementIn;
            angleMinus = Mathf.Deg2Rad * bounds.y;
            anglePlus = Mathf.Deg2Rad * bounds.x;
            fullcircle = bounds == Vector2.zero;

            parentOrigRef = pivot.parent.InverseTransformPoint(pivot.TransformPoint(Vector3.up));
        }

        public void SnapToTarget(Vector3 Tgt)
        {
            Vector3 tmpTGT = pivot.InverseTransformPoint(Tgt);
            Vector2 target = new Vector2(tmpTGT.x, tmpTGT.y);

            float angle = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);

            if (angle == 0) return;

            if (target.x > 0)
            {
                angle = -angle;
            }

            if (!fullcircle)
            {
                tmpTGT = OrigRef;
                target = new Vector2(tmpTGT.x, tmpTGT.y);
                float angleRef = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);
                if (target.x > 0)
                {
                    angleRef = -angleRef;
                }

                angle = Mathf.Clamp(angle, angleRef - angleMinus, angleRef + anglePlus);

                if (angle == 0) return;
            }

            pivot.RotateAround(pivot.forward, angle);
        }

        public void RotToTarget(Vector3 Tgt)
        {
            Vector3 tmpTGT = pivot.InverseTransformPoint(Tgt);
            Vector2 target = new Vector2(tmpTGT.x, tmpTGT.y);

            float angle = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);

            if (angle == 0) return;

            angle = Mathf.Clamp(increment * TimeWarp.deltaTime, 0, angle);
            if (target.x > 0)
            {
                angle = -angle;
            }

            if (!fullcircle)
            {
                tmpTGT = OrigRef;
                target = new Vector2(tmpTGT.x, tmpTGT.y);
                float angleRef = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);
                if (target.x > 0)
                {
                    angleRef = -angleRef;
                }

                angle = Mathf.Clamp(angle, angleRef - angleMinus, angleRef + anglePlus);

                if (angle == 0) return;
            }

            pivot.RotateAround(pivot.forward, angle);
        }

        public bool RotToOrigin()
        {
            Vector3 tmpTGT = OrigRef;
            Vector2 target = new Vector2(tmpTGT.x, tmpTGT.y);

            float angle = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);

            if (angle == 0) return true;

            angle = Mathf.Clamp(increment * TimeWarp.deltaTime, 0, angle);
            if (target.x > 0)
                angle = -angle;

            if (!fullcircle)
            {
                target = new Vector2(tmpTGT.x, tmpTGT.y);
                float angleRef = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);
                if (target.x > 0)
                {
                    angleRef = -angleRef;
                }

                angle = Mathf.Clamp(angle, angleRef - angleMinus, angleRef + anglePlus);

                if (angle != 0)
                    pivot.RotateAround(pivot.forward, angle);
            }
            else
                pivot.RotateAround(pivot.forward, angle);

            return false;
        }

    }


    public class ModuleRTAnimTrackAntenna : RemoteTechAntennaCore
    {
        [KSPField]
        public string
        Animation = "",
        Pivot1Name = "",
        Pivot2Name = "",
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
        ShrapnelDragCoeff = 2F,
        ShrapnelDensity = 1,
        EnergyDrain0 = 0,
        EnergyDrain1 = 0,
        Mode0EnergyCost = 0,
        Mode1EnergyCost = 0,
        antennaRange0 = 0,
        antennaRange1 = 0,
        dishRange0 = 0,
        dishRange1 = 0,
        Pivot1Speed = 1,
        Pivot2Speed = 1;

        [KSPField]
        public Vector2 Pivot1Range = Vector2.zero, Pivot2Range = Vector2.zero;

        Pivot pivot1, pivot2;
        Transform Pivot2Dir;
        TrackingModes mode = TrackingModes.RETRACTED;

        public enum TrackingModes
        {
            RETRACTED,
            EXTENDING,
            TRACKING,
            RESETTING,
            RETRACTING,
            BROKEN
        }


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

        Transform Pivot1
        {
            get { return part.FindModelTransform(Pivot1Name); }
        }

        Transform Pivot2
        {
            get { return part.FindModelTransform(Pivot2Name); }
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
                    m.Locked = true;

                foreach (ModuleRTAnimTrackAntenna m in part.Modules.OfType<ModuleRTAnimTrackAntenna>())
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

                foreach (ModuleRTAnimTrackAntenna m in part.Modules.OfType<ModuleRTAnimTrackAntenna>())
                    m.SetMode1();
            }
            else
                SetMode1();

            if (ModeLock)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.Locked = false;

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.Locked = false;

                foreach (ModuleRTAnimTrackAntenna m in part.Modules.OfType<ModuleRTAnimTrackAntenna>())
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
            if ((mode == TrackingModes.RETRACTED || mode == TrackingModes.RETRACTING || mode == TrackingModes.RESETTING) && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
            else
                if ((mode == TrackingModes.TRACKING || mode == TrackingModes.EXTENDING) && RequestActPower(Mode0EnergyCost))
                {
                    act0();
                }
        }

        [KSPAction("Mode1Action", KSPActionGroup.None, guiName = "Mode1")]
        public void Mode1Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && (mode == TrackingModes.RETRACTED || mode == TrackingModes.RETRACTING || mode == TrackingModes.RESETTING) && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPAction("Mode0Action", KSPActionGroup.None, guiName = "Mode0")]
        public void Mode0Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && (mode == TrackingModes.TRACKING || mode == TrackingModes.EXTENDING) && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "Mode1Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode1Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if ((mode == TrackingModes.RETRACTED || mode == TrackingModes.RETRACTING || mode == TrackingModes.RESETTING) && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }
        [KSPEvent(name = "Mode0Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode0Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if ((mode == TrackingModes.TRACKING || mode == TrackingModes.EXTENDING) && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "OverrideMode1Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode1Event()
        {
            if (Locked) return;
            if (!powered) return;
            if ((mode == TrackingModes.RETRACTED || mode == TrackingModes.RETRACTING || mode == TrackingModes.RESETTING) && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPEvent(name = "OverrideMode0Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode0Event()
        {
            if (Locked) return;
            if (!powered) return;
            if ((mode == TrackingModes.TRACKING || mode == TrackingModes.EXTENDING) && RequestActPower(Mode0EnergyCost))
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

            if (mode == TrackingModes.RESETTING)
            {
                mode = TrackingModes.TRACKING;
            }
            else
            {
                anim[Animation].speed = Mathf.Abs(anim[Animation].speed);

                anim.Play(Animation);
                mode = TrackingModes.EXTENDING;

                if (anim[Animation].normalizedTime == 1)
                    anim[Animation].normalizedTime = 0;
            }
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

            mode = TrackingModes.RESETTING;

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
                mode = TrackingModes.BROKEN;
                List<Transform> toRemove = new List<Transform>();
                RTUtils.findTransformsWithCollider(part.FindModelTransform(Pivot1Name), ref toRemove);
                foreach (Transform t in toRemove)
                    Destroy(t.gameObject);

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

            if (fixAnimLayers)
            {
                int i = 0;
                foreach (AnimationState s in anim)
                {
                    s.layer = i;
                    i++;
                }
            }

            if (animState == 1)
            {
                act1();

                anim[Animation].speed = Mathf.Abs(anim[Animation].speed);

                mode = TrackingModes.TRACKING;

                anim.Play(Animation);
            }
            else
            {
                act0();

                anim[Animation].speed = -Mathf.Abs(anim[Animation].speed);

                mode = TrackingModes.RETRACTED;

                anim.Play(Animation);
            }

            anim[Animation].wrapMode = WrapMode.Clamp;

            base.OnStart(state);

            if (state != StartState.Editor)
            {
                Pivot2Dir = part.FindModelTransform(Pivot2Name);

                pivot1 = new Pivot(part.FindModelTransform(Pivot1Name), Pivot1Speed, Pivot1Range);
                pivot2 = new Pivot(Pivot2Dir, Pivot2Speed, Pivot2Range);

                if (animState == 1)
                {
                    mode = TrackingModes.TRACKING;
                    if (target.isTarget)
                    {
                        pivot1.SnapToTarget(target.position);
                        pivot2.SnapToTarget(target.position);
                    }
                }
            }

            anim[Animation].normalizedTime = animState;
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

        public override void OnUpdate()
        {
            if (!flightStarted) return;

            switch (mode)
            {
                case TrackingModes.BROKEN:
                    return;
                case TrackingModes.TRACKING:
                    if (target.isTarget && powered)
                    {
                        pivot1.RotToTarget(target.position);
                        pivot2.RotToTarget(target.position);
                    }
                    break;
                case TrackingModes.EXTENDING:
                    if (!anim.IsPlaying(Animation))
                        mode = TrackingModes.TRACKING;
                    break;
                case TrackingModes.RETRACTING:
                    if (!anim.IsPlaying(Animation))
                        mode = TrackingModes.RETRACTED;
                    break;
                case TrackingModes.RESETTING:
                    if (pivot1.RotToOrigin() & pivot2.RotToOrigin())
                    {
                        anim[Animation].speed = -Mathf.Abs(anim[Animation].speed);

                        anim.Play(Animation);

                        if (anim[Animation].normalizedTime == 0)
                            anim[Animation].normalizedTime = 1;
                        mode = TrackingModes.RETRACTING;
                    }
                    break;
            }

            if (EnergyDrain1 > 0)
                RequestPower();

            if (vessel != null && RTUtils.PhysicsActive)
            {
                if (willWakeInPanic && animState == 0 && !InControl && !anim.IsPlaying(Animation))
                {
                    SetMode1();
                    UpdateGUI();
                }


                if (MaxQ > 0 && mode != TrackingModes.RETRACTED && vessel.atmDensity > 0 && (Math.Pow(RTUtils.DirectionalSpeed(Pivot2Dir.up, vessel.srf_velocity), 2) * vessel.atmDensity * 0.5) > MaxQ)
                {
                    broken = true;
                    mode = TrackingModes.BROKEN;
                    List<Transform> toRemove = new List<Transform>();
                    RTUtils.findTransformsWithCollider(part.FindModelTransform(Pivot1Name), ref toRemove);

                    foreach (Transform t in toRemove)
                    {
                        Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();
                        
                        rb.angularDrag = 0;
                        rb.angularVelocity = part.rigidbody.angularVelocity;
                        rb.drag = 0;
                        rb.mass = t.collider.bounds.size.x * t.collider.bounds.size.y * t.collider.bounds.size.z * ShrapnelDensity;
                        rb.velocity = part.rigidbody.velocity;
                        rb.isKinematic = false;
                        t.parent = null;
                        rb.AddForce(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5));
                        rb.AddTorque(UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20));

                        DragModel dm = t.gameObject.AddComponent<DragModel>();
                        dm.enabled = true;
                        dm.tf = t;
                        dm.rb = rb;
                        dm.dc = ShrapnelDragCoeff;
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
