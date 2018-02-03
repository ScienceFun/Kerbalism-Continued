namespace KERBALISM 
{
  public sealed class GravityRing : PartModule, ISpecifics
  {
    [KSPField] public double ec_rate;                                   // ec consumed per-second when deployed
    [KSPField] public string deploy = string.Empty;                     // a deploy animation can be specified
    [KSPField] public string rotate = string.Empty;                     // a rotate loop animation can be specified

    [KSPField(isPersistant = true)] public bool deployed;               // true if deployed

    Animator deploy_anim;
    Animator rotate_anim;

    // pseudo-ctor
    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // get animations
      deploy_anim = new Animator(part, deploy);
      rotate_anim = new Animator(part, rotate);

      // set animation state
      deploy_anim.Still(deployed ? 1.0f : 0.0f);
      if (deployed) rotate_anim.Play(false, true);

      // show the deploy toggle if it is deployable
      Events["Toggle"].active = deploy.Length > 0;
    }

    public void Update()
    {
      // update RMB ui
      Events["Toggle"].guiName = deployed ? "Retract" : "Deploy";

      // if it is deploying, wait until the animation is over
      if (deployed && !deploy_anim.Playing() && !rotate_anim.Playing())
      {
        // then start the rotate animation
        rotate_anim.Play(false, true);
      }

      // in flight, if deployed
      if (Lib.IsFlight() && deployed)
      {
        // if there is no ec
        if (ResourceCache.Info(vessel, "ElectricCharge").amount < 0.01)
        {
          // pause rotate animation
          // - safe to pause multiple times
          rotate_anim.Pause();
        }
        // if there is enough ec instead
        else
        {
          // resume rotate animation
          // - safe to resume multiple times
          rotate_anim.Resume(false);
        }
      }
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

      // stop loop animation if exist and we are retracting
      if (!deployed)
      {
        rotate_anim.Stop();
      }

      // start deploy animation in the correct direction, if exist
      deploy_anim.Play(!deployed, false);

      // update ui
      Events["Toggle"].guiName = deployed ? "Retract" : "Deploy";
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