using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MainController : MonoBehaviour
{
    public TileBase[] grassTiles;
    public TileBase[] berriesTiles;
    public TileBase[] rabbitsTiles;

    public Tilemap tilemap;
    public GameObject humanPrefab;
    public GameObject resourcePrefab;

    List<LivingCreature> humans = new List<LivingCreature>();

    float[,] fertilityGrid;
    int[,] tilesIDGrid;

//********************************************************************************

    void Awake()
    {
        humans = Spawn(humanPrefab, new Vector2(100.0f, 100.0f), 20.0f, 100);

        fertilityGrid = new float[100, 100];
        tilesIDGrid = new int[100, 100];
        for (int x = 0; x < fertilityGrid.GetUpperBound(0); x++)
            for (int y = 0; y < fertilityGrid.GetUpperBound(1); y++)
            {
                fertilityGrid[x,y] = 1;
                tilesIDGrid[x,y] = Random.Range(0, 5);
            }

        RenderMap(fertilityGrid, tilesIDGrid, tilemap, grassTiles, berriesTiles, rabbitsTiles);
    }

//********************************************************************************

    void Start()
    {        
    }

//********************************************************************************

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        UpdateLivingCreate(humans, fertilityGrid, new List<LivingCreature>());
        UpdateGraze(humans, fertilityGrid, 1.0f);
       
        for (int x = 0; x < fertilityGrid.GetUpperBound(0); x++)
            for (int y = 0; y < fertilityGrid.GetUpperBound(1); y++)
                fertilityGrid[x,y] =  Mathf.Min(fertilityGrid[x,y] + Time.deltaTime*0.1f);

/*
        if (Input.GetMouseButton(0))
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
            var cell = tilemap.layoutGrid.WorldToCell(pos);

            for(int x=-1; x<2; x++)
                for(int y=-1; y<2; y++)
                    fertilityGrid[cell.x + x,cell.y + y] = 0.0f;           
        }
*/


        RenderMap(fertilityGrid, tilesIDGrid, tilemap, grassTiles, berriesTiles, rabbitsTiles); 
    }

//********************************************************************************

    public void OnNewborn(LivingCreature iNewborn)
    {
        for(int i=0; i<humans.Count; i++)
        if(humans[i] == null)
        {
            humans[i] = iNewborn;
            return;
        }

        humans.Add(iNewborn);
    }

//********************************************************************************

    public static void RenderMap(float[,] iFertility, int[,] iTilesID, Tilemap iTilemap, TileBase[] iGrassTiles, TileBase[] iBerriesTiles, TileBase[] iRabbitsTiles)
    {
        iTilemap.ClearAllTiles(); 

        for (int x = 0; x < iFertility.GetUpperBound(0) ; x++) 
            for (int y = 0; y < iFertility.GetUpperBound(1); y++) 
                iTilemap.SetTile(new Vector3Int(x, y, 0), FertilityToTile(iFertility[x,y], iGrassTiles[iTilesID[x,y]], iBerriesTiles[iTilesID[x,y]], iRabbitsTiles[iTilesID[x,y]])); 
    }

    //********************************************************************************

    static TileBase FertilityToTile(float iFertility, TileBase iGrassTiles, TileBase iBerriesTiles, TileBase iRabbitsTiles)
    {
       if(iFertility >= 0.75f)
        return iRabbitsTiles;

        if(iFertility >= 0.5f)
            return iBerriesTiles;
            
        return iGrassTiles;
    }

    //********************************************************************************

    public static int WorldToFertilityGridPos(float iPosition) => Mathf.RoundToInt((iPosition - 1.0f) / 2.0f);

    public static float FertilityGridToWorldPos(int iPosition) => iPosition * 2.0f + 1.0f;
    public static Vector2 FertilityGridToWorldPos(Vector2 iPos) => new Vector2(FertilityGridToWorldPos((int)iPos.x), FertilityGridToWorldPos((int)iPos.y));

    //********************************************************************************

    static List<LivingCreature> Spawn(GameObject iPrefab, Vector2 iCenter, float iRadius, int iCount)
    {
        var results = new List<LivingCreature>();

        for(int i=0; i<iCount; i++)
        {
            var obj = Instantiate(iPrefab).GetComponent<LivingCreature>();
            obj.OnBorn(null, null);
            var pos = Random.insideUnitCircle*iRadius;
            obj.transform.position = iCenter + pos;
            results.Add(obj);
        }

        return results;
    }

    //********************************************************************************

    static Vector2 FindClosestResource(float[,] iFertilityGrid, Vector3 iPosition)
    {
        var posX = MainController.WorldToFertilityGridPos(iPosition.x);
        var posY = MainController.WorldToFertilityGridPos(iPosition.y);

        if(iFertilityGrid[posX,posY] > 0.5f)
            return MainController.FertilityGridToWorldPos(new Vector2(posX, posY));

        for(int i=-1; i<2; i++)
            for(int k=-1; k<2; k++)
                if(iFertilityGrid[posX+i, posY+k] >= 0.5f)
                    return MainController.FertilityGridToWorldPos(new Vector2(posX+i, posY+k));

        return Vector2.zero;    
    } 

//********************************************************************************

    static float GetAreaFertility(float[,] iFertilityGrid, Vector3 iPosition)
    {
        var posX = MainController.WorldToFertilityGridPos(iPosition.x);
        var posY = MainController.WorldToFertilityGridPos(iPosition.y);

        var fert = 0.0f;
        for(int i=-2; i<3; i++)
            for(int k=-2; k<3; k++)
                fert += iFertilityGrid[posX+i, posY+k];

        return fert / 25.0f;    
    }

    //******************************************************************************** 

    static void UpdateLivingCreate(List<LivingCreature> iLivingCreature, float[,] iFertilityGrid, List<LivingCreature> iEnemies)
    {
        var creatures = new List<LivingCreature>(iLivingCreature);

        foreach(var creature in creatures)
            if(creature!=null)
            {
                var closestFood = FindClosestResource(iFertilityGrid, creature.transform.position);
                                
                if(creature.IsAdult && iEnemies.Count > 0)
                {
                    var closestEnemy = LivingCreature.FindClosest(iEnemies, creature.transform.position);

                    if(closestEnemy!=null && Vector3.Distance(closestEnemy.transform.position, creature.transform.position) < 5.0f)
                        closestFood = new Vector2(closestEnemy.transform.position.x, closestEnemy.transform.position.y);
                }

                creature.UpdateLogic(iLivingCreature, closestFood, GetAreaFertility(iFertilityGrid, creature.transform.position));
            }
    }

    //******************************************************************************** 

    static void UpdateGraze(List<LivingCreature> iLivingCreature, float[,] iFertilityGrid, float iGrazeAmount)
    {
        foreach(var creature in iLivingCreature)
            if(creature!=null)
            {
                int x = WorldToFertilityGridPos(creature.transform.position.x);
                int y = WorldToFertilityGridPos(creature.transform.position.y);
                iFertilityGrid[x, y] = Mathf.Max(0.0f, iFertilityGrid[x, y] - Time.deltaTime*iGrazeAmount);
            }
    }

    //******************************************************************************** 
}
