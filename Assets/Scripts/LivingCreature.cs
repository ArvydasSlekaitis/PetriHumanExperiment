using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LivingCreature : MonoBehaviour
{
    public enum Gender{Male, Female};

    public GameObject prefab;
    public GameObject deathFXPrefab;
    public GameObject loveFXPrefab;
    public GameObject birthFXPrefab;

    public Sprite childrenSprite;
    public Sprite[] adultSprites;

    float health;
    Gender gender;
    float birthDate;

    readonly List<LivingCreature> parents = new List<LivingCreature>();
    readonly List<LivingCreature> children = new List<LivingCreature>();
    LivingCreature spouse = null;

    public List<LivingCreature> Parents {get => parents; }
    public List<LivingCreature> Children { get => children; }

    List<LivingCreature> taboo = new List<LivingCreature>();

    float nextImpulseTime = 0.0f;
    float lastChildbirthData = 0.0f;

    protected float Age => Time.fixedTime - birthDate;
    
    public bool IsAdult => Age >= AdultAge; 
    protected abstract float StartingHealth { get; }
    protected abstract float AdultAge { get; }
    protected abstract List<LivingCreature> GetTabooPartners(List<LivingCreature> iParents, List<LivingCreature> iChildren);
    protected abstract bool IsMonogamous { get; }
    protected abstract float AverageAge { get; }
    protected abstract RangeInt OffspringsPerBirth { get; }

    static MainController mainController;
    new Rigidbody2D rigidbody;

    GameObject loveFX = null;
    GameObject birthFX = null;

    Vector3 fxOffset = new Vector3(0.0f, 0.0f, -0.5f);
    
//********************************************************************************

    public void Awake()
    {
        mainController = GameObject.FindObjectOfType<MainController>();
        rigidbody = GetComponent<Rigidbody2D>();
    }

//********************************************************************************

    public void OnBorn(LivingCreature iMother, LivingCreature iFather)
    {
        health = StartingHealth;
        gender = (Gender)Random.Range(0, 2);
        birthDate = Time.fixedTime - Random.Range(0, AdultAge);
        parents.Add(iMother);
        parents.Add(iFather);
        spouse = null;

        // Birth FX
        birthFX = Instantiate(birthFXPrefab, transform.position + fxOffset, Quaternion.identity);
        Destroy(birthFX, 0.9f);

       UpdateSprite();
    }

//********************************************************************************

    void OnDie()
    {
        GameObject.Destroy(Instantiate(deathFXPrefab, transform.position + fxOffset, Quaternion.identity), 1.5f);
    }

//********************************************************************************

    public void UpdateLogic(List<LivingCreature> iHumans, Vector2 iClosestResource, float iFertilityRatio)
    {
        taboo = GetTabooPartners(parents, children);
        var significantRelative = GetSignificantRelative();      
        var closestResource = iClosestResource;
        var fertility = iFertilityRatio;

        var chanceToDie = (1.0f - health)*Time.deltaTime / AverageAge;

        if(!IsAdult && significantRelative is null)
            chanceToDie *= 5.0f;

        if(closestResource == Vector2.zero)
            health = Mathf.Max(0.0f, health - (1.0f-fertility)*Time.deltaTime);

        if(Random.value < chanceToDie || health <= 0.0f)
        {
            OnDie();
            Destroy(gameObject);
            return;
        }        

        // Update love fx position
        if(loveFX!=null && spouse != null)
            loveFX.transform.position = (transform.position + spouse.transform.position) / 2.0f + fxOffset;

        // Update birth fx position
        if(birthFX != null)
            birthFX.transform.position = transform.position + fxOffset;

        UpdateMovement(iHumans, significantRelative, closestResource);
        UpdateReproduction();
        UpdateSprite();
    }

//********************************************************************************

    LivingCreature GetSignificantRelative()
    {
        if(spouse != null)
            return spouse;

        if(IsAdult)
        {
            for(int i=0; i<children.Count; i++)
                if(children[i] != null)
                    return children[i];
        }
        else
        {
            for(int i=0; i<parents.Count; i++)
                if(parents[i]!=null)
                    return parents[i];             
        }

        return null;
    }

//********************************************************************************

    LivingCreature FindClosestSpouse(List<LivingCreature> iHumans)
    {
        LivingCreature closestSpouse = null;
        float distance = float.PositiveInfinity;

        foreach(var human in iHumans)
            if(human != null)
                if(human.gender != gender && human.spouse == null && human.IsAdult && !taboo.Contains(human))
                {
                    var d = Vector3.Distance(human.transform.position, transform.position);
                    if(d < distance)
                    {
                        closestSpouse = human;
                        distance = d;
                    }                    
                }

        return closestSpouse;
    }

//********************************************************************************

    public void OnCollisionStay2D(Collision2D iOther)
    {
        LivingCreature other = iOther.gameObject.GetComponent<LivingCreature>();
        if(other is null)
            return;

        if(this.GetType().Name != other.GetType().Name)
        {
            if(this.GetType().Name == "Human" && IsAdult)
            {
                health = Mathf.Min(1.0f, health + 0.1f);
                other.OnDie();
                Destroy(other.gameObject); 
            }
            return;
        }

        if(spouse == null && IsAdult && other.IsAdult && other.spouse is null && other.gender!=gender && !taboo.Contains(other))
        {
            if(IsMonogamous)
            {
                loveFX = Instantiate(loveFXPrefab, (transform.position + other.transform.position) / 2.0f + new Vector3(0.0f, 0.0f, -0.5f), Quaternion.identity);
                Destroy(loveFX , 2.0f);
                spouse = other; 
                other.spouse = this;
                lastChildbirthData = Time.fixedTime;
                spouse.lastChildbirthData = Time.fixedTime;
            }
            else
                if(Time.fixedTime - lastChildbirthData > 1.0f)
                    GiveBirth(prefab, OffspringsPerBirth, this, other);
        }

       
    }

//********************************************************************************

    void UpdateReproduction()
    {
        if(gender == Gender.Female && spouse !=null && Time.fixedTime - lastChildbirthData > 1.0f && Age < 40.0f)
            GiveBirth(prefab, OffspringsPerBirth, this, spouse);
    }

//********************************************************************************

    void UpdateSprite() => GetComponent<SpriteRenderer>().sprite = IsAdult ? adultSprites[(int)gender] : childrenSprite;

//********************************************************************************     

    void UpdateMovement(List<LivingCreature> iHumans, LivingCreature iSignificantRelative, Vector2 iClosestResource)
    {
        nextImpulseTime -= Time.deltaTime;
        if(nextImpulseTime <=0.0f)
		{
            if(spouse == null && IsAdult)
                {
                    var closestSpouse = FindClosestSpouse(iHumans);

                    if(closestSpouse != null && Vector3.Distance(transform.position, closestSpouse.transform.position) < 5.0f)
                    {
                        var dir =  closestSpouse.transform.position - transform.position;
                        rigidbody.AddForce(new Vector2(dir.x, dir.y).normalized, ForceMode2D.Impulse);
                    }
                }            
            else if(iSignificantRelative != null && Vector3.Distance(iSignificantRelative.transform.position, transform.position) > 2)
            {                
                var dir =  iSignificantRelative.transform.position - transform.position;
                rigidbody.AddForce(new Vector2(dir.x, dir.y).normalized, ForceMode2D.Impulse);
            }
            else if(iClosestResource != Vector2.zero)
            {
                var dir = new Vector3(iClosestResource.x, iClosestResource.y, 0.0f) - transform.position;
                rigidbody.AddForce(new Vector2(dir.x, dir.y).normalized, ForceMode2D.Impulse);
            }
            else
            {
                rigidbody.AddForce(Random.insideUnitCircle*0.1f, ForceMode2D.Impulse);
            }
            

        if((new Vector3(100.0f, 100.0f, 0.0f) - transform.position).magnitude > 15)
            rigidbody.AddForce((new Vector3(100.0f, 100.0f, 0.0f) -transform.position).normalized * 1.0f, ForceMode2D.Impulse);
        


           // else
            //    rigidbody.AddForce(Random.insideUnitCircle*1.0f, ForceMode2D.Impulse);
			              
			nextImpulseTime = Random.Range(0.01f, 0.5f);
		}
/*
        if(iClosestResource != null)
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(iClosestResource.x, iClosestResource.y), Time.deltaTime*10.0f);
        else
          rigidbody.AddForce(Random.insideUnitCircle*1.0f, ForceMode2D.Impulse);*/
    }

//********************************************************************************

    static void GiveBirth(GameObject iPrefab, RangeInt iOffspirngsPerBirth, LivingCreature iMother, LivingCreature iFather)
    {
        var numberOfOffsprings = Random.Range(iOffspirngsPerBirth.start, iOffspirngsPerBirth.end);
        for(int i=0; i<numberOfOffsprings; i++)
        {
            var child = GameObject.Instantiate(iPrefab, iMother.transform.position, Quaternion.identity).GetComponent<LivingCreature>();           
            child.OnBorn(iMother, iFather);
            iMother.children.Add(child);
            iFather.children.Add(child);     
            iMother.lastChildbirthData = iFather.lastChildbirthData = Time.fixedTime;               
            mainController.OnNewborn(child);
        }
    }

//********************************************************************************

    public static LivingCreature FindClosest(List<LivingCreature> iCreatures, Vector3 iPos)
    {
        LivingCreature closest = null;
        float closestDistance = float.MaxValue;

        foreach(var c in iCreatures)
            if(c!=null)
                if(Vector3.Distance(iPos, c.transform.position) < closestDistance)
                {
                    closestDistance = Vector3.Distance(iPos, c.transform.position);
                    closest = c;
                }

        return closest;
    }

    
    //******************************************************************************** 

}
