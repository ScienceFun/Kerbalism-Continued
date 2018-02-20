using System;
using System.Collections.Generic;

namespace KERBALISM
{
  public sealed class KerbalRule
  {
    public string       name;                 // unique name for the rule

    public double       increase_rate;        // base rate for the accumulator increase
    public List<string> increase_modifier;    // modifier for the accumulator increase rate
    public double       decrease_rate;        // base rate for the accumulator decrease
    public List<string> decrease_modifier;    // modifier for the accumulator decrease rate
    public bool         decrease_always;      // if false, decrease only if increase rate (after modifier) is zero

    public string       process_name;         // name of the process that is running for each kerbal
    public Process      process;              // reference to the process, if any       
    public bool         process_increase;     // if true, increase happen only when the process is input-deprived
    public bool         process_decrease;     // if true, decrease happen only when the process can run (not input-deprived)

    public double       threshold_variance;   // per kerbal variance on all thresholds
    public double       level1_bonus;         // % increase on all thresholds for a level 1 kerbal 
    public double       level2_bonus;         // % increase on all thresholds for a level 2 kerbal
    public double       level3_bonus;         // % increase on all thresholds for a level 3 kerbal
    public double       level4_bonus;         // % increase on all thresholds for a level 4 kerbal
    public double       level5_bonus;         // % increase on all thresholds for a level 5 kerbal

    public bool         death;                // if true, death happens when the fatal_threshold is reached
    public bool         death_random;         // if true, death happens randomly with a an increasing probability (0% at danger_threshold, 100% at fatal_threshold)
    public double       warning_threshold;
    public double       danger_threshold;
    public double       fatal_threshold;

    public double       mumbling_threshold;   // threshold for breakdown effect (0 = deactivated)
    public double       fatfinger_threshold;  // threshold for breakdown effect (0 = deactivated)
    public double       wrongvalve_threshold; // threshold for breakdown effect (0 = deactivated)
    public double       rage_threshold;       // threshold for breakdown effect (0 = deactivated)

    public string       warning_message;
    public string       danger_message;
    public string       fatal_message;
    public string       relax_message;

    public KerbalRule(ConfigNode node)
    {
      name                  = Lib.ConfigValue(node, "name", string.Empty);

      increase_rate         = Lib.ConfigValue(node, "increase_rate", 0.0);
      increase_modifier     = Lib.Tokenize(Lib.ConfigValue(node, "increase_modifier", string.Empty), ',');
      decrease_rate         = Lib.ConfigValue(node, "decrease_rate", 0.0);
      decrease_modifier     = Lib.Tokenize(Lib.ConfigValue(node, "decrease_modifier", string.Empty), ',');
      decrease_always       = Lib.ConfigValue(node, "decrease_always", true);

      process_name          = Lib.ConfigValue(node, "process_name", string.Empty);
      process_increase      = Lib.ConfigValue(node, "process_increase", true);
      process_decrease      = Lib.ConfigValue(node, "process_decrease", true);

      threshold_variance    = Lib.ConfigValue(node, "threshold_variance", 0.0);
      level1_bonus          = Lib.ConfigValue(node, "level1_bonus", 0.0);
      level2_bonus          = Lib.ConfigValue(node, "level2_bonus", 0.0);
      level3_bonus          = Lib.ConfigValue(node, "level3_bonus", 0.0);
      level4_bonus          = Lib.ConfigValue(node, "level4_bonus", 0.0);
      level5_bonus          = Lib.ConfigValue(node, "level5_bonus", 0.0);

      death                 = Lib.ConfigValue(node, "death", true);
      death_random          = Lib.ConfigValue(node, "death_random", true);
      warning_threshold     = Lib.ConfigValue(node, "warning_threshold", 0.33);
      danger_threshold      = Lib.ConfigValue(node, "danger_threshold", 0.66);
      fatal_threshold       = Lib.ConfigValue(node, "fatal_threshold", 1.0);

      mumbling_threshold    = Lib.ConfigValue(node, "mumbling_threshold", 0.0);
      fatfinger_threshold   = Lib.ConfigValue(node, "fatfinger_threshold", 0.0);
      wrongvalve_threshold  = Lib.ConfigValue(node, "wrongvalve_threshold", 0.0);
      rage_threshold        = Lib.ConfigValue(node, "rage_threshold", 0.0);

      warning_message       = Lib.ConfigValue(node, "warning_message", string.Empty);
      danger_message        = Lib.ConfigValue(node, "danger_message", string.Empty);
      fatal_message         = Lib.ConfigValue(node, "fatal_message", string.Empty);
      relax_message         = Lib.ConfigValue(node, "relax_message", string.Empty);

      // check that name is specified
      if (name.Length == 0) throw new Exception("skipping unnamed rule");

      // get the process
      if (process_name.Length != 0)
      {
        process = Profile.processes.Find(k => k.name == process_name);
        if (process == null) throw new Exception("cannot find process");

      }

      // check that degeneration is not zero
      // if (degeneration <= double.Epsilon) throw new Exception("skipping zero degeneration rule");

      // check that resources exist
      // if (input.Length > 0 && Lib.GetDefinition(input) == null) throw new Exception("resource '" + input + "' doesn't exist");
      // if (output.Length > 0 && Lib.GetDefinition(output) == null) throw new Exception("resource '" + output + "' doesn't exist");

      // calculate ratio of input vs output resource
      //if (input.Length > 0 && output.Length > 0 && ratio <= double.Epsilon)
      //{
      //  var input_density = Lib.GetDefinition(input).density;
      //  var output_density = Lib.GetDefinition(output).density;
      //  ratio = Math.Min(input_density, output_density) > double.Epsilon ? input_density / output_density : 1.0;
      //}
    }
  }
}