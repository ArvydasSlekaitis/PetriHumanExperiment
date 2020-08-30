using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Human : LivingCreature
{   
    protected override float AdultAge => 18;
    protected override float StartingHealth => Random.Range(0.1f, 1.0f);
    protected override bool IsMonogamous => true;
    protected override float AverageAge  => 20.0f;
    protected override RangeInt OffspringsPerBirth => new RangeInt(1, 1);

//********************************************************************************

    protected override List<LivingCreature> GetTabooPartners(List<LivingCreature> iParents, List<LivingCreature> iChildren)
    {
        var results = new List<LivingCreature>();

        results.AddRange(iChildren);

        foreach(var p in iParents)
            if(p!=null)
            {
                results.Add(p);
                results.AddRange(p.Parents);
                results.AddRange(p.Children);
            }

        return results;
    }

    //********************************************************************************
}
