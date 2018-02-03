﻿using Contracts;

namespace KERBALISM.CONTRACTS 
{
  // First space harvest
  public sealed class SpaceHarvest : Contract
  {
    protected override bool Generate()
    {
      // never expire
      deadlineType = DeadlineType.None;
      expiryType = DeadlineType.None;

      // set reward
      SetScience(25.0f);
      SetReputation(30.0f, 10.0f);
      SetFunds(25000.0f, 100000.0f);

      // add parameters
      AddParameter(new SpaceHarvestCondition());
      return true;
    }

    protected override string GetHashString()
    {
      return "SpaceHarvest";
    }

    protected override string GetTitle()
    {
      return "Harvest food in space";
    }

    protected override string GetDescription()
    {
      return "Now that we got the technology to grow food in space, "
           + "we should probably test it. Harvest food from a greenhouse in space.";
    }

    protected override string MessageCompleted()
    {
      return "We harvested food in space, and our scientists says it is actually delicious.";
    }

    public override bool MeetRequirements()
    {
      // stop checking when requirements are met
      if (!meet_requirements)
      {
        var greenhouse = PartLoader.getPartInfoByName("kerbalism-greenhouse");

        meet_requirements =
             greenhouse != null                                           // greenhouse part is present
          && greenhouse.tags.Contains("_kerbalism")                       // greenhouse part is enabled
          && ResearchAndDevelopment.PartTechAvailable(greenhouse)         // greenhouse part unlocked
          && !DB.landmarks.space_harvest;                                 // greenhouse never harvested in space before
      }
      return meet_requirements;
    }

    bool meet_requirements;
  }

  public sealed class SpaceHarvestCondition : ContractParameter
  {
    protected override string GetHashString()
    {
      return "SpaceHarvestCondition";
    }

    protected override string GetTitle()
    {
      return "Harvest food in space";
    }

    protected override void OnUpdate()
    {
      if (DB.landmarks.space_harvest) SetComplete();
    }
  }
}