namespace KERBALISM 
{
  public sealed class GravityRing : PartModule, ISpecifics
  {
    [KSPField] public double ec_rate;                                   // ec consumed per-second when deployed
    [KSPField] public string deploy = string.Empty;                     // a deploy animation can be specified
    [KSPField] public string rotate = string.Empty;                     // a rotate loop animation can be specified

    [KSPField(isPersistant = true)] public bool deployed;               // true if deployed
    
    // Add compatibility and revert animation
    [KSPField] public bool  animBackwards;                              // If animation is playing in backwards, this can help to fix
    [KSPField] public bool  rotateIsTransform;                          // Rotation is not an animation, but a Transform
    [KSPField] public float SpinRate = 20.0f;                           // Speed of the centrifuge rotation in deg/s
    [KSPField] public float SpinAccelerationRate = 1.0f;                // Rate at which the SpinRate accelerates (deg/s/s)

    Animator deploy_anim;
    Animator rotate_anim;

    // Add compatibility
    Transformator rotate_transf;

    // pseudo-ctor
    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // get animations
      deploy_anim = new Animator(part, deploy);
      rotate_anim = new Animator(part, rotate);
      // if is using Transform
      rotate_transf = new Transformator(part, rotate, SpinRate, SpinAccelerationRate);

      // set animation state / invert animation
      if (animBackwards) deploy_anim.Still(deployed ? 0.0f : 1.0f);
      else deploy_anim.Still(deployed ? 1.0f : 0.0f);

      if (deployed)
      {
        rotate_transf.Play();
        rotate_anim.Play(false, true);
      }

      // show the deploy toggle if it is deployable
      Events["Toggle"].active = deploy.Length > 0;
    }

    public void Update()
    {
      // in flight, if deployed
      if (Lib.IsFlight() && deployed)
      {
        // if there is no ec
        if (ResourceCache.Info(vessel, "ElectricCharge").amount > double.Epsilon)
        {
          // pause rotate animation
          // - safe to pause multiple times
          if(rotateIsTransform && !rotate_transf.IsRotating()) rotate_transf.Play();
          else rotate_anim.Resume(false);
        }
        // if there is enough ec instead
        else
        {
          // resume rotate animation
          // - safe to resume multiple times
          if (rotateIsTransform && rotate_transf.IsRotating()) rotate_transf.Stop();
          else rotate_anim.Pause();
        }
      }

      Lib.Debug("Deploy is playing: {0}", deploy_anim.Playing());

      // update RMB ui
      //if (deploy_anim.Playing()) Events["Toggle"].active = false;
      //else Events["Toggle"].active = true;

      Events["Toggle"].guiName = deployed ? "Retract" : "Deploy";
    }

    public void FixedUpdate()
    {
      // do nothing in the editor
      if (Lib.IsEditor()) return;

      // if the module is either non-deployable or deployed
      if (deploy.Length == 0 || deployed)
      {
        // get resource handler
        Resource_Info ec = ResourceCache.Info(vessel, "ElectricCharge");

        // consume ec
        ec.Consume(ec_rate * Kerbalism.elapsed_s);
      }

      if (rotate_transf != null) rotate_transf.DoSpin();
    }

    public static void BackgroundUpdate(Vessel vessel, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, GravityRing ring, Resource_Info ec, double elapsed_s)
    {
      // if the module is either non-deployable or deployed
      if (ring.deploy.Length == 0 || Lib.Proto.GetBool(m, "deployed"))
      {
        // consume ec
        ec.Consume(ring.ec_rate * elapsed_s);
      }
    }

    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Deploy", active = true)]
    public void Toggle()
    {
      // switch deployed state
      deployed = !deployed;
    }

    // action groups
    [KSPAction("Deploy/Retract Ring")] public void Action(KSPActionParam param) { Toggle(); }

    // part tooltip
    public override string GetInfo()
    {
      return Specs().Info();
    }

    // specifics support
    public Specifics Specs()
    {
      Specifics specs = new Specifics();
      specs.Add("bonus", "firm-ground");
      specs.Add("EC/s", Lib.HumanReadableRate(ec_rate));
      specs.Add("deployable", deploy.Length > 0 ? "yes" : "no");
      return specs;
    }
  }
}